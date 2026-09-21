using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public sealed class AdminAuditLog
{
    public long Id { get; set; }

    [Required]
    public string AdminUserId { get; set; } = string.Empty;
    public ApplicationUser AdminUser { get; set; } = null!;

    [Required, StringLength(150)]
    public string AdminName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Controller { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string HttpMethod { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string RequestPath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public bool Succeeded { get; set; }
    public int StatusCode { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PlatformSetting
{
    public int Id { get; set; } = 1;

    [Required, StringLength(150)]
    public string InstitutionName { get; set; } = "StudyPilot University";

    [Required, EmailAddress, StringLength(256)]
    public string SupportEmail { get; set; } = "support@studypilot.local";

    [Required, StringLength(100)]
    public string TimeZoneId { get; set; } = "Asia/Dhaka";

    [Required, StringLength(30)]
    public string DateFormat { get; set; } = "dd MMM yyyy";

    [Range(5, 100)]
    public int DefaultPageSize { get; set; } = 10;

    [Range(30, 3650)]
    public int AuditRetentionDays { get; set; } = 365;

    public bool StudentRegistrationEnabled { get; set; } = true;
    public bool FacultyRegistrationEnabled { get; set; } = true;
    public bool AcademicAiEnabled { get; set; } = true;
    public bool CommunityEnabled { get; set; } = true;
    public bool EventsEnabled { get; set; } = true;
    public bool MaintenanceNoticeEnabled { get; set; }

    [StringLength(500)]
    public string? MaintenanceNotice { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}
