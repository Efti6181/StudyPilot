using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class StudentProfile
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [StringLength(100)]
    public string? Department { get; set; }

    [Range(1, 12)]
    public int? Semester { get; set; }

    [StringLength(30)]
    public string? Batch { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(20)]
    public string? GuardianPhoneNumber { get; set; }

    [StringLength(250)]
    public string? PresentAddress { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    [StringLength(5)]
    public string? BloodGroup { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [MaxLength(2 * 1024 * 1024)]
    public byte[]? ProfileImageData { get; set; }

    [StringLength(50)]
    public string? ProfileImageContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
