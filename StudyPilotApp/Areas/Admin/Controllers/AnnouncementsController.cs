using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class AnnouncementsController : Controller
{
    private const int PageSize = 10;
    private static readonly TimeSpan CampusOffset = TimeSpan.FromHours(6);
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(
        UserManager<ApplicationUser> userManager,
        IAnnouncementService announcementService)
    {
        _userManager = userManager;
        _announcementService = announcementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        AnnouncementAudience? audience,
        AnnouncementPriority? priority,
        string status = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        search = CleanSearch(search);
        status = NormalizeStatus(status);
        if (audience.HasValue && !Enum.IsDefined(typeof(AnnouncementAudience), audience.Value)) audience = null;
        if (priority.HasValue && !Enum.IsDefined(typeof(AnnouncementPriority), priority.Value)) priority = null;
        var data = await _announcementService.SearchAsync(
            search, audience, priority, status, page, PageSize, cancellationToken);
        var model = new AnnouncementIndexViewModel
        {
            Search = search, Audience = audience, Priority = priority, Status = status,
            TotalCount = data.TotalCount, PublishedCount = data.PublishedCount,
            DraftCount = data.DraftCount, ActiveCount = data.ActiveCount,
            UrgentCount = data.UrgentCount, DeliveredRecipientCount = data.DeliveredRecipientCount,
            FilteredCount = data.FilteredCount, Page = data.Page, TotalPages = data.TotalPages,
            Announcements = data.Items.Select(ToRow).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _announcementService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var (displayStatus, statusClass) = GetStatus(item);
        var model = new AnnouncementDetailsViewModel
        {
            Id = item.Id, Title = item.Title, Summary = item.Summary, Content = item.Content,
            Audience = item.Audience, Priority = item.Priority, IsPublished = item.IsPublished,
            PublishedAt = item.PublishedAt, ExpiresAt = item.ExpiresAt,
            DeliveredAt = item.DeliveredAt, RecipientCount = item.RecipientCount,
            CreatedByName = item.CreatedByName, CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt, DisplayStatus = displayStatus, StatusClass = statusClass
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var model = new AnnouncementFormViewModel
        {
            Audience = AnnouncementAudience.Everyone,
            Priority = AnnouncementPriority.Normal,
            IsPublished = false
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementFormViewModel model, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateForm(model);
        if (ModelState.IsValid)
        {
            var result = await _announcementService.CreateAsync(ToInput(model), admin.Id, cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = model.IsPublished
                    ? $"Announcement published to {result.RecipientCount} active recipient(s)."
                    : "Announcement draft created successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The announcement could not be created.");
        }
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        var item = await _announcementService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var model = new AnnouncementFormViewModel
        {
            Id = item.Id, Title = item.Title, Summary = item.Summary, Content = item.Content,
            Audience = item.Audience, Priority = item.Priority,
            ExpiresAt = item.ExpiresAt.HasValue ? ToCampusTime(item.ExpiresAt.Value) : null,
            IsPublished = item.IsPublished, AudienceLocked = item.DeliveredAt.HasValue
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnnouncementFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id) return BadRequest();
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        ValidateForm(model);
        if (ModelState.IsValid)
        {
            var result = await _announcementService.UpdateAsync(id, ToInput(model), cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = "Announcement updated successfully.";
                return RedirectToAction(nameof(Details), new { id });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The announcement could not be updated.");
        }
        var existing = await _announcementService.GetAsync(id, cancellationToken);
        model.AudienceLocked = existing?.DeliveredAt.HasValue == true;
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPublished(int id, bool isPublished, CancellationToken cancellationToken)
    {
        var result = await _announcementService.SetPublishedAsync(id, isPublished, cancellationToken);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] = result.Succeeded
            ? isPublished
                ? $"Announcement published. {result.RecipientCount} recipient(s) have been notified."
                : "Announcement unpublished. Previously delivered notifications remain in recipient history."
            : result.Error ?? "Announcement publication status could not be changed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _announcementService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["AdminError"] = result.Error ?? "The announcement could not be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["AdminSuccess"] = "Announcement draft deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateForm(AnnouncementFormViewModel model)
    {
        if (model.Audience.HasValue && !Enum.IsDefined(typeof(AnnouncementAudience), model.Audience.Value))
            ModelState.AddModelError(nameof(model.Audience), "Select a valid audience.");
        if (model.Priority.HasValue && !Enum.IsDefined(typeof(AnnouncementPriority), model.Priority.Value))
            ModelState.AddModelError(nameof(model.Priority), "Select a valid priority.");
        if (model.IsPublished && model.ExpiresAt.HasValue && ToUtc(model.ExpiresAt.Value) <= DateTimeOffset.UtcNow)
            ModelState.AddModelError(nameof(model.ExpiresAt), "A published announcement must expire in the future.");
    }

    private static AnnouncementInput ToInput(AnnouncementFormViewModel model) => new(
        model.Title, model.Summary, model.Content, model.Audience!.Value,
        model.Priority!.Value, model.ExpiresAt.HasValue ? ToUtc(model.ExpiresAt.Value) : null,
        model.IsPublished);

    private static AnnouncementRowViewModel ToRow(AnnouncementData item)
    {
        var (displayStatus, statusClass) = GetStatus(item);
        return new AnnouncementRowViewModel
        {
            Id = item.Id, Title = item.Title, Summary = item.Summary,
            Audience = item.Audience, Priority = item.Priority,
            IsPublished = item.IsPublished, PublishedAt = item.PublishedAt,
            ExpiresAt = item.ExpiresAt, RecipientCount = item.RecipientCount,
            DisplayStatus = displayStatus, StatusClass = statusClass
        };
    }

    private static (string Label, string CssClass) GetStatus(AnnouncementData item)
    {
        if (!item.IsPublished) return ("Draft", "draft");
        if (item.ExpiresAt.HasValue && item.ExpiresAt < DateTimeOffset.UtcNow) return ("Expired", "expired");
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
        "published" => "published", "draft" => "draft", "active" => "active",
        "expired" => "expired", _ => "all"
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
        return parts.Length == 1 ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant() : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
