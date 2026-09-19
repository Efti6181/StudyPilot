using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface ICommunityService
{
    Task<CommunityPostPage> GetPostsAsync(
        string currentUserId,
        string? search,
        CommunityCategory? category,
        string sort,
        bool myPostsOnly,
        int page,
        int pageSize);

    Task<CommunityPost?> GetPostAsync(int postId, bool trackChanges = false);
    Task<CommunityComment?> GetCommentAsync(int commentId, bool trackChanges = false);
    Task AddPostAsync(CommunityPost post);
    Task AddCommentAsync(CommunityComment comment);
    Task<bool> TogglePostLikeAsync(int postId, string userId);
    Task<bool> ToggleCommentLikeAsync(int commentId, string userId);
    void RemovePost(CommunityPost post);
    void RemoveComment(CommunityComment comment);
    Task SaveChangesAsync();
}

public sealed record CommunityPostPage(
    IReadOnlyList<CommunityPost> Items,
    int TotalCount);
