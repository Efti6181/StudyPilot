using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyPilotApp.Areas.Admin.ViewModels;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ModerationController : Controller
{
    private const int PageSize = 12;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IContentModerationService _moderationService;

    public ModerationController(
        UserManager<ApplicationUser> userManager,
        IContentModerationService moderationService)
    {
        _userManager = userManager;
        _moderationService = moderationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string scope = "all",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        search = CleanSearch(search);
        scope = NormalizeScope(scope);
        var data = await _moderationService.SearchAsync(search, scope, page, PageSize, cancellationToken);
        var model = new ModerationIndexViewModel
        {
            Search = search, Scope = scope, PostCount = data.PostCount,
            CommentCount = data.CommentCount, ResourceCount = data.ResourceCount,
            RemovedCount = data.RemovedCount, FilteredCount = data.FilteredCount,
            Page = data.Page, TotalPages = data.TotalPages,
            Items = data.Items.Select(item => new ModerationItemViewModel
            {
                ContentType = item.ContentType, SourceId = item.SourceId,
                Title = item.Title, Content = item.Content,
                OwnerName = item.OwnerName, OwnerEmail = item.OwnerEmail,
                CreatedAt = item.CreatedAt, Detail = item.Detail,
                InteractionCount = item.InteractionCount
            }).ToList(),
            History = data.History.Select(item => new ModerationHistoryViewModel
            {
                Id = item.Id, ContentType = item.ContentType, SourceId = item.SourceId,
                ContentTitle = item.ContentTitle, ContentExcerpt = item.ContentExcerpt,
                OwnerName = item.OwnerName, OwnerEmail = item.OwnerEmail,
                Reason = item.Reason, ModeratorName = item.ModeratorName,
                ModeratedAt = item.ModeratedAt
            }).ToList()
        };
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Review(
        ModeratedContentType contentType,
        int id,
        CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        if (!Enum.IsDefined(typeof(ModeratedContentType), contentType)) return BadRequest();
        var item = await _moderationService.GetContentAsync(contentType, id, cancellationToken);
        if (item is null) return NotFound();
        var model = ToReview(item);
        PopulateShell(model, admin);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(ModerationReviewViewModel model, CancellationToken cancellationToken)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();
        if (!Enum.IsDefined(typeof(ModeratedContentType), model.ContentType)) return BadRequest();
        var item = await _moderationService.GetContentAsync(model.ContentType, model.SourceId, cancellationToken);
        if (item is null) return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _moderationService.RemoveAsync(
                model.ContentType, model.SourceId, model.Reason, admin.Id, cancellationToken);
            if (result.Succeeded)
            {
                TempData["AdminSuccess"] = "Content removed, owner notified, and moderation history recorded.";
                return RedirectToAction(nameof(Index), new { scope = "history" });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The content could not be removed.");
        }

        model.Title = item.Title; model.Content = item.Content;
        model.OwnerName = item.OwnerName; model.OwnerEmail = item.OwnerEmail;
        model.CreatedAt = item.CreatedAt; model.Detail = item.Detail;
        model.InteractionCount = item.InteractionCount;
        PopulateShell(model, admin);
        return View("Review", model);
    }

    private static ModerationReviewViewModel ToReview(ModerationContentData item) => new()
    {
        ContentType = item.ContentType, SourceId = item.SourceId,
        Title = item.Title, Content = item.Content,
        OwnerName = item.OwnerName, OwnerEmail = item.OwnerEmail,
        CreatedAt = item.CreatedAt, Detail = item.Detail,
        InteractionCount = item.InteractionCount
    };

    private static string NormalizeScope(string? scope) => scope?.ToLowerInvariant() switch
    {
        "posts" => "posts", "comments" => "comments", "resources" => "resources",
        "history" => "history", _ => "all"
    };

    private static string? CleanSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;
        var value = search.Trim();
        return value[..Math.Min(value.Length, 100)];
    }

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
