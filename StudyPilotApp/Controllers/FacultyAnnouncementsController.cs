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
public sealed class FacultyAnnouncementsController : Controller
{
    private static readonly TimeSpan CampusOffset = TimeSpan.FromHours(6);
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly IFacultyAnnouncementService _service;
    private readonly IFacultyAssessmentService _courses;

    public FacultyAnnouncementsController(UserManager<ApplicationUser> users, ApplicationDbContext db, IFacultyAnnouncementService service, IFacultyAssessmentService courses)
    { _users = users; _db = db; _service = service; _courses = courses; }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? assignmentId, AnnouncementPriority? priority, FacultyAnnouncementStatus? status, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        search = CleanSearch(search);
        if (priority.HasValue && !Enum.IsDefined(priority.Value)) priority = null;
        if (status.HasValue && !Enum.IsDefined(status.Value)) status = null;
        var courses = await _courses.GetCourseOptionsAsync(context.Value.User.Id, token);
        if (assignmentId.HasValue && courses.All(x => x.AssignmentId != assignmentId.Value)) assignmentId = null;
        var model = new FacultyAnnouncementIndexViewModel { Search = search, AssignmentId = assignmentId, Priority = priority, Status = status, Courses = courses, Items = await _service.SearchAsync(context.Value.User.Id, search, assignmentId, priority, status, token) };
        Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        var item = await _service.GetOwnedAsync(context.Value.User.Id, id, cancellationToken: token); if (item is null) return NotFound();
        var counts = await _db.FacultyAnnouncementRecipients.AsNoTracking().Where(x => x.FacultyAnnouncementId == id).GroupBy(_ => 1).Select(x => new { Total = x.Count(), Read = x.Count(r => r.IsRead) }).SingleOrDefaultAsync(token);
        var model = new FacultyAnnouncementDetailsViewModel { Announcement = item, RecipientCount = counts?.Total ?? 0, ReadCount = counts?.Read ?? 0 };
        Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assignmentId, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        var model = new FacultyAnnouncementFormViewModel { AssignmentId = assignmentId, Priority = AnnouncementPriority.Normal };
        await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacultyAnnouncementFormViewModel model, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge(); await Validate(model, context.Value.User.Id, false, token);
        if (!ModelState.IsValid) { await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model); }
        var id = await _service.CreateAsync(context.Value.User.Id, Input(model), token); if (id == 0) return NotFound();
        TempData["FacultySuccess"] = "Announcement draft created."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        var item = await _service.GetOwnedAsync(context.Value.User.Id, id, cancellationToken: token); if (item is null) return NotFound(); if (item.Status == FacultyAnnouncementStatus.Archived) return BadRequest();
        var model = new FacultyAnnouncementFormViewModel { Id = item.Id, AssignmentId = item.FacultyCourseAssignmentId, Title = item.Title, Summary = item.Summary, Content = item.Content, Priority = item.Priority, ExpiresAt = item.ExpiresAt.HasValue ? ToCampusTime(item.ExpiresAt.Value) : null, CurrentStatus = item.Status };
        await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FacultyAnnouncementFormViewModel model, CancellationToken token)
    {
        if (model.Id != id) return BadRequest(); var context = await Context(token); if (context is null) return Challenge();
        var current = await _service.GetOwnedAsync(context.Value.User.Id, id, cancellationToken: token); if (current is null) return NotFound(); model.CurrentStatus = current.Status;
        await Validate(model, context.Value.User.Id, current.Status == FacultyAnnouncementStatus.Published, token);
        if (!ModelState.IsValid) { await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model); }
        if (!await _service.UpdateAsync(context.Value.User.Id, id, Input(model), token)) return BadRequest();
        TempData["FacultySuccess"] = "Announcement updated."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken token)
    {
        var userId = _users.GetUserId(User); if (userId is null) return Challenge(); var result = await _service.PublishAsync(userId, id, token);
        TempData[result.Succeeded ? "FacultySuccess" : "FacultyError"] = result.Succeeded ? $"Announcement published to {result.RecipientCount} matching student course(s)." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, CancellationToken token)
    { var userId = _users.GetUserId(User); if (userId is null) return Challenge(); if (!await _service.ArchiveAsync(userId, id, token)) return NotFound(); TempData["FacultySuccess"] = "Announcement archived."; return RedirectToAction(nameof(Details), new { id }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    { var userId = _users.GetUserId(User); if (userId is null) return Challenge(); if (!await _service.DeleteDraftAsync(userId, id, token)) return BadRequest(); TempData["FacultySuccess"] = "Announcement draft deleted."; return RedirectToAction(nameof(Index)); }

    private async Task Validate(FacultyAnnouncementFormViewModel model, string userId, bool published, CancellationToken token)
    {
        if (model.Priority.HasValue && !Enum.IsDefined(model.Priority.Value)) ModelState.AddModelError(nameof(model.Priority), "Select a valid priority.");
        if (model.AssignmentId.HasValue && !await _db.FacultyCourseAssignments.AnyAsync(x => x.Id == model.AssignmentId.Value && x.IsActive && x.FacultyProfile.ApplicationUserId == userId, token)) ModelState.AddModelError(nameof(model.AssignmentId), "Select one of your active course assignments.");
        if (published && model.ExpiresAt.HasValue && ToUtc(model.ExpiresAt.Value) <= DateTimeOffset.UtcNow) ModelState.AddModelError(nameof(model.ExpiresAt), "A published announcement must expire in the future.");
    }

    private async Task Prepare(FacultyAnnouncementFormViewModel model, string userId, CancellationToken token) => model.Courses = await _courses.GetCourseOptionsAsync(userId, token);
    private static FacultyAnnouncementInput Input(FacultyAnnouncementFormViewModel model) => new(model.AssignmentId!.Value, model.Title, model.Summary, model.Content, model.Priority!.Value, model.ExpiresAt.HasValue ? ToUtc(model.ExpiresAt.Value) : null);
    private async Task<(ApplicationUser User, FacultyProfile Profile)?> Context(CancellationToken token) { var user = await _users.GetUserAsync(User); if (user is null) return null; var profile = await _db.FacultyProfiles.AsNoTracking().Include(x => x.Department).SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id, token); return profile is null ? null : (user, profile); }
    private static void Shell(FacultyShellViewModel model, ApplicationUser user, FacultyProfile profile) { var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Faculty" : user.FullName; var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries); model.FullName = name; model.Email = user.Email ?? ""; model.FacultyId = profile.FacultyId; model.DepartmentLabel = profile.Department?.Name ?? "Department not assigned"; model.DesignationLabel = profile.Designation ?? "Faculty member"; model.HasProfileImage = profile.ProfileImageData is not null; model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds(); model.Initials = parts.Length > 1 ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant() : name[..Math.Min(2, name.Length)].ToUpperInvariant(); }
    private static string? CleanSearch(string? value) { value = value?.Trim(); return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(100, value.Length)]; }
    private static DateTimeOffset ToUtc(DateTime value) =>
        new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), CampusOffset).ToUniversalTime();
    private static DateTime ToCampusTime(DateTimeOffset value) => value.ToOffset(CampusOffset).DateTime;
}
