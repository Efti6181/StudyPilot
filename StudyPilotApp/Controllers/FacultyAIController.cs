using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Faculty")]
public sealed class FacultyAIController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly IFacultyAIService _facultyAI;
    private readonly IAcademicAIConversationService _conversations;

    public FacultyAIController(
        UserManager<ApplicationUser> users,
        ApplicationDbContext db,
        IFacultyAIService facultyAI,
        IAcademicAIConversationService conversations)
    {
        _users = users;
        _db = db;
        _facultyAI = facultyAI;
        _conversations = conversations;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        long? conversationId,
        CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();
        var model = await BuildIndexModelAsync(
            context.Value.User, context.Value.Profile, conversationId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("academic-ai")]
    public async Task<IActionResult> Ask(
        FacultyAIAskViewModel input,
        CancellationToken cancellationToken)
    {
        var context = await GetFacultyContextAsync(cancellationToken);
        if (context is null) return MissingFacultyProfile();

        if (!Enum.IsDefined(input.Mode))
            ModelState.AddModelError(nameof(input.Mode), "Select a valid Faculty AI mode.");
        input.Prompt = input.Prompt?.Trim() ?? string.Empty;

        if (input.AssignmentId.HasValue && !await _db.FacultyCourseAssignments.AsNoTracking()
                .AnyAsync(item => item.Id == input.AssignmentId.Value && item.IsActive &&
                    item.FacultyProfile.ApplicationUserId == context.Value.User.Id,
                    cancellationToken))
            return NotFound();

        if ((input.Mode is FacultyAIMode.LessonPlan or FacultyAIMode.AssessmentDesigner) &&
            !input.AssignmentId.HasValue)
            ModelState.AddModelError(nameof(input.AssignmentId), "Choose one of your active courses for this mode.");

        AcademicAIConversation? conversation = null;
        if (input.ConversationId.HasValue)
        {
            conversation = await _conversations.GetOwnedAsync(
                context.Value.User.Id, input.ConversationId.Value, trackChanges: true);
            if (conversation is null) return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexModelAsync(
                context.Value.User, context.Value.Profile, input.ConversationId, cancellationToken);
            if (invalidModel is null) return NotFound();
            ViewData["FacultyAIInput"] = input;
            return View("Index", invalidModel);
        }

        conversation ??= await _conversations.CreateAsync(
            context.Value.User.Id,
            CreateConversationTitle(input.Prompt),
            AcademicAIMode.Ask);

        var history = conversation.Messages
            .OrderBy(item => item.CreatedAt)
            .TakeLast(8)
            .Select(item => new AcademicAIHistoryMessage(item.Role, item.Content))
            .ToList();
        var result = await _facultyAI.GenerateAsync(
            context.Value.User.Id,
            input.Mode,
            input.Prompt,
            input.AssignmentId,
            history,
            cancellationToken);

        await _conversations.AddExchangeAsync(conversation, input.Prompt, result);
        if (result.IsFallback)
            TempData["FacultyAIInfo"] = BuildProviderMessage(result.ErrorCode);

        return RedirectToAction(nameof(Index), new { conversationId = conversation.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConversation(long id)
    {
        var userId = _users.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (!await _conversations.DeleteAsync(userId, id)) return NotFound();
        TempData["FacultySuccess"] = "AI conversation deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<FacultyAIIndexViewModel?> BuildIndexModelAsync(
        ApplicationUser user,
        FacultyProfile profile,
        long? conversationId,
        CancellationToken cancellationToken)
    {
        var conversations = await _conversations.GetRecentAsync(user.Id, 20);
        AcademicAIConversation? active = null;
        if (conversationId.HasValue)
        {
            active = await _conversations.GetOwnedAsync(user.Id, conversationId.Value);
            if (active is null) return null;
        }

        var courses = await _db.FacultyCourseAssignments.AsNoTracking()
            .Where(item => item.FacultyProfile.ApplicationUserId == user.Id && item.IsActive)
            .OrderByDescending(item => item.AcademicPeriod.IsCurrent)
            .ThenBy(item => item.CatalogCourse.Code)
            .Select(item => new FacultyAISelectItemViewModel
            {
                Id = item.Id,
                Label = item.CatalogCourse.Code + " — " + item.CatalogCourse.Name + " · Section " + item.Section
            }).ToListAsync(cancellationToken);

        var model = new FacultyAIIndexViewModel
        {
            ConversationId = active?.Id,
            ActiveConversationTitle = active?.Title ?? "New teaching conversation",
            IsProviderConfigured = _facultyAI.IsProviderConfigured,
            Conversations = conversations.Select(item => new FacultyAIConversationItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                UpdatedAt = item.UpdatedAt,
                IsActive = item.Id == active?.Id
            }).ToList(),
            Messages = active?.Messages.Select(item => new AcademicAIMessageViewModel
            {
                Id = item.Id,
                Role = item.Role,
                Content = item.Content,
                CreatedAt = item.CreatedAt,
                IsFallback = item.IsFallback,
                Provider = item.Provider
            }).ToList() ?? [],
            Courses = courses
        };
        PopulateShell(model, user, profile);
        return model;
    }

    private async Task<(ApplicationUser User, FacultyProfile Profile)?> GetFacultyContextAsync(
        CancellationToken cancellationToken)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return null;
        var profile = await _db.FacultyProfiles.AsNoTracking()
            .Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.ApplicationUserId == user.Id, cancellationToken);
        return profile is null ? null : (user, profile);
    }

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

    private static string CreateConversationTitle(string prompt)
    {
        var title = string.Join(' ', prompt.Split(
            ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return title[..Math.Min(title.Length, 80)];
    }

    private static string BuildProviderMessage(string? errorCode) => errorCode switch
    {
        "not_configured" => "Gemini is not configured. Verify AcademicAI user secrets and restart StudyPilot.",
        "api_key_rejected" => "Gemini rejected the API key. Create a new private key in Google AI Studio, update user secrets, and restart StudyPilot.",
        "model_unavailable" => "The configured Gemini model is unavailable to this API project. StudyPilot also tried its compatible fallback models.",
        "quota_exceeded" => "The Gemini API quota or rate limit was reached. Wait for the quota window to reset or review the project quota in Google AI Studio.",
        "timeout" => "Gemini did not respond before the configured timeout. Faculty AI used its safe fallback.",
        "network_error" => "StudyPilot could not reach the Gemini API. Check the internet connection, firewall, proxy, and system time.",
        "request_rejected" => "Gemini rejected the request configuration. Check the configured model and application logs.",
        "invalid_response" => "Gemini returned an unreadable response. Check the application logs and try again.",
        "provider_busy" => "Gemini is temporarily busy. Wait briefly and try again.",
        "output_limit" => "Gemini used its response limit before producing usable text. Faculty AI increased the chat allowance; retry this request.",
        "safety_block" => "Gemini blocked this response for safety or recitation reasons. Rephrase the request and keep it focused on an editable teaching draft.",
        "empty_response" => "Gemini returned no usable text, so Faculty AI used its safe fallback.",
        _ => "The external AI provider was unavailable, so StudyPilot used its safe faculty fallback."
    };

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "FA";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static ObjectResult MissingFacultyProfile() =>
        new(new ProblemDetails
        {
            Title = "Faculty profile unavailable",
            Detail = "A Faculty profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
