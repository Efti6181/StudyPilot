using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public sealed class Department
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string NormalizedCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string NormalizedName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<AcademicProgram> Programs { get; set; } = [];
    public ICollection<CatalogCourse> CatalogCourses { get; set; } = [];
    public ICollection<UniversityMember> AuthorizedMembers { get; set; } = [];
    public ICollection<StudentProfile> StudentProfiles { get; set; } = [];
}
