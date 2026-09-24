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
public sealed class FacultyEventsController : Controller
{
    private const int PageSize = 9;
    private static readonly TimeSpan CampusOffset = TimeSpan.FromHours(6);
    private static readonly HashSet<string> AllowedScopes =
        new(StringComparer.OrdinalIgnoreCase) { "upcoming", "registered", "saved", "past", "all" };
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase) { "soonest", "latest", "popular", "newest" };

    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly IEventService _events;
    private readonly INotificationService _notifications;

    public FacultyEventsController(
        UserManager<ApplicationUser> users,
        ApplicationDbContext db,
        IEventService events,
        INotificationService notifications)
    {
        _users = users;
        _db = db;
        _events = events;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string scope = "upcoming",
        string sort = "soonest",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();

        search = NormalizeSearch(search);
        type = IsDefined(type) ? type : null;
        locationType = IsDefined(locationType) ? locationType : null;
        scope = AllowedScopes.Contains(scope) ? scope.ToLowerInvariant() : "upcoming";
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "soonest";
        page = Math.Max(1, page);

        var result = await _events.GetEventsAsync(
            context.Value.User.Id, search, type, locationType, scope, sort, page, PageSize);
        var totalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / (double)PageSize));
        if (page > totalPages)
        {
            page = totalPages;
            result = await _events.GetEventsAsync(
                context.Value.User.Id, search, type, locationType, scope, sort, page, PageSize);
        }

        var summary = await _events.GetSummaryAsync(context.Value.User.Id);
        var model = new FacultyEventIndexViewModel
        {
            Events = result.Items.Select(item => ToCard(item, context.Value.User.Id)).ToList(),
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
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var campusEvent = await _events.GetEventAsync(id);
        if (campusEvent is null) return NotFound();

        var model = new FacultyEventDetailsViewModel
        {
            Event = ToCard(campusEvent, context.Value.User.Id)
        };
        PopulateShell(model, context.Value.User, context.Value.Profile);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(int id)
    {
        var userId = _users.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var result = await _events.RegisterAsync(id, userId);
        switch (result)
        {
            case EventRegistrationResult.Registered:
                TempData["FacultySuccess"] = "Your event registration is confirmed.";
                var campusEvent = await _events.GetEventAsync(id);
                if (campusEvent is not null)
                {
                    await _notifications.CreateAsync(
                        userId,
                        "Event registration confirmed",
                        $"You are registered for {campusEvent.Title} on {campusEvent.StartAt.ToOffset(CampusOffset):dd MMM yyyy}.",
                        NotificationType.Event,
                        $"/FacultyEvents/Details/{id}");
                }
                break;
            case EventRegistrationResult.AlreadyRegistered:
                TempData["FacultyInfo"] = "You are already registered for this event.";
                break;
            case EventRegistrationResult.RegistrationClosed:
                TempData["FacultyError"] = "Registration for this event is closed.";
                break;
            case EventRegistrationResult.EventEnded:
                TempData["FacultyError"] = "This event has already ended.";
                break;
            case EventRegistrationResult.Full:
                TempData["FacultyError"] = "This event is currently full.";
                break;
            default:
                return NotFound();
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRegistration(int id)
    {
        var userId = _users.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();

        var cancelled = await _events.CancelRegistrationAsync(id, userId);
        if (cancelled)
        {
            var campusEvent = await _events.GetEventAsync(id);
            if (campusEvent is not null)
            {
                await _notifications.CreateAsync(
                    userId,
                    "Event registration cancelled",
                    $"Your registration for {campusEvent.Title} was cancelled.",
                    NotificationType.Event,
                    $"/FacultyEvents/Details/{id}");
            }
        }
        TempData[cancelled ? "FacultySuccess" : "FacultyInfo"] = cancelled
            ? "Your registration was cancelled."
            : "No active registration was found.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSaved(int id, string? returnUrl = null)
    {
        var userId = _users.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var saved = await _events.ToggleSavedAsync(id, userId);
        if (!saved.HasValue) return NotFound();

        TempData["FacultySuccess"] = saved.Value
            ? "Event saved to your list."
            : "Event removed from your saved list.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> JoinOnline(int id)
    {
        var userId = _users.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        var campusEvent = await _events.GetEventAsync(id);
        if (campusEvent is null ||
            !campusEvent.Registrations.Any(item =>
                item.ApplicationUserId == userId && item.CancelledAt == null) ||
            !TryNormalizeHttpUrl(campusEvent.OnlineUrl, out var safeUrl))
            return NotFound();
        return Redirect(safeUrl);
    }

    private async Task<(ApplicationUser User, FacultyProfile Profile)?> GetContextAsync(
        CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return null;
        var profile = await _db.FacultyProfiles.AsNoTracking()
            .Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
        return profile is null ? null : (user, profile);
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

    private static void PopulateShell(
        FacultyShellViewModel model,
        ApplicationUser user,
        FacultyProfile profile)
    {
        var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Faculty" : user.FullName;
        model.FullName = name;
        model.Email = user.Email ?? string.Empty;
        model.FacultyId = profile.FacultyId;
        model.DepartmentLabel = profile.Department?.Name ?? "Department not assigned";
        model.DesignationLabel = profile.Designation ?? "Faculty member";
        model.HasProfileImage = profile.ProfileImageData is not null;
        model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds();
        model.Initials = CreateInitials(name);
    }

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "FA";
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

    private static bool TryNormalizeHttpUrl(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return false;
        normalized = uri.AbsoluteUri;
        return true;
    }

    private static ObjectResult MissingFacultyProfile() =>
        new(new ProblemDetails
        {
            Title = "Faculty profile unavailable",
            Detail = "A Faculty profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
