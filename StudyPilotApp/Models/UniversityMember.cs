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

    public bool IsActive { get; set; } = true;
    public bool IsClaimed { get; set; }
    public string? ApplicationUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
