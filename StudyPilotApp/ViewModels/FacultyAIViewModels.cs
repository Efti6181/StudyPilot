using System.ComponentModel.DataAnnotations;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class FacultyAIIndexViewModel : FacultyShellViewModel
{
    public long? ConversationId { get; set; }
    public IReadOnlyList<FacultyAIConversationItemViewModel> Conversations { get; set; } = [];
    public IReadOnlyList<AcademicAIMessageViewModel> Messages { get; set; } = [];
    public IReadOnlyList<FacultyAISelectItemViewModel> Courses { get; set; } = [];
    public bool IsProviderConfigured { get; set; }
    public string ActiveConversationTitle { get; set; } = "New teaching conversation";
    public FacultyAIMode ActiveMode { get; set; } = FacultyAIMode.Ask;
}

public sealed class FacultyAIAskViewModel
{
    public long? ConversationId { get; set; }

    [Required]
    public FacultyAIMode Mode { get; set; } = FacultyAIMode.Ask;

    [Required, StringLength(6000, MinimumLength = 2)]
    [Display(Name = "Faculty request")]
    public string Prompt { get; set; } = string.Empty;

    [Display(Name = "Course context")]
    public int? AssignmentId { get; set; }
}

public sealed class FacultyAIConversationItemViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class FacultyAISelectItemViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
