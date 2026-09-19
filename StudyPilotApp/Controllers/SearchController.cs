using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public sealed class SearchController : Controller
{
    private const int ResultsPerSection = 6;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public SearchController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var profile = await _dbContext.StudentProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ApplicationUserId == user.Id);
        if (profile is null)
            return Problem("A Student profile is not linked to this account. Please contact an administrator.");

        var query = (q ?? string.Empty).Trim();
        if (query.Length > 100) query = query[..100];

        var model = BuildShell(user, profile, query);
        if (query.Length == 0) return View(model);

        var term = query.ToLower();
        var results = new List<StudentSearchResultViewModel>();

        var courses = await _dbContext.Courses
            .AsNoTracking()
            .Where(item =>
                item.ApplicationUserId == user.Id &&
                (item.CourseCode.ToLower().Contains(term) ||
                 item.CourseName.ToLower().Contains(term) ||
                 (item.Instructor != null && item.Instructor.ToLower().Contains(term))))
            .OrderBy(item => item.CourseCode)
            .Take(ResultsPerSection)
            .Select(item => new { item.Id, item.CourseCode, item.CourseName })
            .ToListAsync();
        results.AddRange(courses.Select(item => new StudentSearchResultViewModel
        {
            Type = "Course",
            Title = $"{item.CourseCode} · {item.CourseName}",
            Subtitle = "Open course details and academic progress.",
            Icon = "bi-journal-bookmark",
            Controller = "Courses",
            Action = "Details",
            RouteId = item.Id
        }));

        var assessments = await _dbContext.Assessments
            .AsNoTracking()
            .Include(item => item.Course)
            .Where(item =>
                item.ApplicationUserId == user.Id &&
                (item.Title.ToLower().Contains(term) ||
                 item.Course.CourseCode.ToLower().Contains(term) ||
                 item.Course.CourseName.ToLower().Contains(term)))
            .OrderBy(item => item.DueDate)
            .Take(ResultsPerSection)
            .Select(item => new { item.Id, item.Title, item.Course.CourseCode, item.DueDate })
            .ToListAsync();
        results.AddRange(assessments.Select(item => new StudentSearchResultViewModel
        {
            Type = "Assessment",
            Title = item.Title,
            Subtitle = $"{item.CourseCode} · Due {item.DueDate:dd MMM yyyy, hh:mm tt}",
            Icon = "bi-clipboard2-check",
            Controller = "Assessments",
            Action = "Details",
            RouteId = item.Id
        }));

        var resources = await _dbContext.StudyResources
            .AsNoTracking()
            .Include(item => item.Course)
            .Where(item =>
                item.ApplicationUserId == user.Id &&
                (item.Title.ToLower().Contains(term) ||
                 (item.Description != null && item.Description.ToLower().Contains(term)) ||
                 (item.Tags != null && item.Tags.ToLower().Contains(term)) ||
                 (item.Course != null && item.Course.CourseCode.ToLower().Contains(term))))
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Take(ResultsPerSection)
            .Select(item => new { item.Id, item.Title, item.Kind, CourseCode = item.Course == null ? null : item.Course.CourseCode })
            .ToListAsync();
        results.AddRange(resources.Select(item => new StudentSearchResultViewModel
        {
            Type = "Resource",
            Title = item.Title,
            Subtitle = $"{item.Kind}{(item.CourseCode is null ? string.Empty : $" · {item.CourseCode}")}",
            Icon = "bi-collection-play",
            Controller = "Resources",
            Action = "Details",
            RouteId = item.Id
        }));

        var now = DateTimeOffset.UtcNow;
        var events = await _dbContext.CampusEvents
            .AsNoTracking()
            .Where(item =>
                item.IsPublished &&
                item.EndAt >= now &&
                (item.Title.ToLower().Contains(term) ||
                 item.ShortDescription.ToLower().Contains(term) ||
                 item.OrganizerName.ToLower().Contains(term) ||
                 (item.Venue != null && item.Venue.ToLower().Contains(term))))
            .OrderBy(item => item.StartAt)
            .Take(ResultsPerSection)
            .Select(item => new { item.Id, item.Title, item.OrganizerName, item.StartAt })
            .ToListAsync();
        results.AddRange(events.Select(item => new StudentSearchResultViewModel
        {
            Type = "Event",
            Title = item.Title,
            Subtitle = $"{item.OrganizerName} · {item.StartAt.ToLocalTime():dd MMM yyyy}",
            Icon = "bi-calendar-event",
            Controller = "Events",
            Action = "Details",
            RouteId = item.Id
        }));

        var posts = await _dbContext.CommunityPosts
            .AsNoTracking()
            .Where(item =>
                item.Title.ToLower().Contains(term) ||
                item.Content.ToLower().Contains(term))
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Take(ResultsPerSection)
            .Select(item => new { item.Id, item.Title, item.Category })
            .ToListAsync();
        results.AddRange(posts.Select(item => new StudentSearchResultViewModel
        {
            Type = "Community",
            Title = item.Title,
            Subtitle = item.Category.ToString(),
            Icon = "bi-people",
            Controller = "Community",
            Action = "Details",
            RouteId = item.Id
        }));

        model.Results = results;
        return View(model);
    }

    private static StudentSearchViewModel BuildShell(
        ApplicationUser user,
        StudentProfile profile,
        string query)
    {
        var fullName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.Email ?? "Student"
            : user.FullName;

        return new StudentSearchViewModel
        {
            Query = query,
            FullName = fullName,
            Email = user.Email ?? string.Empty,
            StudentId = profile.StudentId,
            Initials = CreateInitials(fullName),
            DepartmentLabel = profile.Department ?? "Department not set",
            SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set",
            HasProfileImage = profile.ProfileImageData is not null,
            ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds()
        };
    }

    private static string CreateInitials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "ST",
            1 => words[0][..1].ToUpperInvariant(),
            _ => string.Concat(words[0][0], words[^1][0]).ToUpperInvariant()
        };
    }
}
