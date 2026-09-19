using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum CommunityCategory
{
    [Display(Name = "General Discussion")]
    General = 0,

    [Display(Name = "Course Help")]
    CourseHelp = 1,

    [Display(Name = "Study Tips")]
    StudyTips = 2,

    [Display(Name = "Projects & Collaboration")]
    Projects = 3,

    [Display(Name = "Career & Skills")]
    Career = 4,

    [Display(Name = "Campus Life")]
    CampusLife = 5,

    Announcements = 6
}

public sealed class CommunityPost
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(8000)]
    public string Content { get; set; } = string.Empty;

    public CommunityCategory Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<CommunityComment> Comments { get; set; } = [];
    public ICollection<CommunityPostLike> Likes { get; set; } = [];
}

public sealed class CommunityComment
{
    public int Id { get; set; }
    public int CommunityPostId { get; set; }
    public CommunityPost CommunityPost { get; set; } = null!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<CommunityCommentLike> Likes { get; set; } = [];
}

public sealed class CommunityPostLike
{
    public int CommunityPostId { get; set; }
    public CommunityPost CommunityPost { get; set; } = null!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CommunityCommentLike
{
    public int CommunityCommentId { get; set; }
    public CommunityComment CommunityComment { get; set; } = null!;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
