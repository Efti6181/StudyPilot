using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Faculty")]
public sealed class FacultyController : Controller
{
    private const int MaxProfileImageBytes = 2 * 1024 * 1024;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IFacultyPortalService _facultyService;

    public FacultyController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IFacultyPortalService facultyService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _facultyService = facultyService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingProfile();
        var dashboard = await _facultyService.GetDashboardAsync(context.Value.User.Id, cancellationToken);
        if (dashboard is null) return MissingProfile();
        var hour = DateTimeOffset.Now.Hour;
        var model = new FacultyDashboardViewModel
        {
            Greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening",
            AssignedCourses = dashboard.AssignedCourses, ActiveCourses = dashboard.ActiveCourses,
            AssignedCredits = dashboard.AssignedCredits, UpcomingEvents = dashboard.UpcomingEvents,
            ActiveAnnouncements = dashboard.ActiveAnnouncements, RecentCommunityPosts = dashboard.RecentCommunityPosts,
            UpcomingAssessments = dashboard.UpcomingAssessments, RecentlyPublishedAssessments = dashboard.RecentlyPublishedAssessments,
            PublishedResources = dashboard.PublishedResources,
            PublishedCourseAnnouncements = dashboard.PublishedCourseAnnouncements,
            Courses = dashboard.Courses, Events = dashboard.Events,
            Announcements = dashboard.Announcements, CommunityItems = dashboard.CommunityItems
        };
        CopyShell(model, BuildShell(context.Value.User, context.Value.Profile));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingProfile();
        var shell = BuildShell(context.Value.User, context.Value.Profile);
        var model = new FacultyProfileViewModel
        {
            Designation = context.Value.Profile.Designation, PhoneNumber = context.Value.Profile.PhoneNumber,
            OfficeLocation = context.Value.Profile.OfficeLocation, OfficeHours = context.Value.Profile.OfficeHours,
            Bio = context.Value.Profile.Bio, TeachingInterests = context.Value.Profile.TeachingInterests
        };
        CopyShell(model, shell);
        model.CompletionPercentage = CalculateCompletion(model, shell.HasProfileImage);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxProfileImageBytes + 200_000)]
    public async Task<IActionResult> Profile(FacultyProfileViewModel model, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(cancellationToken, true);
        if (context is null) return MissingProfile();
        byte[]? uploadedImage = null;
        string? uploadedContentType = null;
        if (model.ProfileImage is not null)
            (uploadedImage, uploadedContentType) = await ValidateAndReadImageAsync(model.ProfileImage, cancellationToken);
        if (!ModelState.IsValid)
        {
            var shell = BuildShell(context.Value.User, context.Value.Profile);
            CopyShell(model, shell);
            model.CompletionPercentage = CalculateCompletion(model, shell.HasProfileImage);
            return View(model);
        }
        var profile = context.Value.Profile;
        profile.Designation = Clean(model.Designation); profile.PhoneNumber = Clean(model.PhoneNumber);
        profile.OfficeLocation = Clean(model.OfficeLocation); profile.OfficeHours = Clean(model.OfficeHours);
        profile.Bio = Clean(model.Bio); profile.TeachingInterests = Clean(model.TeachingInterests);
        if (uploadedImage is not null && uploadedContentType is not null)
        {
            profile.ProfileImageData = uploadedImage; profile.ProfileImageContentType = uploadedContentType;
        }
        else if (model.RemoveProfileImage)
        {
            profile.ProfileImageData = null; profile.ProfileImageContentType = null;
        }
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        TempData["FacultySuccess"] = "Your faculty profile was updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> ProfileImage(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var image = await _dbContext.FacultyProfiles.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .Select(item => new { item.ProfileImageData, item.ProfileImageContentType })
            .SingleOrDefaultAsync(cancellationToken);
        return image?.ProfileImageData is null || string.IsNullOrWhiteSpace(image.ProfileImageContentType)
            ? NotFound() : File(image.ProfileImageData, image.ProfileImageContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Courses(string? search, int? academicPeriodId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingProfile();
        if (!string.IsNullOrWhiteSpace(search)) { search = search.Trim(); search = search[..Math.Min(search.Length, 100)]; } else search = null;
        var periods = await _facultyService.GetPeriodsAsync(context.Value.User.Id, cancellationToken);
        if (academicPeriodId.HasValue && periods.All(item => item.Id != academicPeriodId.Value)) academicPeriodId = null;
        var data = await _facultyService.SearchCoursesAsync(context.Value.User.Id, search, academicPeriodId, activeOnly, cancellationToken);
        var model = new FacultyCoursesViewModel
        {
            Search = search, AcademicPeriodId = academicPeriodId, ActiveOnly = activeOnly,
            TotalCount = data.TotalCount, ActiveCount = data.ActiveCount, Periods = periods, Courses = data.Items
        };
        CopyShell(model, BuildShell(context.Value.User, context.Value.Profile));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Course(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingProfile();
        var course = await _facultyService.GetCourseAsync(context.Value.User.Id, id, cancellationToken: cancellationToken);
        if (course is null) return NotFound();
        var model = new FacultyCourseDetailsViewModel { Course = course };
        CopyShell(model, BuildShell(context.Value.User, context.Value.Profile));
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditCourseOverview(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingProfile();
        var course = await _facultyService.GetCourseAsync(context.Value.User.Id, id, cancellationToken: cancellationToken);
        if (course is null) return NotFound();
        var model = new FacultyCourseOverviewViewModel { AssignmentId = course.AssignmentId, Code = course.Code, Name = course.Name, CourseOverview = course.CourseOverview };
        CopyShell(model, BuildShell(context.Value.User, context.Value.Profile));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourseOverview(int id, FacultyCourseOverviewViewModel model, CancellationToken cancellationToken)
    {
        if (model.AssignmentId != id) return BadRequest();
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingProfile();
        if (!ModelState.IsValid)
        {
            var course = await _facultyService.GetCourseAsync(context.Value.User.Id, id, cancellationToken: cancellationToken);
            if (course is null) return NotFound();
            model.Code = course.Code; model.Name = course.Name;
            CopyShell(model, BuildShell(context.Value.User, context.Value.Profile));
            return View(model);
        }
        if (!await _facultyService.UpdateCourseOverviewAsync(context.Value.User.Id, id, model.CourseOverview, cancellationToken)) return NotFound();
        TempData["FacultySuccess"] = "Course teaching overview updated.";
        return RedirectToAction(nameof(Course), new { id });
    }

    private async Task<(ApplicationUser User, FacultyProfile Profile)?> GetContextAsync(CancellationToken cancellationToken, bool trackChanges = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return null;
        var query = _dbContext.FacultyProfiles.Include(item => item.Department).Where(item => item.ApplicationUserId == user.Id);
        if (!trackChanges) query = query.AsNoTracking();
        var profile = await query.SingleOrDefaultAsync(cancellationToken);
        return profile is null ? null : (user, profile);
    }

    private IActionResult MissingProfile() => Problem("A Faculty profile is not linked to this account. Please contact an administrator.");

    private static FacultyShellViewModel BuildShell(ApplicationUser user, FacultyProfile profile)
    {
        var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Faculty" : user.FullName;
        return new FacultyShellViewModel
        {
            FullName = name, Email = user.Email ?? string.Empty, FacultyId = profile.FacultyId,
            Initials = Initials(name), DepartmentLabel = profile.Department?.Name ?? "Department not assigned",
            DesignationLabel = profile.Designation ?? "Faculty member", HasProfileImage = profile.ProfileImageData is not null,
            ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds()
        };
    }

    private static void CopyShell(FacultyShellViewModel target, FacultyShellViewModel source)
    {
        target.FullName = source.FullName; target.Email = source.Email; target.FacultyId = source.FacultyId;
        target.Initials = source.Initials; target.DepartmentLabel = source.DepartmentLabel; target.DesignationLabel = source.DesignationLabel;
        target.HasProfileImage = source.HasProfileImage; target.ProfileImageVersion = source.ProfileImageVersion;
    }

    private async Task<(byte[]? Data, string? ContentType)> ValidateAndReadImageAsync(IFormFile image, CancellationToken cancellationToken)
    {
        if (image.Length == 0 || image.Length > MaxProfileImageBytes)
        {
            ModelState.AddModelError(nameof(FacultyProfileViewModel.ProfileImage), "Choose a JPG, PNG or WebP image smaller than 2 MB.");
            return (null, null);
        }
        await using var stream = new MemoryStream(); await image.CopyToAsync(stream, cancellationToken);
        var data = stream.ToArray(); var detected = DetectImageContentType(data);
        var expected = Path.GetExtension(image.FileName).ToLowerInvariant() switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null };
        if (detected is null || detected != expected)
        {
            ModelState.AddModelError(nameof(FacultyProfileViewModel.ProfileImage), "The file is not a valid JPG, PNG or WebP image.");
            return (null, null);
        }
        return (data, detected);
    }

    private static string? DetectImageContentType(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return "image/jpeg";
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (data.Length >= png.Length && data[..png.Length].SequenceEqual(png)) return "image/png";
        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }

    private static int CalculateCompletion(FacultyProfileViewModel model, bool hasImage)
    {
        var values = new[] { model.Designation, model.PhoneNumber, model.OfficeLocation, model.OfficeHours, model.Bio, model.TeachingInterests };
        return (int)Math.Round((values.Count(item => !string.IsNullOrWhiteSpace(item)) + (hasImage ? 1 : 0)) / 7d * 100);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "FA";
        return parts.Length == 1 ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant() : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
