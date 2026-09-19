using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IAcademicAIConversationService
{
    Task<IReadOnlyList<AcademicAIConversation>> GetRecentAsync(string userId, int count);
    Task<AcademicAIConversation?> GetOwnedAsync(string userId, long conversationId, bool trackChanges = false);
    Task<AcademicAIConversation> CreateAsync(string userId, string title, AcademicAIMode mode);
    Task AddExchangeAsync(
        AcademicAIConversation conversation,
        string userPrompt,
        AcademicAIResult result);
    Task<bool> DeleteAsync(string userId, long conversationId);
}
