using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IAcademicPeriodService
{
    Task<AcademicPeriodListData> SearchAsync(
        string? search,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AcademicPeriodData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<AcademicPeriodSaveResult> CreateAsync(
        AcademicPeriodInput input,
        CancellationToken cancellationToken = default);
    Task<AcademicPeriodSaveResult> UpdateAsync(
        int id,
        AcademicPeriodInput input,
        CancellationToken cancellationToken = default);
    Task<AcademicPeriodSaveResult> SetCurrentAsync(
        int id,
        CancellationToken cancellationToken = default);
    Task<AcademicPeriodSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}

public sealed record AcademicPeriodInput(
    AcademicTerm Term,
    int AcademicYear,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? RegistrationStartDate,
    DateOnly? RegistrationEndDate,
    string? Notes,
    bool IsCurrent,
    bool IsActive);

public sealed record AcademicPeriodData(
    int Id,
    AcademicTerm Term,
    int AcademicYear,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly? RegistrationStartDate,
    DateOnly? RegistrationEndDate,
    string? Notes,
    bool IsCurrent,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public string Label => $"{Term} {AcademicYear}";
}

public sealed record AcademicPeriodListData(
    IReadOnlyList<AcademicPeriodData> Items,
    int TotalCount,
    int CurrentCount,
    int UpcomingCount,
    int PastCount,
    int InactiveCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record AcademicPeriodSaveResult(bool Succeeded, int? Id = null, string? Error = null)
{
    public static AcademicPeriodSaveResult Success(int id) => new(true, id);
    public static AcademicPeriodSaveResult Failure(string error) => new(false, null, error);
}
