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

[Authorize(Roles = "Student")]
public sealed class AcademicAIController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAcademicAIService _academicAIService;
    private readonly IAcademicAIConversationService _conversationService;

    public AcademicAIController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IAcademicAIService academicAIService,
        IAcademicAIConversationService conversationService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _academicAIService = academicAIService;
        _conversationService = conversationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? conversationId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var model = await BuildIndexModelAsync(user, conversationId);
        if (model is null) return NotFound();
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("academic-ai")]
    public async Task<IActionResult> Ask(AcademicAIAskViewModel input, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!Enum.IsDefined(input.Mode))
            ModelState.AddModelError(nameof(input.Mode), "Select a valid AI assistant mode.");

        input.Prompt = input.Prompt?.Trim() ?? string.Empty;

        if (input.CourseId.HasValue && !await _dbContext.Courses.AnyAsync(item =>
                item.Id == input.CourseId.Value && item.ApplicationUserId == user.Id,
                cancellationToken))
            return NotFound();

        if (input.AssessmentId.HasValue && !await _dbContext.Assessments.AnyAsync(item =>
                item.Id == input.AssessmentId.Value && item.ApplicationUserId == user.Id,
                cancellationToken))
            return NotFound();

        if (input.Mode == AcademicAIMode.AssessmentBreakdown && !input.AssessmentId.HasValue)
            ModelState.AddModelError(nameof(input.AssessmentId), "Choose an assessment to break down.");

        AcademicAIConversation? conversation = null;
        if (input.ConversationId.HasValue)
        {
            conversation = await _conversationService.GetOwnedAsync(
                user.Id, input.ConversationId.Value, trackChanges: true);
            if (conversation is null) return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildIndexModelAsync(user, input.ConversationId);
            if (invalidModel is null) return NotFound();
            if (!await PopulateShellAsync(invalidModel, user)) return MissingStudentProfile();
            ViewData["AskInput"] = input;
            return View("Index", invalidModel);
        }

        conversation ??= await _conversationService.CreateAsync(
            user.Id, CreateConversationTitle(input.Prompt), input.Mode);
        conversation.Mode = input.Mode;

        var history = conversation.Messages
            .OrderBy(item => item.CreatedAt)
            .TakeLast(8)
            .Select(item => new AcademicAIHistoryMessage(item.Role, item.Content))
            .ToList();

        var result = await _academicAIService.GenerateAsync(
            user.Id,
            input.Mode,
            input.Prompt,
            input.CourseId,
            input.AssessmentId,
            history,
            cancellationToken);

        await _conversationService.AddExchangeAsync(conversation, input.Prompt, result);
        if (result.IsFallback)
            TempData["AIInfo"] = BuildProviderMessage(result.ErrorCode);

        return RedirectToAction(nameof(Index), new { conversationId = conversation.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConversation(long id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId)) return Challenge();
        if (!await _conversationService.DeleteAsync(userId, id)) return NotFound();
        TempData["AISuccess"] = "Conversation deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AcademicAIIndexViewModel?> BuildIndexModelAsync(
        ApplicationUser user,
        long? conversationId)
    {
        var conversations = await _conversationService.GetRecentAsync(user.Id, 20);
        AcademicAIConversation? active = null;
        if (conversationId.HasValue)
        {
            active = await _conversationService.GetOwnedAsync(user.Id, conversationId.Value);
            if (active is null) return null;
        }

        var courses = await _dbContext.Courses.AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id && item.Status == CourseStatus.Active)
            .OrderBy(item => item.CourseCode)
            .Select(item => new AcademicAISelectItemViewModel
            {
                Id = item.Id,
                Label = item.CourseCode + " — " + item.CourseName
            }).ToListAsync();

        var assessments = await _dbContext.Assessments.AsNoTracking()
            .Where(item => item.ApplicationUserId == user.Id && item.Status != AssessmentStatus.Completed)
            .OrderBy(item => item.DueDate)
            .Select(item => new AcademicAISelectItemViewModel
            {
                Id = item.Id,
                Label = item.Course.CourseCode + " — " + item.Title
            }).ToListAsync();

        return new AcademicAIIndexViewModel
        {
            ConversationId = active?.Id,
            ActiveConversationTitle = active?.Title ?? "New conversation",
            ActiveMode = active?.Mode ?? AcademicAIMode.Ask,
            IsProviderConfigured = _academicAIService.IsProviderConfigured,
            Conversations = conversations.Select(item => new AcademicAIConversationItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Mode = item.Mode,
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
            }).ToList() ?? [] ,
            Courses = courses,
            Assessments = assessments
        };
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
        "timeout" => "Gemini did not respond before the configured timeout. StudyPilot used its deterministic fallback.",
        "network_error" => "StudyPilot could not reach the Gemini API. Check the internet connection, firewall, proxy, and system time.",
        "request_rejected" => "Gemini rejected the request configuration. Check the configured model and application logs.",
        "invalid_response" => "Gemini returned an unreadable response. Check the application logs and try again.",
        "provider_busy" => "Gemini is temporarily busy. Wait briefly and try again.",
        "output_limit" => "Gemini used its response limit before producing usable text. StudyPilot increased the chat allowance; retry this request.",
        "safety_block" => "Gemini blocked this response for safety or recitation reasons. Rephrase the request and avoid asking for copied answers or restricted content.",
        "empty_response" => "Gemini returned no usable text, so StudyPilot used its deterministic fallback.",
        _ => "The external AI provider was unavailable, so StudyPilot used its deterministic fallback."
    };

    private static string CreateInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return "ST";
        return parts.Length == 1
            ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
            : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    private static ObjectResult MissingStudentProfile() =>
        new(new ProblemDetails
        {
            Title = "Student profile unavailable",
            Detail = "A Student profile is not linked to this account. Please contact an administrator.",
            Status = StatusCodes.Status500InternalServerError
        }) { StatusCode = StatusCodes.Status500InternalServerError };
}
