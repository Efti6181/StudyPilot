using System.ComponentModel.DataAnnotations;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class AcademicAIIndexViewModel : StudentShellViewModel
{
    public long? ConversationId { get; set; }
    public IReadOnlyList<AcademicAIConversationItemViewModel> Conversations { get; set; } = [];
    public IReadOnlyList<AcademicAIMessageViewModel> Messages { get; set; } = [];
    public IReadOnlyList<AcademicAISelectItemViewModel> Courses { get; set; } = [];
    public IReadOnlyList<AcademicAISelectItemViewModel> Assessments { get; set; } = [];
    public bool IsProviderConfigured { get; set; }
    public string ActiveConversationTitle { get; set; } = "New conversation";
    public AcademicAIMode ActiveMode { get; set; } = AcademicAIMode.Ask;
}

public sealed class AcademicAIAskViewModel
{
    public long? ConversationId { get; set; }

    [Required]
    public AcademicAIMode Mode { get; set; } = AcademicAIMode.Ask;

    [Required, StringLength(6000, MinimumLength = 2)]
    [Display(Name = "Your request")]
    public string Prompt { get; set; } = string.Empty;

    public int? CourseId { get; set; }
    public int? AssessmentId { get; set; }
}

public sealed class AcademicAIConversationItemViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public AcademicAIMode Mode { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AcademicAIMessageViewModel
{
    public long Id { get; set; }
    public AcademicAIMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsFallback { get; set; }
    public string? Provider { get; set; }
}

public sealed class AcademicAISelectItemViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
