namespace StudyPilotApp.Services;

public interface IAcademicProgramService
{
    Task<AcademicProgramListData> SearchAsync(
        string? search,
        int? departmentId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AcademicProgramData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProgramDepartmentData>> GetDepartmentsAsync(
        bool activeOnly,
        int? includeDepartmentId = null,
        CancellationToken cancellationToken = default);
    Task<AcademicProgramSaveResult> CreateAsync(AcademicProgramInput input, CancellationToken cancellationToken = default);
    Task<AcademicProgramSaveResult> UpdateAsync(int id, AcademicProgramInput input, CancellationToken cancellationToken = default);
    Task<AcademicProgramSaveResult> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}

public sealed record AcademicProgramInput(
    string Code,
    string Name,
    string? Description,
    int DepartmentId,
    decimal TotalCredits,
    decimal DurationYears,
    bool IsActive);

public sealed record AcademicProgramData(
    int Id,
    string Code,
    string Name,
    string? Description,
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    decimal TotalCredits,
    decimal DurationYears,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ProgramDepartmentData(int Id, string Code, string Name, bool IsActive);

public sealed record AcademicProgramListData(
    IReadOnlyList<AcademicProgramData> Items,
    int TotalCount,
    int FilteredCount,
    int ActiveCount,
    int InactiveCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record AcademicProgramSaveResult(bool Succeeded, int? Id = null, string? Error = null)
{
    public static AcademicProgramSaveResult Success(int id) => new(true, id);
    public static AcademicProgramSaveResult Failure(string error) => new(false, null, error);
}
