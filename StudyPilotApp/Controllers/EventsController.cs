using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class EventsController : Controller
{
    private const int PageSize = 9;
    private static readonly HashSet<string> AllowedScopes =
        new(StringComparer.OrdinalIgnoreCase) { "upcoming", "registered", "saved", "past", "all" };
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "soonest", "latest", "popular", "newest" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEventService _eventService;
    private readonly INotificationService _notificationService;

    public EventsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IEventService eventService,
        INotificationService notificationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _eventService = eventService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string scope = "upcoming",
        string sort = "soonest",
        int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        search = NormalizeSearch(search);
        type = IsDefined(type) ? type : null;
        locationType = IsDefined(locationType) ? locationType : null;
        scope = AllowedScopes.Contains(scope) ? scope.ToLowerInvariant() : "upcoming";
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "soonest";
        page = Math.Max(1, page);

        var result = await _eventService.GetEventsAsync(
            user.Id, search, type, locationType, scope, sort, page, PageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
        if (page > totalPages)
        {
            page = totalPages;
            result = await _eventService.GetEventsAsync(
                user.Id, search, type, locationType, scope, sort, page, PageSize);
        }

        var summary = await _eventService.GetSummaryAsync(user.Id);
        var model = new EventIndexViewModel
        {
            Events = result.Items.Select(item => ToCard(item, user.Id)).ToList(),
            Search = search,
            Type = type,
            LocationType = locationType,
            Scope = scope,
            Sort = sort,
            Page = page,
            PageSize = PageSize,
            TotalEvents = result.TotalCount,
            RegisteredCount = summary.Registered,
            SavedCount = summary.Saved
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var campusEvent = await _eventService.GetEventAsync(id);
        if (campusEvent is null) return NotFound();

        var model = new EventDetailsViewModel { Event = ToCard(campusEvent, user.Id) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var result = await _eventService.RegisterAsync(id, userId);
        switch (result)
        {
            case EventRegistrationResult.Registered:
                TempData["EventSuccess"] = "Your event registration is confirmed.";
                var registeredEvent = await _eventService.GetEventAsync(id);
                if (registeredEvent is not null)
                {
                    await _notificationService.CreateAsync(
                        userId,
                        "Event registration confirmed",
                        $"You are registered for {registeredEvent.Title} on {registeredEvent.StartAt.ToLocalTime():dd MMM yyyy}.",
                        NotificationType.Event,
                        $"/Events/Details/{id}");
                }
                break;
            case EventRegistrationResult.AlreadyRegistered:
                TempData["EventInfo"] = "You are already registered for this event.";
                break;
            case EventRegistrationResult.RegistrationClosed:
                TempData["EventError"] = "Registration for this event is closed.";
                break;
            case EventRegistrationResult.EventEnded:
                TempData["EventError"] = "This event has already ended.";
                break;
            case EventRegistrationResult.Full:
                TempData["EventError"] = "This event is currently full.";
                break;
            default:
                return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var cancelled = await _eventService.CancelRegistrationAsync(id, userId);
        if (cancelled)
        {
            var cancelledEvent = await _eventService.GetEventAsync(id);
            if (cancelledEvent is not null)
            {
                await _notificationService.CreateAsync(
                    userId,
                    "Event registration cancelled",
                    $"Your registration for {cancelledEvent.Title} was cancelled.",
                    NotificationType.Event,
                    $"/Events/Details/{id}");
            }
        }
        TempData[cancelled ? "EventSuccess" : "EventInfo"] = cancelled
            ? "Your registration was cancelled."
            : "No active registration was found.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSaved(int id, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var saved = await _eventService.ToggleSavedAsync(id, userId);
        if (!saved.HasValue) return NotFound();

        TempData["EventSuccess"] = saved.Value
            ? "Event saved to your list."
            : "Event removed from your saved list.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> JoinOnline(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var campusEvent = await _eventService.GetEventAsync(id);
        if (campusEvent is null ||
            !campusEvent.Registrations.Any(item =>
                item.ApplicationUserId == userId && item.CancelledAt == null) ||
            !TryNormalizeHttpUrl(campusEvent.OnlineUrl, out var safeUrl))
            return NotFound();
        return Redirect(safeUrl);
    }

    private static EventCardViewModel ToCard(CampusEvent item, string userId) => new()
    {
        Id = item.Id,
        Title = item.Title,
        ShortDescription = item.ShortDescription,
        Description = item.Description,
        Type = item.Type,
        LocationType = item.LocationType,
        Venue = item.Venue,
        OrganizerName = item.OrganizerName,
        StartAt = item.StartAt,
        EndAt = item.EndAt,
        RegistrationDeadline = item.RegistrationDeadline,
        Capacity = item.Capacity,
        RegistrationCount = item.Registrations.Count(registration => registration.CancelledAt == null),
        IsRegistered = item.Registrations.Any(registration =>
            registration.ApplicationUserId == userId && registration.CancelledAt == null),
        IsSaved = item.SavedByStudents.Any(saved => saved.ApplicationUserId == userId),
        HasOnlineLink = !string.IsNullOrWhiteSpace(item.OnlineUrl)
    };

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

    private static bool TryNormalizeHttpUrl(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return false;
        normalized = uri.AbsoluteUri;
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
