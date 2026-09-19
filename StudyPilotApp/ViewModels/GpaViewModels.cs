using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class GpaDashboardViewModel : StudentShellViewModel
{
    public IReadOnlyList<SemesterSummaryViewModel> Semesters { get; set; } = [];
    public decimal CurrentCgpa { get; set; }
    public decimal ProjectedCgpa { get; set; }
    public decimal CompletedCredits { get; set; }
    public int CompletedCourses { get; set; }
    public decimal MaximumGradePoint { get; set; } = 4m;

    [Range(typeof(decimal), "0", "10")]
    public decimal? GoalCurrentCgpa { get; set; }

    [Range(typeof(decimal), "0", "1000")]
    public decimal? GoalCompletedCredits { get; set; }

    [Range(typeof(decimal), "0", "10")]
    public decimal? TargetCgpa { get; set; }

    [Range(typeof(decimal), "0.5", "500")]
    public decimal? FutureCredits { get; set; }

    public bool GoalCalculated { get; set; }
    public bool GoalIsPossible { get; set; }
    public decimal? RequiredGpa { get; set; }
    public string? GoalMessage { get; set; }
}

public sealed class SemesterSummaryViewModel
{
    public int Id { get; set; }
    public int SemesterNumber { get; set; }
    public AcademicTerm AcademicTerm { get; set; }
    public int AcademicYear { get; set; }
    public int CourseCount { get; set; }
    public decimal ActualCredits { get; set; }
    public decimal SemesterGpa { get; set; }
    public decimal ProjectedGpa { get; set; }
    public bool HasActualResults { get; set; }
}

public sealed class SemesterFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }

    [Required, Range(1, 12)]
    [Display(Name = "Semester Number")]
    public int? SemesterNumber { get; set; }

    [Required]
    [Display(Name = "Academic Term")]
    public AcademicTerm? AcademicTerm { get; set; }

    [Required, Range(2000, 2100)]
    [Display(Name = "Academic Year")]
    public int? AcademicYear { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class SemesterDetailsViewModel : StudentShellViewModel
{
    public int Id { get; set; }
    public int SemesterNumber { get; set; }
    public AcademicTerm AcademicTerm { get; set; }
    public int AcademicYear { get; set; }
    public string? Notes { get; set; }
    public decimal SemesterGpa { get; set; }
    public decimal ProjectedGpa { get; set; }
    public decimal ActualCredits { get; set; }
    public decimal PlannedCredits { get; set; }
    public IReadOnlyList<CourseGradeRowViewModel> Grades { get; set; } = [];
}

public sealed class CourseGradeRowViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal CreditHours { get; set; }
    public string ExpectedLetterGrade { get; set; } = string.Empty;
    public string? ActualLetterGrade { get; set; }
    public decimal ExpectedGradePoint { get; set; }
    public decimal? ActualGradePoint { get; set; }
    public string? Notes { get; set; }
}

public sealed class CourseGradeFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }
    public int SemesterResultId { get; set; }
    public string SemesterLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a course.")]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    public IReadOnlyList<SelectListItem> CourseOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> GradeOptions { get; set; } = [];

    [Required(ErrorMessage = "Please select an expected grade.")]
    [Display(Name = "Expected Grade")]
    public string ExpectedLetterGrade { get; set; } = string.Empty;

    [Display(Name = "Actual Grade")]
    public string? ActualLetterGrade { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public sealed class GpaCalculatorViewModel : StudentShellViewModel
{
    public List<GpaCalculatorRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<SelectListItem> GradeOptions { get; set; } = [];
    public bool HasResult { get; set; }
    public decimal CalculatedGpa { get; set; }
    public decimal TotalCredits { get; set; }
}

public sealed class GpaCalculatorRowViewModel
{
    [StringLength(100)]
    [Display(Name = "Course")]
    public string? CourseName { get; set; }

    [Range(typeof(decimal), "0.5", "20")]
    public decimal? Credits { get; set; }

    [StringLength(10)]
    public string? LetterGrade { get; set; }
}

public sealed class GradingScaleViewModel : StudentShellViewModel
{
    public List<GradingScaleRowViewModel> Entries { get; set; } = [];
}

public sealed class GradingScaleRowViewModel
{
    public int Id { get; set; }
    public string LetterGrade { get; set; } = string.Empty;

    [Required, Range(typeof(decimal), "0", "100")]
    [Display(Name = "Minimum %")]
    public decimal MinimumPercentage { get; set; }

    [Required, Range(typeof(decimal), "0", "10")]
    [Display(Name = "Grade Point")]
    public decimal GradePoint { get; set; }
}

public sealed class SemesterDeleteViewModel : StudentShellViewModel
{
    public int Id { get; set; }
    public string SemesterLabel { get; set; } = string.Empty;
    public int CourseCount { get; set; }
}
