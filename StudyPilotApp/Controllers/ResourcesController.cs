using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class ResourcesController : Controller
{
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "newest", "oldest", "title", "course", "category" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IResourceService _resourceService;

    public ResourcesController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IResourceService resourceService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? courseId,
        ResourceCategory? category,
        ResourceKind? kind,
        bool favoritesOnly = false,
        string sort = "newest")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = NormalizeSearch(search);
        category = IsDefined(category) ? category : null;
        kind = IsDefined(kind) ? kind : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "newest";
        if (courseId.HasValue && !await _resourceService.OwnsCourseAsync(user.Id, courseId.Value))
            courseId = null;

        var resources = await _resourceService.GetOwnedResourcesAsync(
            user.Id, search, courseId, category, kind, favoritesOnly, sort);
        var summary = await _resourceService.GetSummaryAsync(user.Id);

        var model = new ResourceListViewModel
        {
            Resources = resources.Select(ToCard).ToList(),
            CourseOptions = await BuildCourseOptionsAsync(user.Id, courseId),
            Search = search,
            CourseId = courseId,
            Category = category,
            Kind = kind,
            FavoritesOnly = favoritesOnly,
            Sort = sort,
            Total = summary.Total,
            Files = summary.Files,
            Links = summary.Links,
            Favorites = summary.Favorites,
            TotalFileBytes = summary.TotalFileBytes
        };

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id);
        if (resource is null) return NotFound();

        var model = new ResourceDetailsViewModel { Resource = ToCard(resource) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        if (courseId.HasValue && !await _resourceService.OwnsCourseAsync(user.Id, courseId.Value))
            courseId = null;

        var model = new ResourceFormViewModel
        {
            CourseId = courseId,
            Kind = ResourceKind.File,
            Category = ResourceCategory.Notes
        };
        await PopulateCourseOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(ResourceService.MaximumFileSize + 1_048_576)]
    public async Task<IActionResult> Create(ResourceFormViewModel model, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await ValidateFormAsync(model, user.Id, isEdit: false);

        if (!ModelState.IsValid)
        {
            await PopulateCourseOptionsAsync(model, user.Id);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        StoredResourceFile? stored = null;
        try
        {
            if (model.Kind == ResourceKind.File)
                stored = await _resourceService.StoreFileAsync(model.Upload!, cancellationToken);

            var resource = new StudyResource { ApplicationUserId = user.Id };
            ApplyForm(resource, model, stored);
            await _resourceService.AddAsync(resource);
            await _resourceService.SaveChangesAsync();
            TempData["ResourceSuccess"] = $"{resource.Title} was added to your resource library.";
            return RedirectToAction(nameof(Details), new { id = resource.Id });
        }
        catch (InvalidOperationException exception)
        {
            if (stored is not null) await _resourceService.DeleteStoredFileAsync(stored.StoredFileName);
            ModelState.AddModelError(nameof(model.Upload), exception.Message);
        }
        catch
        {
            if (stored is not null) await _resourceService.DeleteStoredFileAsync(stored.StoredFileName);
            throw;
        }

        await PopulateCourseOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id);
        if (resource is null) return NotFound();

        var model = ToForm(resource);
        await PopulateCourseOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(ResourceService.MaximumFileSize + 1_048_576)]
    public async Task<IActionResult> Edit(int id, ResourceFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id, trackChanges: true);
        if (resource is null) return NotFound();

        model.ExistingFileName = resource.OriginalFileName;
        model.ExistingFileSize = resource.FileSizeBytes.HasValue
            ? ResourceCardViewModel.FormatBytes(resource.FileSizeBytes.Value)
            : null;
        await ValidateFormAsync(model, user.Id, isEdit: true, existing: resource);

        if (!ModelState.IsValid)
        {
            await PopulateCourseOptionsAsync(model, user.Id);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        StoredResourceFile? replacement = null;
        var oldStoredName = resource.StoredFileName;
        try
        {
            if (model.Kind == ResourceKind.File && model.Upload is not null)
                replacement = await _resourceService.StoreFileAsync(model.Upload, cancellationToken);

            ApplyForm(resource, model, replacement);
            resource.UpdatedAt = DateTimeOffset.UtcNow;
            await _resourceService.SaveChangesAsync();

            if (oldStoredName is not null &&
                (resource.Kind == ResourceKind.Link || replacement is not null))
            {
                await _resourceService.DeleteStoredFileAsync(oldStoredName);
            }

            TempData["ResourceSuccess"] = $"{resource.Title} was updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException exception)
        {
            if (replacement is not null) await _resourceService.DeleteStoredFileAsync(replacement.StoredFileName);
            ModelState.AddModelError(nameof(model.Upload), exception.Message);
        }
        catch
        {
            if (replacement is not null) await _resourceService.DeleteStoredFileAsync(replacement.StoredFileName);
            throw;
        }

        await PopulateCourseOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id, string? returnUrl = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id, trackChanges: true);
        if (resource is null) return NotFound();
        resource.IsFavorite = !resource.IsFavorite;
        resource.UpdatedAt = DateTimeOffset.UtcNow;
        await _resourceService.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id);
        if (resource is null || resource.Kind != ResourceKind.File ||
            string.IsNullOrWhiteSpace(resource.StoredFileName)) return NotFound();

        var stream = await _resourceService.OpenFileAsync(resource.StoredFileName, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, resource.ContentType ?? "application/octet-stream",
            resource.OriginalFileName ?? "resource-file");
    }

    [HttpGet]
    public async Task<IActionResult> OpenLink(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id);
        if (resource is null || resource.Kind != ResourceKind.Link ||
            !TryNormalizeHttpUrl(resource.ExternalUrl, out var safeUrl)) return NotFound();
        return Redirect(safeUrl);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id);
        if (resource is null) return NotFound();
        var model = new ResourceDeleteViewModel { Resource = ToCard(resource) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var resource = await _resourceService.GetOwnedResourceAsync(user.Id, id, trackChanges: true);
        if (resource is null) return NotFound();

        var title = resource.Title;
        var storedName = resource.StoredFileName;
        _resourceService.Remove(resource);
        await _resourceService.SaveChangesAsync();
        await _resourceService.DeleteStoredFileAsync(storedName);
        TempData["ResourceSuccess"] = $"{title} was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFormAsync(
        ResourceFormViewModel model,
        string userId,
        bool isEdit,
        StudyResource? existing = null)
    {
        if (model.CourseId.HasValue && !await _resourceService.OwnsCourseAsync(userId, model.CourseId.Value))
            ModelState.AddModelError(nameof(model.CourseId), "Please select one of your own courses.");
        if (!IsDefined(model.Kind)) ModelState.AddModelError(nameof(model.Kind), "Please select a valid source.");
        if (!IsDefined(model.Category)) ModelState.AddModelError(nameof(model.Category), "Please select a valid category.");

        if (model.Kind == ResourceKind.File)
        {
            var existingFileAvailable = isEdit && existing?.Kind == ResourceKind.File &&
                                        !string.IsNullOrWhiteSpace(existing.StoredFileName);
            if (model.Upload is null && !existingFileAvailable)
                ModelState.AddModelError(nameof(model.Upload), "Please select a file to upload.");
            if (model.Upload is not null)
            {
                var extension = Path.GetExtension(Path.GetFileName(model.Upload.FileName));
                if (!ResourceService.AllowedExtensions.Contains(extension))
                    ModelState.AddModelError(nameof(model.Upload), "This file type is not allowed.");
                if (model.Upload.Length <= 0 || model.Upload.Length > ResourceService.MaximumFileSize)
                    ModelState.AddModelError(nameof(model.Upload), "The file must be between 1 byte and 20 MB.");
            }
            model.ExternalUrl = null;
        }
        else if (model.Kind == ResourceKind.Link)
        {
            if (!TryNormalizeHttpUrl(model.ExternalUrl, out var normalizedUrl))
                ModelState.AddModelError(nameof(model.ExternalUrl), "Enter a valid HTTP or HTTPS URL.");
            else
                model.ExternalUrl = normalizedUrl;
        }
    }

    private static bool TryNormalizeHttpUrl(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return false;
        normalized = uri.AbsoluteUri;
        return true;
    }

    private static void ApplyForm(
        StudyResource resource,
        ResourceFormViewModel model,
        StoredResourceFile? stored)
    {
        resource.Title = model.Title.Trim();
        resource.Description = Clean(model.Description);
        resource.CourseId = model.CourseId;
        resource.Kind = model.Kind!.Value;
        resource.Category = model.Category!.Value;
        resource.Tags = Clean(model.Tags);
        resource.IsFavorite = model.IsFavorite;

        if (resource.Kind == ResourceKind.Link)
        {
            resource.ExternalUrl = model.ExternalUrl;
            resource.OriginalFileName = null;
            resource.StoredFileName = null;
            resource.ContentType = null;
            resource.FileSizeBytes = null;
        }
        else
        {
            resource.ExternalUrl = null;
            if (stored is not null)
            {
                resource.OriginalFileName = stored.OriginalFileName;
                resource.StoredFileName = stored.StoredFileName;
                resource.ContentType = stored.ContentType;
                resource.FileSizeBytes = stored.SizeBytes;
            }
        }
    }

    private static ResourceCardViewModel ToCard(StudyResource resource) => new()
    {
        Id = resource.Id,
        Title = resource.Title,
        Description = resource.Description,
        Kind = resource.Kind,
        Category = resource.Category,
        Tags = resource.Tags,
        ExternalUrl = resource.ExternalUrl,
        OriginalFileName = resource.OriginalFileName,
        FileSizeBytes = resource.FileSizeBytes,
        IsFavorite = resource.IsFavorite,
        CourseId = resource.CourseId,
        CourseCode = resource.Course?.CourseCode,
        CourseName = resource.Course?.CourseName,
        CreatedAt = resource.CreatedAt,
        UpdatedAt = resource.UpdatedAt
    };

    private static ResourceFormViewModel ToForm(StudyResource resource) => new()
    {
        Id = resource.Id,
        Title = resource.Title,
        Description = resource.Description,
        CourseId = resource.CourseId,
        Kind = resource.Kind,
        Category = resource.Category,
        Tags = resource.Tags,
        ExternalUrl = resource.ExternalUrl,
        IsFavorite = resource.IsFavorite,
        ExistingFileName = resource.OriginalFileName,
        ExistingFileSize = resource.FileSizeBytes.HasValue
            ? ResourceCardViewModel.FormatBytes(resource.FileSizeBytes.Value)
            : null
    };

    private async Task PopulateCourseOptionsAsync(ResourceFormViewModel model, string userId) =>
        model.CourseOptions = await BuildCourseOptionsAsync(userId, model.CourseId);

    private async Task<IReadOnlyList<SelectListItem>> BuildCourseOptionsAsync(string userId, int? selectedId)
    {
        var courses = await _resourceService.GetCourseOptionsAsync(userId);
        return courses.Select(course => new SelectListItem
        {
            Value = course.Id.ToString(),
            Text = $"{course.CourseCode} — {course.CourseName}",
            Selected = course.Id == selectedId
        }).ToList();
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user)
    {
        var profile = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id)
            .Select(item => new
            {
                item.StudentId, item.Department, item.Semester,
                HasProfileImage = item.ProfileImageData != null,
                item.CreatedAt, item.UpdatedAt
            }).SingleOrDefaultAsync();
        if (profile is null) return false;
        var fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Student" : user.FullName;
        model.FullName = fullName;
        model.Email = user.Email ?? string.Empty;
        model.StudentId = profile.StudentId;
        model.Initials = CreateInitials(fullName);
        model.DepartmentLabel = profile.Department ?? "Department not set";
        model.SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set";
        model.HasProfileImage = profile.HasProfileImage;
        model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds();
        return true;
    }

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "ST";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static string? NormalizeSearch(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 100)];
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsDefined<TEnum>(TEnum? value) where TEnum : struct, Enum =>
        value.HasValue && Enum.IsDefined(typeof(TEnum), value.Value);

    private static ObjectResult MissingStudentProfile() =>
        new(new ProblemDetails
        {
            Title = "Student profile unavailable",
            Detail = "A Student profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
