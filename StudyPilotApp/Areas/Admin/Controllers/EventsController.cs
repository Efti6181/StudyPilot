using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class EventsController : Controller
{
    private const int PageSize = 10;
    private static readonly TimeSpan CampusOffset = TimeSpan.FromHours(6);
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminEventService _eventService;

    public EventsController(UserManager<ApplicationUser> userManager, IAdminEventService eventService)
    {
        _userManager = userManager;
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        search = CleanSearch(search);
        status = NormalizeStatus(status);
        if (type.HasValue && !Enum.IsDefined(typeof(CampusEventType), type.Value)) type = null;
        if (locationType.HasValue && !Enum.IsDefined(typeof(EventLocationType), locationType.Value)) locationType = null;

        var data = await _eventService.SearchAsync(
            search, type, locationType, status, page, PageSize, cancellationToken);
        var model = new AdminEventIndexViewModel
        {
            Search = search,
            Type = type,
            LocationType = locationType,
            Status = status,
            TotalCount = data.TotalCount,
            PublishedCount = data.PublishedCount,
            DraftCount = data.DraftCount,
            UpcomingCount = data.UpcomingCount,
            ActiveRegistrationCount = data.ActiveRegistrationCount,
            FilteredCount = data.FilteredCount,
            Page = data.Page,
            TotalPages = data.TotalPages,
            Events = data.Items.Select(ToRow).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _eventService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var registrants = await _eventService.GetRegistrantsAsync(id, cancellationToken);
        var (displayStatus, statusClass) = GetStatus(item);
        var model = new AdminEventDetailsViewModel
        {
            Id = item.Id, Title = item.Title, ShortDescription = item.ShortDescription,
            Description = item.Description, Type = item.Type, LocationType = item.LocationType,
            Venue = item.Venue, OnlineUrl = item.OnlineUrl, OrganizerName = item.OrganizerName,
            StartAt = item.StartAt, EndAt = item.EndAt,
            RegistrationDeadline = item.RegistrationDeadline, Capacity = item.Capacity,
            IsPublished = item.IsPublished, CreatedByName = item.CreatedByName,
            CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt,
            RegistrationCount = item.RegistrationCount, CancelledCount = item.CancelledCount,
            SavedCount = item.SavedCount, DisplayStatus = displayStatus, StatusClass = statusClass,
            Registrants = registrants.Select(registration => new AdminEventRegistrantViewModel
            {
                UserId = registration.UserId,
                FullName = registration.FullName,
                Email = registration.Email,
                RegisteredAt = registration.RegisteredAt,
                CancelledAt = registration.CancelledAt
            }).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var start = DateTime.Today.AddDays(7).AddHours(10);
        var model = new AdminEventFormViewModel
        {
            Type = CampusEventType.Workshop,
            LocationType = EventLocationType.OnCampus,
            OrganizerName = "Premier University",
            StartAt = start,
            EndAt = start.AddHours(2),
            RegistrationDeadline = start.AddDays(-1),
            IsPublished = false
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminEventFormViewModel model, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateForm(model);
        if (ModelState.IsValid)
        {
            var result = await _eventService.CreateAsync(ToInput(model), admin.Id, cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = model.IsPublished
                    ? "The event was created and published for students."
                    : "The event draft was created successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The event could not be created.");
        }
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _eventService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new AdminEventFormViewModel
        {
            Id = item.Id, Title = item.Title, ShortDescription = item.ShortDescription,
            Description = item.Description, Type = item.Type, LocationType = item.LocationType,
            Venue = item.Venue, OnlineUrl = item.OnlineUrl, OrganizerName = item.OrganizerName,
            StartAt = ToCampusTime(item.StartAt), EndAt = ToCampusTime(item.EndAt),
            RegistrationDeadline = ToCampusTime(item.RegistrationDeadline),
            Capacity = item.Capacity, IsPublished = item.IsPublished
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminEventFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateForm(model);
        if (ModelState.IsValid)
        {
            var result = await _eventService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = "Event details were updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The event could not be updated.");
        }
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPublished(int id, bool isPublished, CancellationToken cancellationToken)
    {
        var result = await _eventService.SetPublishedAsync(id, isPublished, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Succeeded
            ? isPublished ? "The event is now visible to students." : "The event was moved back to draft."
            : result.Error ?? "The event publication status could not be changed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _eventService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AdminError"] = result.Error ?? "The event could not be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["AdminSuccess"] = "The event was permanently deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateForm(AdminEventFormViewModel model)
    {
        if (model.Type.HasValue && !Enum.IsDefined(typeof(CampusEventType), model.Type.Value))
            ModelState.AddModelError(nameof(model.Type), "Select a valid event type.");
        if (model.LocationType.HasValue && !Enum.IsDefined(typeof(EventLocationType), model.LocationType.Value))
            ModelState.AddModelError(nameof(model.LocationType), "Select a valid event format.");
        if (model.StartAt.HasValue && model.EndAt.HasValue && model.EndAt <= model.StartAt)
            ModelState.AddModelError(nameof(model.EndAt), "The end time must be after the start time.");
        if (model.StartAt.HasValue && model.RegistrationDeadline.HasValue && model.RegistrationDeadline > model.StartAt)
            ModelState.AddModelError(nameof(model.RegistrationDeadline), "Registration must close before the event starts.");
        if (model.LocationType is EventLocationType.OnCampus or EventLocationType.Hybrid && string.IsNullOrWhiteSpace(model.Venue))
            ModelState.AddModelError(nameof(model.Venue), "A venue is required for on-campus and hybrid events.");
        if (model.LocationType is EventLocationType.Online or EventLocationType.Hybrid && string.IsNullOrWhiteSpace(model.OnlineUrl))
            ModelState.AddModelError(nameof(model.OnlineUrl), "An online URL is required for online and hybrid events.");
    }

    private static AdminEventInput ToInput(AdminEventFormViewModel model) => new(
        model.Title, model.ShortDescription, model.Description, model.Type!.Value,
        model.LocationType!.Value, model.Venue, model.OnlineUrl, model.OrganizerName,
        ToUtc(model.StartAt!.Value), ToUtc(model.EndAt!.Value),
        ToUtc(model.RegistrationDeadline!.Value), model.Capacity, model.IsPublished);

    private static AdminEventRowViewModel ToRow(AdminManagedEventData item)
    {
        var (displayStatus, statusClass) = GetStatus(item);
        return new AdminEventRowViewModel
        {
            Id = item.Id, Title = item.Title, Type = item.Type,
            LocationType = item.LocationType,
            Location = item.LocationType == EventLocationType.Online ? "Online" : item.Venue ?? item.LocationType.ToString(),
            StartAt = item.StartAt, EndAt = item.EndAt,
            RegistrationCount = item.RegistrationCount, Capacity = item.Capacity,
            IsPublished = item.IsPublished, DisplayStatus = displayStatus, StatusClass = statusClass
        };
    }

    private static (string Label, string CssClass) GetStatus(AdminManagedEventData item)
    {
        var now = DateTimeOffset.UtcNow;
        if (!item.IsPublished) return ("Draft", "draft");
        if (item.EndAt < now) return ("Ended", "ended");
        if (item.StartAt <= now) return ("Ongoing", "ongoing");
        return ("Published", "published");
    }

    private static DateTimeOffset ToUtc(DateTime value) =>
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), CampusOffset).ToUniversalTime();

    private static DateTime ToCampusTime(DateTimeOffset value) => value.ToOffset(CampusOffset).DateTime;

    private static string? CleanSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

    private static string NormalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "published" => "published", "draft" => "draft", "upcoming" => "upcoming",
        "ongoing" => "ongoing", "ended" => "ended", _ => "all"
    };

    private static void PopulateShell(AdminShellViewModel model, ApplicationUser user)
    {
        model.FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Administrator" : user.FullName;
        model.Email = user.Email ?? string.Empty;
        model.Initials = CreateInitials(user.FullName);
    }

    private static string CreateInitials(string? value)
    {
        var parts = (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "AD";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
