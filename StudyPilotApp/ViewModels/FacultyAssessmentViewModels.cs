using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public sealed class FacultyAssessmentIndexViewModel : FacultyShellViewModel
{
    public IReadOnlyList<FacultyAssessmentData> Items { get; set; } = [];
    public IReadOnlyList<FacultyAssessmentCourseOption> Courses { get; set; } = [];
    public string? Search { get; set; }
    public int? AssignmentId { get; set; }
    public FacultyAssessmentStatus? Status { get; set; }
}

public sealed class FacultyAssessmentFormViewModel : FacultyShellViewModel
{
    public int? Id { get; set; }
    [Required, Display(Name = "Course and section")] public int? FacultyCourseAssignmentId { get; set; }
    public IReadOnlyList<FacultyAssessmentCourseOption> Courses { get; set; } = [];
    [Required, StringLength(160, MinimumLength = 2)] public string Title { get; set; } = string.Empty;
    [Required] public AssessmentType? Type { get; set; }
    [StringLength(4000)] public string? Description { get; set; }
    [StringLength(4000)] public string? Instructions { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Assigned date")] public DateOnly? AssignedDate { get; set; }
    [Required, DataType(DataType.DateTime), Display(Name = "Due date and time")] public DateTime? DueDate { get; set; }
    [Range(typeof(decimal), "0", "100000"), Display(Name = "Total marks")] public decimal? TotalMarks { get; set; }
    [Range(typeof(decimal), "0", "100"), Display(Name = "Weight / importance (%)")] public decimal? WeightPercentage { get; set; }
    [Required, Display(Name = "Estimated difficulty")] public AssessmentDifficulty? Difficulty { get; set; }
    [Display(Name = "Attachment")] public IFormFile? Attachment { get; set; }
    public bool RemoveAttachment { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentFileName { get; set; }
    public FacultyAssessmentStatus CurrentStatus { get; set; }
}

public sealed class FacultyAssessmentDetailsViewModel : FacultyShellViewModel
{
    public FacultyAssessment Assessment { get; set; } = null!;
    public int StudentCount { get; set; }
}
