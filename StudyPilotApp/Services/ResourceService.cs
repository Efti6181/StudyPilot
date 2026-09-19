using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class ResourceService : IResourceService
{
    public const long MaximumFileSize = 20 * 1024 * 1024;

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".txt", ".md", ".zip", ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".cs", ".java", ".py", ".js", ".html", ".css"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly string _storageRoot;
    private readonly FileExtensionContentTypeProvider _contentTypes = new();

    public ResourceService(ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _storageRoot = Path.Combine(environment.ContentRootPath, "App_Data", "ResourceFiles");
    }

    public async Task<IReadOnlyList<StudyResource>> GetOwnedResourcesAsync(
        string userId,
        string? search,
        int? courseId,
        ResourceCategory? category,
        ResourceKind? kind,
        bool favoritesOnly,
        string sort)
    {
        var query = _dbContext.StudyResources
            .AsNoTracking()
            .Include(item => item.Course)
            .Where(item => item.ApplicationUserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                (item.Description != null && EF.Functions.ILike(item.Description, pattern)) ||
                (item.Tags != null && EF.Functions.ILike(item.Tags, pattern)) ||
                (item.Course != null &&
                    (EF.Functions.ILike(item.Course.CourseCode, pattern) ||
                     EF.Functions.ILike(item.Course.CourseName, pattern))));
        }

        if (courseId.HasValue) query = query.Where(item => item.CourseId == courseId.Value);
        if (category.HasValue) query = query.Where(item => item.Category == category.Value);
        if (kind.HasValue) query = query.Where(item => item.Kind == kind.Value);
        if (favoritesOnly) query = query.Where(item => item.IsFavorite);

        query = sort switch
        {
            "oldest" => query.OrderBy(item => item.CreatedAt),
            "title" => query.OrderBy(item => item.Title),
            "course" => query.OrderBy(item => item.Course == null ? "" : item.Course.CourseCode)
                             .ThenBy(item => item.Title),
            "category" => query.OrderBy(item => item.Category).ThenBy(item => item.Title),
            _ => query.OrderByDescending(item => item.CreatedAt)
        };

        return await query.ToListAsync();
    }

    public async Task<ResourceSummary> GetSummaryAsync(string userId)
    {
        var query = _dbContext.StudyResources.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId);

        return new ResourceSummary(
            await query.CountAsync(),
            await query.CountAsync(item => item.Kind == ResourceKind.File),
            await query.CountAsync(item => item.Kind == ResourceKind.Link),
            await query.CountAsync(item => item.IsFavorite),
            await query.Where(item => item.FileSizeBytes.HasValue)
                .SumAsync(item => item.FileSizeBytes ?? 0));
    }

    public Task<StudyResource?> GetOwnedResourceAsync(string userId, int resourceId, bool trackChanges = false)
    {
        var query = _dbContext.StudyResources
            .Include(item => item.Course)
            .Where(item => item.Id == resourceId && item.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public Task<bool> OwnsCourseAsync(string userId, int courseId) =>
        _dbContext.Courses.AnyAsync(item => item.Id == courseId && item.ApplicationUserId == userId);

    public async Task<IReadOnlyList<Course>> GetCourseOptionsAsync(string userId) =>
        await _dbContext.Courses.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .OrderByDescending(item => item.Status == CourseStatus.Active)
            .ThenBy(item => item.CourseCode)
            .ToListAsync();

    public async Task<StoredResourceFile> StoreFileAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("This file type is not allowed.");
        if (file.Length <= 0 || file.Length > MaximumFileSize)
            throw new InvalidOperationException("The file must be between 1 byte and 20 MB.");

        Directory.CreateDirectory(_storageRoot);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var path = GetSafePath(storedName);

        await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(output, cancellationToken);
        }

        var contentType = _contentTypes.TryGetContentType(originalName, out var detected)
            ? detected
            : "application/octet-stream";

        return new StoredResourceFile(originalName, storedName, contentType, file.Length);
    }

    public Task<Stream?> OpenFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var path = GetSafePath(storedFileName);
        Stream? stream = System.IO.File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteStoredFileAsync(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)) return Task.CompletedTask;
        var path = GetSafePath(storedFileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task AddAsync(StudyResource resource) =>
        await _dbContext.StudyResources.AddAsync(resource);

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
    public void Remove(StudyResource resource) => _dbContext.StudyResources.Remove(resource);

    private string GetSafePath(string storedFileName)
    {
        if (Path.GetFileName(storedFileName) != storedFileName)
            throw new InvalidOperationException("Invalid stored filename.");

        var fullRoot = Path.GetFullPath(_storageRoot) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, storedFileName));
        if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid stored filename.");
        return fullPath;
    }
}
