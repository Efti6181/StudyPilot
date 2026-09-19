using System.ComponentModel.DataAnnotations;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public sealed class SmartStudyPlanIndexViewModel : StudentShellViewModel
{
    public IReadOnlyList<SmartStudyPlanSummaryViewModel> Plans { get; set; } = [];
    public int AvailablePeriodCount { get; set; }
}

public sealed class SmartStudyPlanSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string CareerGoal { get; set; } = string.Empty;
    public decimal WeeklyStudyHours { get; set; }
    public int CourseCount { get; set; }
    public int SessionCount { get; set; }
    public bool IsActive { get; set; }
    public bool UsedAiAnalysis { get; set; }
    public string AnalysisProvider { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SmartStudyPlanCreateViewModel : StudentShellViewModel
{
    [Required(ErrorMessage = "Select a semester.")]
    [Display(Name = "Semester courses")]
    public string PeriodKey { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    [Display(Name = "Career goal")]
    public string CareerGoal { get; set; } = string.Empty;

    [Display(Name = "Study days")]
    public List<DayOfWeek> StudyDays { get; set; } =
        [DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday];

    [Required, DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeOnly PreferredStartTime { get; set; } = new(18, 0);

    [Required, DataType(DataType.Time)]
    [Display(Name = "End time")]
    public TimeOnly PreferredEndTime { get; set; } = new(22, 0);

    [Required, Range(typeof(decimal), "1", "60")]
    [Display(Name = "Study hours each week")]
    public decimal WeeklyStudyHours { get; set; } = 9m;

    [Required]
    [Display(Name = "Session length")]
    public int SessionMinutes { get; set; } = 60;

    [Required, Range(0, 30)]
    [Display(Name = "Break between sessions")]
    public int BreakMinutes { get; set; } = 10;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Confirm that you entered all courses for this semester.")]
    [Display(Name = "I confirm that all courses for this semester are entered")]
    public bool ConfirmCoursesComplete { get; set; }

    public IReadOnlyList<StudyPlanPeriod> Periods { get; set; } = [];
    public bool IsAiConfigured { get; set; }
}

public sealed class SmartStudyPlanDetailsViewModel : StudentShellViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string CareerGoal { get; set; } = string.Empty;
    public string StudyDays { get; set; } = string.Empty;
    public string PreferredWindow { get; set; } = string.Empty;
    public decimal WeeklyStudyHours { get; set; }
    public int SessionMinutes { get; set; }
    public int BreakMinutes { get; set; }
    public bool UsedAiAnalysis { get; set; }
    public string AnalysisProvider { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public IReadOnlyList<SmartStudyDayViewModel> Schedule { get; set; } = [];
    public IReadOnlyList<SmartStudyCourseViewModel> Courses { get; set; } = [];
}

public sealed class SmartStudyDayViewModel
{
    public DayOfWeek Day { get; set; }
    public IReadOnlyList<SmartStudySessionViewModel> Sessions { get; set; } = [];
}

public sealed class SmartStudySessionViewModel
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public sealed class SmartStudyCourseViewModel
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public CourseDifficultyLevel Difficulty { get; set; }
    public CareerRelevanceLevel CareerRelevance { get; set; }
    public string AnalysisReason { get; set; } = string.Empty;
    public int WeeklyMinutes { get; set; }
    public int SessionCount { get; set; }
}
