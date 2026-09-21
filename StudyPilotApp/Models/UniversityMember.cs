using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class UniversityMember
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string UniversityId { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string NormalizedEmail { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Role { get; set; } = string.Empty;

    [StringLength(150)]
    public string? FullName { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? ProgramId { get; set; }
    public AcademicProgram? Program { get; set; }

    [StringLength(30)]
    public string? Batch { get; set; }

    [Range(1, 12)]
    public int? CurrentSemester { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsClaimed { get; set; }
    public string? ApplicationUserId { get; set; }
    public ApplicationUser? RegisteredUser { get; set; }
    public string? CreatedByAdminId { get; set; }
    public ApplicationUser? CreatedByAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
