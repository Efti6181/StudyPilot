using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class FacultyProfile
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string FacultyId { get; set; } = string.Empty;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    [StringLength(100)]
    public string? Designation { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(150)]
    public string? OfficeLocation { get; set; }

    [StringLength(250)]
    public string? OfficeHours { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(1000)]
    public string? TeachingInterests { get; set; }

    [MaxLength(2 * 1024 * 1024)]
    public byte[]? ProfileImageData { get; set; }

    [StringLength(50)]
    public string? ProfileImageContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<FacultyCourseAssignment> CourseAssignments { get; set; } = [];
}
