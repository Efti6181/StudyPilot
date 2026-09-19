using System.ComponentModel.DataAnnotations;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class CommunityIndexViewModel : StudentShellViewModel
{
    public IReadOnlyList<CommunityPostCardViewModel> Posts { get; set; } = [];
    public string? Search { get; set; }
    public CommunityCategory? Category { get; set; }
    public string Sort { get; set; } = "newest";
    public bool MyPostsOnly { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPosts { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalPosts / (double)PageSize));
}

public sealed class CommunityPostCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public CommunityCategory Category { get; set; }
    public string AuthorName { get; set; } = "Student";
    public string AuthorInitials { get; set; } = "ST";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }

    public string Preview => Content.Length <= 260 ? Content : $"{Content[..260]}…";
    public string CategoryLabel => Category switch
    {
        CommunityCategory.General => "General Discussion",
        CommunityCategory.CourseHelp => "Course Help",
        CommunityCategory.StudyTips => "Study Tips",
        CommunityCategory.Projects => "Projects & Collaboration",
        CommunityCategory.Career => "Career & Skills",
        CommunityCategory.CampusLife => "Campus Life",
        _ => "Announcements"
    };
}

public sealed class CommunityPostFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please enter a post title.")]
    [StringLength(160, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write the post content.")]
    [StringLength(8000, MinimumLength = 10)]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    public CommunityCategory? Category { get; set; }
}

public sealed class CommunityDetailsViewModel : StudentShellViewModel
{
    public CommunityPostCardViewModel Post { get; set; } = new();
    public IReadOnlyList<CommunityCommentViewModel> Comments { get; set; } = [];

    [Required(ErrorMessage = "Please write a comment.")]
    [StringLength(2000, MinimumLength = 2)]
    public string NewComment { get; set; } = string.Empty;
}

public sealed class CommunityCommentViewModel
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = "Student";
    public string AuthorInitials { get; set; } = "ST";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsOwnedByCurrentUser { get; set; }
}

public sealed class CommunityCommentEditViewModel : StudentShellViewModel
{
    public int Id { get; set; }
    public int PostId { get; set; }

    [Required(ErrorMessage = "Please write a comment.")]
    [StringLength(2000, MinimumLength = 2)]
    public string Content { get; set; } = string.Empty;
}

public sealed class CommunityDeleteViewModel : StudentShellViewModel
{
    public CommunityPostCardViewModel Post { get; set; } = new();
}
