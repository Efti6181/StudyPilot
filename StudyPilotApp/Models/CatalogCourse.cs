using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public sealed class CatalogCourse
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string NormalizedCode { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Name { get; set; } = string.Empty;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int? ProgramId { get; set; }
    public AcademicProgram? Program { get; set; }

    [Range(typeof(decimal), "0.5", "9")]
    public decimal CreditHours { get; set; }

    public CourseType CourseType { get; set; }

    [Range(1, 12)]
    public int? RecommendedSemester { get; set; }

    public CourseDifficultyLevel Difficulty { get; set; } = CourseDifficultyLevel.Moderate;
    public CareerRelevanceLevel CareerRelevance { get; set; } = CareerRelevanceLevel.Medium;

    [StringLength(1500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Prerequisites { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
