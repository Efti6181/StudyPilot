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
public sealed class CourseAnnouncementsController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly IFacultyAnnouncementService _service;

    public CourseAnnouncementsController(UserManager<ApplicationUser> users, ApplicationDbContext db, IFacultyAnnouncementService service)
    { _users = users; _db = db; _service = service; }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? courseId, AnnouncementPriority? priority, bool activeOnly = true, CancellationToken token = default)
    {
        var user = await _users.GetUserAsync(User); if (user is null) return Challenge(); search = CleanSearch(search);
        if (priority.HasValue && !Enum.IsDefined(priority.Value)) priority = null;
        var courses = await _db.Courses.AsNoTracking().Where(x => x.ApplicationUserId == user.Id).OrderBy(x => x.CourseCode).Select(x => new { x.Id, x.CourseCode, x.CourseName }).ToListAsync(token);
        if (courseId.HasValue && courses.All(x => x.Id != courseId.Value)) courseId = null;
        var items = await _service.SearchForStudentAsync(user.Id, search, courseId, priority, activeOnly, token);
        var model = new CourseAnnouncementIndexViewModel
        {
            Items = items, Search = search, CourseId = courseId, Priority = priority, ActiveOnly = activeOnly,
            UnreadCount = items.Count(x => !x.IsRead),
            CourseOptions = courses.Select(x => new SelectListItem($"{x.CourseCode} — {x.CourseName}", x.Id.ToString(), x.Id == courseId)).ToList()
        };
        if (!await Shell(model, user, token)) return Problem("A Student profile is not linked to this account."); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var user = await _users.GetUserAsync(User); if (user is null) return Challenge();
        var item = await _service.GetForStudentAsync(user.Id, id, false, token); if (item is null) return NotFound();
        var model = new CourseAnnouncementDetailsViewModel { Announcement = item };
        if (!await Shell(model, user, token)) return Problem("A Student profile is not linked to this account."); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken token)
    {
        var userId = _users.GetUserId(User); if (userId is null) return Challenge();
        if (await _service.GetForStudentAsync(userId, id, true, token) is null) return NotFound();
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> Shell(StudentShellViewModel model, ApplicationUser user, CancellationToken token)
    {
        var profile = await _db.StudentProfiles.AsNoTracking().Where(x => x.ApplicationUserId == user.Id).Select(x => new { x.StudentId, x.Department, x.Semester, HasImage = x.ProfileImageData != null, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync(token);
        if (profile is null) return false; var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Student" : user.FullName; var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        model.FullName = name; model.Email = user.Email ?? ""; model.StudentId = profile.StudentId; model.DepartmentLabel = profile.Department ?? "Department not set"; model.SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set"; model.HasProfileImage = profile.HasImage; model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds(); model.Initials = parts.Length > 1 ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant() : name[..Math.Min(2, name.Length)].ToUpperInvariant(); return true;
    }
    private static string? CleanSearch(string? value) { value = value?.Trim(); return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(100, value.Length)]; }
}
