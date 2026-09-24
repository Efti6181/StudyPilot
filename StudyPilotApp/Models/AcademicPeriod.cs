using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public sealed class AcademicPeriod
{
    public int Id { get; set; }

    public AcademicTerm Term { get; set; }

    [Range(2000, 2100)]
    public int AcademicYear { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly? RegistrationStartDate { get; set; }
    public DateOnly? RegistrationEndDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<FacultyCourseAssignment> FacultyCourseAssignments { get; set; } = [];
}
