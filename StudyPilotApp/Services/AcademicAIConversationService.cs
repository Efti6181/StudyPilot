using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AcademicAIConversationService : IAcademicAIConversationService
{
    private readonly ApplicationDbContext _dbContext;

    public AcademicAIConversationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AcademicAIConversation>> GetRecentAsync(string userId, int count) =>
        await _dbContext.AcademicAIConversations.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .OrderByDescending(item => item.UpdatedAt)
            .Take(Math.Clamp(count, 1, 30))
            .ToListAsync();

    public Task<AcademicAIConversation?> GetOwnedAsync(
        string userId,
        long conversationId,
        bool trackChanges = false)
    {
        var query = _dbContext.AcademicAIConversations
            .Include(item => item.Messages.OrderBy(message => message.CreatedAt))
            .Where(item => item.Id == conversationId && item.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public async Task<AcademicAIConversation> CreateAsync(
        string userId,
        string title,
        AcademicAIMode mode)
    {
        var conversation = new AcademicAIConversation
        {
            ApplicationUserId = userId,
            Title = NormalizeTitle(title),
            Mode = mode
        };
        await _dbContext.AcademicAIConversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();
        return conversation;
    }

    public async Task AddExchangeAsync(
        AcademicAIConversation conversation,
        string userPrompt,
        AcademicAIResult result)
    {
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.AcademicAIMessages.AddRangeAsync(
            new AcademicAIMessage
            {
                ConversationId = conversation.Id,
                Role = AcademicAIMessageRole.User,
                Content = userPrompt.Trim()
            },
            new AcademicAIMessage
            {
                ConversationId = conversation.Id,
                Role = AcademicAIMessageRole.Assistant,
                Content = result.Text,
                IsFallback = result.IsFallback,
                Provider = result.Provider[..Math.Min(result.Provider.Length, 80)]
            });
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string userId, long conversationId)
    {
        var conversation = await _dbContext.AcademicAIConversations.SingleOrDefaultAsync(item =>
            item.Id == conversationId && item.ApplicationUserId == userId);
        if (conversation is null) return false;
        _dbContext.AcademicAIConversations.Remove(conversation);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static string NormalizeTitle(string value)
    {
        value = string.Join(' ', value.Trim().Split(
            ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        if (value.Length == 0) return "Academic conversation";
        return value[..Math.Min(value.Length, 120)];
    }
}
