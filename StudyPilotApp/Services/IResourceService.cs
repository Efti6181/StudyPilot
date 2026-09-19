using Microsoft.AspNetCore.Http;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IResourceService
{
    Task<IReadOnlyList<StudyResource>> GetOwnedResourcesAsync(
        string userId,
        string? search,
        int? courseId,
        ResourceCategory? category,
        ResourceKind? kind,
        bool favoritesOnly,
        string sort);

    Task<ResourceSummary> GetSummaryAsync(string userId);
    Task<StudyResource?> GetOwnedResourceAsync(string userId, int resourceId, bool trackChanges = false);
    Task<bool> OwnsCourseAsync(string userId, int courseId);
    Task<IReadOnlyList<Course>> GetCourseOptionsAsync(string userId);
    Task<StoredResourceFile> StoreFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<Stream?> OpenFileAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task DeleteStoredFileAsync(string? storedFileName);
    Task AddAsync(StudyResource resource);
    Task SaveChangesAsync();
    void Remove(StudyResource resource);
}

public sealed record ResourceSummary(
    int Total,
    int Files,
    int Links,
    int Favorites,
    long TotalFileBytes);

public sealed record StoredResourceFile(
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes);
