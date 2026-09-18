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
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
