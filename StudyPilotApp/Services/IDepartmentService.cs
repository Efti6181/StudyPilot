namespace StudyPilotApp.Services;

public interface IDepartmentService
{
    Task<DepartmentListData> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<DepartmentData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<DepartmentSaveResult> CreateAsync(DepartmentInput input, CancellationToken cancellationToken = default);
    Task<DepartmentSaveResult> UpdateAsync(int id, DepartmentInput input, CancellationToken cancellationToken = default);
    Task<DepartmentSaveResult> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}

public sealed record DepartmentInput(string Code, string Name, string? Description, bool IsActive);

public sealed record DepartmentData(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record DepartmentListData(
    IReadOnlyList<DepartmentData> Items,
    int TotalCount,
    int FilteredCount,
    int ActiveCount,
    int InactiveCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record DepartmentSaveResult(bool Succeeded, int? Id = null, string? Error = null)
{
    public static DepartmentSaveResult Success(int id) => new(true, id);
    public static DepartmentSaveResult Failure(string error) => new(false, null, error);
}
