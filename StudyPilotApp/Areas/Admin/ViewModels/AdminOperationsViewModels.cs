using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class AdminReportsViewModel : AdminShellViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalCourses { get; set; }
    public int CompletedAssessments { get; set; }
    public int TotalAssessments { get; set; }
    public int UpcomingEvents { get; set; }
    public int PublishedAnnouncements { get; set; }
    public int CommunityInteractions { get; set; }
    public decimal AverageCourseProgress { get; set; }
    public IReadOnlyList<string> GrowthLabels { get; set; } = [];
    public IReadOnlyList<int> StudentGrowth { get; set; } = [];
    public IReadOnlyList<int> FacultyGrowth { get; set; } = [];
    public IReadOnlyList<string> RoleLabels { get; set; } = [];
    public IReadOnlyList<int> RoleCounts { get; set; } = [];
    public IReadOnlyList<string> ContentLabels { get; set; } = [];
    public IReadOnlyList<int> ContentCounts { get; set; } = [];
    public IReadOnlyList<ReportSummaryRowViewModel> SummaryRows { get; set; } = [];
}

public sealed class ReportSummaryRowViewModel
{
    public string Area { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}

public sealed class AdminAuditIndexViewModel : AdminShellViewModel
{
    public string? Search { get; set; }
    public string? ControllerFilter { get; set; }
    public string Status { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int FilteredCount { get; set; }
    public int TotalCount { get; set; }
    public int TodayCount { get; set; }
    public int FailedCount { get; set; }
    public int UniqueAdminCount { get; set; }
    public IReadOnlyList<string> Controllers { get; set; } = [];
    public IReadOnlyList<AdminAuditRowViewModel> Items { get; set; } = [];
}

public sealed class AdminAuditRowViewModel
{
    public long Id { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public bool Succeeded { get; set; }
    public int StatusCode { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PlatformSettingsViewModel : AdminShellViewModel
{
    [Required, StringLength(150)]
    [Display(Name = "Institution name")]
    public string InstitutionName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Support email")]
    public string SupportEmail { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Time zone")]
    public string TimeZoneId { get; set; } = "Asia/Dhaka";

    [Required, StringLength(30)]
    [Display(Name = "Date format")]
    public string DateFormat { get; set; } = "dd MMM yyyy";

    [Range(5, 100)]
    [Display(Name = "Default page size")]
    public int DefaultPageSize { get; set; } = 10;

    [Range(30, 3650)]
    [Display(Name = "Audit retention (days)")]
    public int AuditRetentionDays { get; set; } = 365;

    [Display(Name = "Student registration")]
    public bool StudentRegistrationEnabled { get; set; }

    [Display(Name = "Faculty registration")]
    public bool FacultyRegistrationEnabled { get; set; }

    [Display(Name = "Academic AI")]
    public bool AcademicAiEnabled { get; set; }

    [Display(Name = "Student community")]
    public bool CommunityEnabled { get; set; }

    [Display(Name = "Campus events")]
    public bool EventsEnabled { get; set; }

    [Display(Name = "Show maintenance notice")]
    public bool MaintenanceNoticeEnabled { get; set; }

    [StringLength(500)]
    [Display(Name = "Maintenance notice")]
    public string? MaintenanceNotice { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }
}
