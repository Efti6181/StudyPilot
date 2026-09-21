namespace StudyPilotApp.Services;

public interface IRegistrationAuthorizationService
{
    Task<RegistrationAuthorizationListData> SearchAsync(
        string? search,
        string? role,
        string status,
        int? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<RegistrationAuthorizationData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthorizationDepartmentData>> GetDepartmentsAsync(
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthorizationProgramData>> GetProgramsAsync(
        int departmentId,
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default);
    Task<RegistrationAuthorizationSaveResult> CreateAsync(
        RegistrationAuthorizationInput input,
        string adminUserId,
        CancellationToken cancellationToken = default);
    Task<RegistrationAuthorizationSaveResult> UpdateAsync(
        int id,
        RegistrationAuthorizationInput input,
        CancellationToken cancellationToken = default);
    Task<RegistrationAuthorizationSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}

public sealed record RegistrationAuthorizationInput(
    string UniversityId,
    string Email,
    string Role,
    string FullName,
    int DepartmentId,
    int? ProgramId,
    string? Batch,
    int? CurrentSemester,
    bool IsActive);

public sealed record RegistrationAuthorizationData(
    int Id,
    string UniversityId,
    string Email,
    string Role,
    string? FullName,
    int? DepartmentId,
    string? DepartmentCode,
    string? DepartmentName,
    int? ProgramId,
    string? ProgramCode,
    string? ProgramName,
    string? Batch,
    int? CurrentSemester,
    bool IsActive,
    bool IsClaimed,
    string? ApplicationUserId,
    string? RegisteredUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RegisteredAt,
    DateTimeOffset? UpdatedAt);

public sealed record RegistrationAuthorizationListData(
    IReadOnlyList<RegistrationAuthorizationData> Items,
    int TotalCount,
    int StudentCount,
    int FacultyCount,
    int RegisteredCount,
    int UnregisteredCount,
    int DisabledCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record AuthorizationDepartmentData(int Id, string Code, string Name, bool IsActive);
public sealed record AuthorizationProgramData(int Id, string Code, string Name, int DepartmentId, bool IsActive);

public sealed record RegistrationAuthorizationSaveResult(bool Succeeded, int? Id = null, string? Error = null)
{
    public static RegistrationAuthorizationSaveResult Success(int id) => new(true, id);
    public static RegistrationAuthorizationSaveResult Failure(string error) => new(false, null, error);
}
