using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface ICourseCatalogService
{
    Task<CatalogCourseListData> SearchAsync(
        string? search,
        int? departmentId,
        int? programId,
        CourseType? courseType,
        CourseDifficultyLevel? difficulty,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<CatalogCourseData?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogDepartmentData>> GetDepartmentsAsync(
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogProgramData>> GetProgramsAsync(
        int? departmentId,
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default);
    Task<CatalogCourseSaveResult> CreateAsync(
        CatalogCourseInput input,
        CancellationToken cancellationToken = default);
    Task<CatalogCourseSaveResult> UpdateAsync(
        int id,
        CatalogCourseInput input,
        CancellationToken cancellationToken = default);
    Task<CatalogCourseSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogCourseInput(
    string Code,
    string Name,
    int DepartmentId,
    int? ProgramId,
    decimal CreditHours,
    CourseType CourseType,
    int? RecommendedSemester,
    CourseDifficultyLevel Difficulty,
    CareerRelevanceLevel CareerRelevance,
    string? Description,
    string? Prerequisites,
    bool IsActive);

public sealed record CatalogCourseData(
    int Id,
    string Code,
    string Name,
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    int? ProgramId,
    string? ProgramCode,
    string? ProgramName,
    decimal CreditHours,
    CourseType CourseType,
    int? RecommendedSemester,
    CourseDifficultyLevel Difficulty,
    CareerRelevanceLevel CareerRelevance,
    string? Description,
    string? Prerequisites,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CatalogCourseListData(
    IReadOnlyList<CatalogCourseData> Items,
    int TotalCount,
    int ActiveCount,
    int InactiveCount,
    int CoreCount,
    int HardCount,
    int FilteredCount,
    int Page,
    int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
}

public sealed record CatalogDepartmentData(int Id, string Code, string Name, bool IsActive);
public sealed record CatalogProgramData(int Id, string Code, string Name, int DepartmentId, bool IsActive);

public sealed record CatalogCourseSaveResult(bool Succeeded, int? Id = null, string? Error = null)
{
    public static CatalogCourseSaveResult Success(int id) => new(true, id);
    public static CatalogCourseSaveResult Failure(string error) => new(false, null, error);
}
