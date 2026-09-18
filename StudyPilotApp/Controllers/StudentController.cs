using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public StudentController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var studentId = await _dbContext.StudentProfiles
            .Where(profile => profile.ApplicationUserId == user.Id)
            .Select(profile => profile.StudentId)
            .SingleOrDefaultAsync();

        var fullName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.Email ?? "Student"
            : user.FullName;

        var hour = DateTimeOffset.Now.Hour;
        var greeting = hour < 12
            ? "Good morning"
            : hour < 18
                ? "Good afternoon"
                : "Good evening";

        var model = new StudentDashboardViewModel
        {
            FullName = fullName,
            Email = user.Email ?? string.Empty,
            StudentId = studentId ?? "Not assigned",
            Initials = CreateInitials(fullName),
            Greeting = greeting
        };

        return View(model);
    }

    private static string CreateInitials(string fullName)
    {
        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "ST";
        }

        if (parts.Length == 1)
        {
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        }

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
