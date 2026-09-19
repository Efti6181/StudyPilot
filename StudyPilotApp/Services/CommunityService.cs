using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class CommunityService : ICommunityService
{
    private readonly ApplicationDbContext _dbContext;

    public CommunityService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CommunityPostPage> GetPostsAsync(
        string currentUserId,
        string? search,
        CommunityCategory? category,
        string sort,
        bool myPostsOnly,
        int page,
        int pageSize)
    {
        var query = _dbContext.CommunityPosts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(post =>
                EF.Functions.ILike(post.Title, pattern) ||
                EF.Functions.ILike(post.Content, pattern) ||
                EF.Functions.ILike(post.ApplicationUser.FullName, pattern));
        }

        if (category.HasValue) query = query.Where(post => post.Category == category.Value);
        if (myPostsOnly) query = query.Where(post => post.ApplicationUserId == currentUserId);

        var totalCount = await query.CountAsync();
        query = sort switch
        {
            "oldest" => query.OrderBy(post => post.CreatedAt),
            "popular" => query.OrderByDescending(post => post.Likes.Count)
                              .ThenByDescending(post => post.Comments.Count)
                              .ThenByDescending(post => post.CreatedAt),
            "discussed" => query.OrderByDescending(post => post.Comments.Count)
                                .ThenByDescending(post => post.CreatedAt),
            _ => query.OrderByDescending(post => post.CreatedAt)
        };

        var items = await query
            .Include(post => post.ApplicationUser)
            .Include(post => post.Likes)
            .Include(post => post.Comments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return new CommunityPostPage(items, totalCount);
    }

    public Task<CommunityPost?> GetPostAsync(int postId, bool trackChanges = false)
    {
        var query = _dbContext.CommunityPosts
            .Include(post => post.ApplicationUser)
            .Include(post => post.Likes)
            .Include(post => post.Comments)
                .ThenInclude(comment => comment.ApplicationUser)
            .Include(post => post.Comments)
                .ThenInclude(comment => comment.Likes)
            .AsSplitQuery()
            .Where(post => post.Id == postId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public Task<CommunityComment?> GetCommentAsync(int commentId, bool trackChanges = false)
    {
        var query = _dbContext.CommunityComments
            .Where(comment => comment.Id == commentId);
        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public async Task AddPostAsync(CommunityPost post) =>
        await _dbContext.CommunityPosts.AddAsync(post);

    public async Task AddCommentAsync(CommunityComment comment) =>
        await _dbContext.CommunityComments.AddAsync(comment);

    public async Task<bool> TogglePostLikeAsync(int postId, string userId)
    {
        if (!await _dbContext.CommunityPosts.AnyAsync(post => post.Id == postId))
            throw new KeyNotFoundException();

        var like = await _dbContext.CommunityPostLikes.SingleOrDefaultAsync(item =>
            item.CommunityPostId == postId && item.ApplicationUserId == userId);
        if (like is null)
        {
            await _dbContext.CommunityPostLikes.AddAsync(new CommunityPostLike
            {
                CommunityPostId = postId,
                ApplicationUserId = userId
            });
            await _dbContext.SaveChangesAsync();
            return true;
        }

        _dbContext.CommunityPostLikes.Remove(like);
        await _dbContext.SaveChangesAsync();
        return false;
    }

    public async Task<bool> ToggleCommentLikeAsync(int commentId, string userId)
    {
        if (!await _dbContext.CommunityComments.AnyAsync(comment => comment.Id == commentId))
            throw new KeyNotFoundException();

        var like = await _dbContext.CommunityCommentLikes.SingleOrDefaultAsync(item =>
            item.CommunityCommentId == commentId && item.ApplicationUserId == userId);
        if (like is null)
        {
            await _dbContext.CommunityCommentLikes.AddAsync(new CommunityCommentLike
            {
                CommunityCommentId = commentId,
                ApplicationUserId = userId
            });
            await _dbContext.SaveChangesAsync();
            return true;
        }

        _dbContext.CommunityCommentLikes.Remove(like);
        await _dbContext.SaveChangesAsync();
        return false;
    }

    public void RemovePost(CommunityPost post) => _dbContext.CommunityPosts.Remove(post);
    public void RemoveComment(CommunityComment comment) => _dbContext.CommunityComments.Remove(comment);
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
