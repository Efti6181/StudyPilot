using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudyPilotApp.Models;

namespace StudyPilotApp.ViewModels;

public sealed class AssessmentListViewModel : StudentShellViewModel
{
    public IReadOnlyList<AssessmentCardViewModel> Assessments { get; set; } = [];
    public IReadOnlyList<SelectListItem> CourseOptions { get; set; } = [];
    public string? Search { get; set; }
    public int? CourseId { get; set; }
    public AssessmentStatus? Status { get; set; }
    public AssessmentType? Type { get; set; }
    public bool OverdueOnly { get; set; }
    public string Sort { get; set; } = "deadline";
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Upcoming { get; set; }
    public int Overdue { get; set; }
    public int DueThisWeek { get; set; }
}

public sealed class AssessmentCardViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public AssessmentType Type { get; set; }
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public int? FacultyAssessmentId { get; set; }
    public bool IsFacultyPublished => FacultyAssessmentId.HasValue;
    public bool HasFacultyAttachment { get; set; }
    public DateOnly AssignedDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? TotalMarks { get; set; }
    public decimal? ObtainedMarks { get; set; }
    public decimal? WeightPercentage { get; set; }
    public AssessmentStatus Status { get; set; }
    public AssessmentDifficulty Difficulty { get; set; }
    public decimal? EstimatedStudyHours { get; set; }
    public string? Notes { get; set; }
    public bool IsOverdue => Status != AssessmentStatus.Completed && DueDate < DateTime.Now;
    public string StatusCss => IsOverdue ? "overdue" : Status.ToString().ToLowerInvariant();
    public string StatusLabel => IsOverdue ? "Overdue" : Status switch
    {
        AssessmentStatus.NotStarted => "Not Started",
        AssessmentStatus.InProgress => "In Progress",
        _ => "Completed"
    };
    public string TypeLabel => Type switch
    {
        AssessmentType.FinalExam => "Final Exam",
        AssessmentType.ClassTest => "Class Test",
        _ => Type.ToString()
    };
    public string MarksLabel => TotalMarks.HasValue
        ? $"{(ObtainedMarks.HasValue ? ObtainedMarks.Value.ToString("0.##") : "—")} / {TotalMarks.Value:0.##}"
        : "Not set";
}

public sealed class AssessmentFormViewModel : StudentShellViewModel
{
    public int? Id { get; set; }
    public bool IsFacultyPublished { get; set; }

    [Required(ErrorMessage = "Please select a course.")]
    [Display(Name = "Course")]
    public int? CourseId { get; set; }

    public IReadOnlyList<SelectListItem> CourseOptions { get; set; } = [];

    [Required(ErrorMessage = "Please enter a title.")]
    [StringLength(160, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an assessment type.")]
    public AssessmentType? Type { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Assigned Date")]
    public DateOnly? AssignedDate { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Due Date and Time")]
    public DateTime? DueDate { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Total Marks")]
    public decimal? TotalMarks { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Obtained Marks")]
    public decimal? ObtainedMarks { get; set; }

    [Range(typeof(decimal), "0", "100")]
    [Display(Name = "Weight / Percentage")]
    public decimal? WeightPercentage { get; set; }

    [Required]
    public AssessmentStatus? Status { get; set; }

    [Required]
    public AssessmentDifficulty? Difficulty { get; set; }

    [Range(typeof(decimal), "0", "1000")]
    [Display(Name = "Estimated Study Hours")]
    public decimal? EstimatedStudyHours { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed class AssessmentDetailsViewModel : StudentShellViewModel
{
    public AssessmentCardViewModel Assessment { get; set; } = new();
}

public sealed class AssessmentDeleteViewModel : StudentShellViewModel
{
    public AssessmentCardViewModel Assessment { get; set; } = new();
}
