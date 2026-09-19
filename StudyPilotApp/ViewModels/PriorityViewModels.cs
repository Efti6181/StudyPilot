using System.ComponentModel.DataAnnotations;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class PriorityListViewModel : StudentShellViewModel
{
    public IReadOnlyList<PriorityCourseViewModel> Courses { get; set; } = [];
    public IReadOnlyList<PriorityCourseViewModel> TodayFocus { get; set; } = [];
    public string? Search { get; set; }
    public PriorityLevel? Level { get; set; }
    public string Sort { get; set; } = "priority";
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int DueThisWeekCount { get; set; }
    public int PendingAssessmentCount { get; set; }
}

public sealed class PriorityCourseViewModel
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal CreditHours { get; set; }
    public int CourseProgress { get; set; }
    public string? TargetGrade { get; set; }
    public decimal Score { get; set; }
    public PriorityLevel CalculatedLevel { get; set; }
    public PriorityLevel DisplayLevel { get; set; }
    public bool IsManualOverride { get; set; }
    public bool IsPinned { get; set; }
    public int SuggestedStudyMinutes { get; set; }
    public int PendingAssessments { get; set; }
    public DateTime? NearestDeadline { get; set; }
    public decimal? AssessmentAverage { get; set; }
    public int ConfidenceRating { get; set; }
    public int TopicCompletionPercentage { get; set; }
    public int WorkloadRisk { get; set; }
    public decimal AvailableStudyHoursPerWeek { get; set; }
    public string? Notes { get; set; }
    public PriorityFactorViewModel Factors { get; set; } = new();
    public IReadOnlyList<string> Reasons { get; set; } = [];

    public string LevelCss => DisplayLevel.ToString().ToLowerInvariant();
    public bool IsOverdue => NearestDeadline.HasValue && NearestDeadline.Value < DateTime.Now;
    public int? DaysUntilDeadline => NearestDeadline.HasValue
        ? (int)Math.Ceiling((NearestDeadline.Value - DateTime.Now).TotalDays)
        : null;
}

public sealed class PriorityFactorViewModel
{
    public decimal TargetGradeGap { get; set; }
    public decimal AssessmentUrgency { get; set; }
    public decimal CourseCredit { get; set; }
    public decimal Weakness { get; set; }
    public decimal IncompleteTopics { get; set; }
    public decimal WorkloadRisk { get; set; }
}

public sealed class PriorityConfigureViewModel : StudentShellViewModel
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int CourseProgress { get; set; }
    public int PendingAssessments { get; set; }
    public DateTime? NearestDeadline { get; set; }
    public decimal? AssessmentAverage { get; set; }

    [Required, Range(1, 5)]
    [Display(Name = "Confidence Rating")]
    public int ConfidenceRating { get; set; } = 3;

    [Required, Range(0, 100)]
    [Display(Name = "Topic Completion")]
    public int TopicCompletionPercentage { get; set; } = 50;

    [Required, Range(1, 5)]
    [Display(Name = "Workload Risk")]
    public int WorkloadRisk { get; set; } = 3;

    [Required, Range(typeof(decimal), "0", "168")]
    [Display(Name = "Available Study Hours Per Week")]
    public decimal AvailableStudyHoursPerWeek { get; set; } = 5m;

    [Display(Name = "Manual Priority Override")]
    public PriorityLevel? ManualPriorityLevel { get; set; }

    [Display(Name = "Pin to Today's Focus")]
    public bool IsPinned { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class PriorityDetailsViewModel : StudentShellViewModel
{
    public PriorityCourseViewModel Priority { get; set; } = new();
    public PriorityWeightSettingsViewModel Weights { get; set; } = new();
}

public sealed class PriorityWeightSettingsViewModel : StudentShellViewModel
{
    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Target-grade gap")]
    public decimal TargetGradeGapWeight { get; set; }

    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Assessment urgency")]
    public decimal AssessmentUrgencyWeight { get; set; }

    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Course credit")]
    public decimal CourseCreditWeight { get; set; }

    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Confidence / performance weakness")]
    public decimal WeaknessWeight { get; set; }

    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Incomplete topics")]
    public decimal IncompleteTopicsWeight { get; set; }

    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Workload risk")]
    public decimal WorkloadRiskWeight { get; set; }

    public decimal TotalWeight => TargetGradeGapWeight + AssessmentUrgencyWeight +
                                  CourseCreditWeight + WeaknessWeight +
                                  IncompleteTopicsWeight + WorkloadRiskWeight;
}
