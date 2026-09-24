using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IFacultyResourceService
{
    Task<IReadOnlyList<FacultyResourceData>> SearchAsync(string userId, string? search, int? assignmentId, ResourceCategory? category, FacultyResourceStatus? status, CancellationToken token = default);
    Task<FacultyResource?> GetOwnedAsync(string userId, int id, bool track = false, CancellationToken token = default);
    Task<int> CreateAsync(string userId, FacultyResourceInput input, StoredResourceFile? file, CancellationToken token = default);
    Task<bool> UpdateAsync(string userId, int id, FacultyResourceInput input, StoredResourceFile? file, CancellationToken token = default);
    Task<FacultyResourcePublishResult> PublishAsync(string userId, int id, CancellationToken token = default);
    Task<bool> ArchiveAsync(string userId, int id, CancellationToken token = default);
    Task<FacultyResource?> DeleteDraftAsync(string userId, int id, CancellationToken token = default);
}

public sealed record FacultyResourceInput(int AssignmentId, string Title, string? Description, ResourceKind Kind, ResourceCategory Category, string? Tags, string? ExternalUrl);
public sealed record FacultyResourceData(int Id, int AssignmentId, string CourseCode, string CourseName, AcademicTerm Term, int AcademicYear, string Section, string Title, string? Description, ResourceKind Kind, ResourceCategory Category, FacultyResourceStatus Status, int StudentCount, DateTimeOffset CreatedAt);
public sealed record FacultyResourcePublishResult(bool Succeeded, int StudentCount, string? Error = null);
