using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudyPilotApp.Areas.Admin.ViewModels;

public sealed class AdminUserIndexViewModel : AdminShellViewModel
{
    public IReadOnlyList<AdminUserRowViewModel> Users { get; set; } = [];
    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public string? Search { get; set; }
    public string Role { get; set; } = "all";
    public string Status { get; set; } = "all";
    public int? DepartmentId { get; set; }
    public int TotalCount { get; set; }
    public int StudentCount { get; set; }
    public int FacultyCount { get; set; }
    public int AdminCount { get; set; }
    public int ActiveCount { get; set; }
    public int DisabledCount { get; set; }
    public int FilteredCount { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}

public sealed class AdminUserRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? UniversityId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string Initials { get; set; } = string.Empty;
}

public sealed class AdminUserDetailsViewModel : AdminShellViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsProtectedAdmin => Role == "Admin";
    public bool IsCurrentAdmin { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeactivatedAt { get; set; }
    public DateTimeOffset? AccountStatusChangedAt { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsTemporarilyLocked => LockoutEnd > DateTimeOffset.UtcNow;
    public string? UniversityId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? ProgramCode { get; set; }
    public string? ProgramName { get; set; }
    public string? Batch { get; set; }
    public int? CurrentSemester { get; set; }
    public string UserInitials { get; set; } = string.Empty;
}
