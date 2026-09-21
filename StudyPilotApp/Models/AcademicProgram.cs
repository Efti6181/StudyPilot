using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public sealed class AcademicProgram
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string NormalizedCode { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string NormalizedName { get; set; } = string.Empty;

    [StringLength(1200)]
    public string? Description { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    [Range(typeof(decimal), "1", "500")]
    public decimal TotalCredits { get; set; }

    [Range(typeof(decimal), "0.5", "10")]
    public decimal DurationYears { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<UniversityMember> AuthorizedMembers { get; set; } = [];
    public ICollection<CatalogCourse> CatalogCourses { get; set; } = [];
}
