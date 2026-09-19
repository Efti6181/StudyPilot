using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum AcademicAIMode
{
    [Display(Name = "Ask StudyPilot")]
    Ask = 0,

    [Display(Name = "Weekly Review")]
    WeeklyReview = 1,

    [Display(Name = "Assessment Breakdown")]
    AssessmentBreakdown = 2,

    [Display(Name = "Study Plan")]
    StudyPlan = 3,

    [Display(Name = "Notes, Questions & Flashcards")]
    MaterialHelper = 4
}

public enum AcademicAIMessageRole
{
    User = 0,
    Assistant = 1
}

public sealed class AcademicAIConversation
{
    public long Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    public AcademicAIMode Mode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<AcademicAIMessage> Messages { get; set; } = [];
}

public sealed class AcademicAIMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public AcademicAIConversation Conversation { get; set; } = null!;
    public AcademicAIMessageRole Role { get; set; }

    [Required, StringLength(12000)]
    public string Content { get; set; } = string.Empty;

    public bool IsFallback { get; set; }

    [StringLength(80)]
    public string? Provider { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
