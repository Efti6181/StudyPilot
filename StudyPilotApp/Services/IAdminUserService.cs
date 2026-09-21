namespace StudyPilotApp.Services;

public interface IAdminUserService
{
    Task<AdminUserListData> SearchAsync(
        string? search,
        string? role,
        bool? isActive,
        int? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AdminUserData?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminUserStatusResult> SetActiveAsync(
        string id,
        bool isActive,
        string actingAdminId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminUserData(
    string Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeactivatedAt,
    DateTimeOffset? AccountStatusChangedAt,
    bool EmailConfirmed,
    string? PhoneNumber,
    int AccessFailedCount,
    DateTimeOffset? LockoutEnd,
    string? UniversityId,
    int? DepartmentId,
    string? DepartmentCode,
    string? DepartmentName,
    int? ProgramId,
    string? ProgramCode,
    string? ProgramName,
    string? Batch,
    int? CurrentSemester);

public sealed record AdminUserListData(
    IReadOnlyList<AdminUserData> Items,
    int TotalCount,
    int StudentCount,
    int FacultyCount,
    int AdminCount,
    int ActiveCount,
    int DisabledCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record AdminUserStatusResult(bool Succeeded, string? Error = null)
{
    public static AdminUserStatusResult Success() => new(true);
    public static AdminUserStatusResult Failure(string error) => new(false, error);
}
