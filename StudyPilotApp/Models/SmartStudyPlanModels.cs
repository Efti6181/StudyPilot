using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum CourseDifficultyLevel
{
    Easy = 1,
    Moderate = 2,
    Hard = 3
}

public enum CareerRelevanceLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

public sealed class SmartStudyPlan
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Semester { get; set; }

    public AcademicTerm AcademicTerm { get; set; }

    [Range(2000, 2100)]
    public int AcademicYear { get; set; }

    [Required, StringLength(200)]
    public string CareerGoal { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string StudyDays { get; set; } = string.Empty;

    public TimeOnly PreferredStartTime { get; set; }
    public TimeOnly PreferredEndTime { get; set; }

    [Range(typeof(decimal), "1", "60")]
    public decimal WeeklyStudyHours { get; set; }

    [Range(25, 120)]
    public int SessionMinutes { get; set; }

    [Range(0, 30)]
    public int BreakMinutes { get; set; }

    public bool UsedAiAnalysis { get; set; }

    [StringLength(100)]
    public string AnalysisProvider { get; set; } = "Deterministic analysis";

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<SmartStudyPlanCourse> Courses { get; set; } = [];
}

public sealed class SmartStudyPlanCourse
{
    public int Id { get; set; }
    public int SmartStudyPlanId { get; set; }
    public SmartStudyPlan SmartStudyPlan { get; set; } = null!;

    public int CourseId { get; set; }

    [Required, StringLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string CourseName { get; set; } = string.Empty;

    public CourseDifficultyLevel Difficulty { get; set; }
    public CareerRelevanceLevel CareerRelevance { get; set; }

    [Required, StringLength(500)]
    public string AnalysisReason { get; set; } = string.Empty;

    public int WeeklyMinutes { get; set; }
    public decimal AllocationScore { get; set; }
    public ICollection<SmartStudySession> Sessions { get; set; } = [];
}

public sealed class SmartStudySession
{
    public int Id { get; set; }
    public int SmartStudyPlanCourseId { get; set; }
    public SmartStudyPlanCourse SmartStudyPlanCourse { get; set; } = null!;
    public DayOfWeek Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Sequence { get; set; }
}
