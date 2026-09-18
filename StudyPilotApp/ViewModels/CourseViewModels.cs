using System.ComponentModel.DataAnnotations;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class CourseListViewModel : StudentShellViewModel
{
    public IReadOnlyList<CourseCardViewModel> Courses { get; set; } = [];
    public string? Search { get; set; }
    public CourseStatus? Status { get; set; }
    public CourseType? Type { get; set; }
    public string Sort { get; set; } = "code";
    public int ActiveCourses { get; set; }
    public decimal ActiveCredits { get; set; }
    public double AverageProgress { get; set; }
    public int NeedsAttention { get; set; }
}

public sealed class CourseCardViewModel
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public decimal CreditHours { get; set; }
    public string Instructor { get; set; } = "Instructor not set";
    public string InstructorInitials { get; set; } = "IN";
    public int Semester { get; set; }
    public AcademicTerm AcademicTerm { get; set; }
    public int AcademicYear { get; set; }
    public CourseType CourseType { get; set; }
    public CourseStatus Status { get; set; }
    public CourseColor Color { get; set; }
    public int ProgressPercentage { get; set; }
    public string? TargetGrade { get; set; }
    public string? Description { get; set; }

    public string ColorCss => Color.ToString().ToLowerInvariant();
    public string ProgressState => ProgressPercentage < 60 ? "risk" : ProgressPercentage >= 75 ? "strong" : "normal";
}

public sealed class CourseFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please enter the course code.")]
    [StringLength(20, MinimumLength = 2)]
    [Display(Name = "Course Code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the course name.")]
    [StringLength(150, MinimumLength = 2)]
    [Display(Name = "Course Name")]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.5", "9.0", ErrorMessage = "Credit hours must be between 0.5 and 9.0.")]
    [Display(Name = "Credit Hours")]
    public decimal? CreditHours { get; set; }

    [StringLength(150)]
    public string? Instructor { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "Please select a valid semester.")]
    public int? Semester { get; set; }

    [Required(ErrorMessage = "Please select an academic term.")]
    [Display(Name = "Academic Term")]
    public AcademicTerm? AcademicTerm { get; set; }

    [Required]
    [Range(2000, 2100, ErrorMessage = "Please enter a valid academic year.")]
    [Display(Name = "Academic Year")]
    public int? AcademicYear { get; set; }

    [Required(ErrorMessage = "Please select a course type.")]
    [Display(Name = "Course Type")]
    public CourseType? CourseType { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Target Grade")]
    public string? TargetGrade { get; set; }

    [Required]
    public CourseStatus? Status { get; set; }

    [Required]
    [Display(Name = "Card Color")]
    public CourseColor? Color { get; set; }

    [Required]
    [Range(0, 100)]
    [Display(Name = "Current Progress")]
    public int? ProgressPercentage { get; set; }
}

public sealed class CourseDetailsViewModel : StudentShellViewModel
{
    public CourseCardViewModel Course { get; set; } = new();
    public int AssessmentCount { get; set; }
}

public sealed class CourseDeleteViewModel : StudentShellViewModel
{
    public CourseCardViewModel Course { get; set; } = new();
    public int RelatedAssessmentCount { get; set; }
}
