using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private const int MaxProfileImageBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedGenders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Male", "Female", "Other", "Prefer not to say"
        };

    private static readonly HashSet<string> AllowedBloodGroups =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"
        };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEventService _eventService;

    public StudentController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IEventService eventService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var profile = await FindProfileAsync(user.Id);
        if (profile is null)
        {
            return Problem("A Student profile is not linked to this account. Please contact an administrator.");
        }

        var shell = BuildShell(user, profile);
        var hour = DateTimeOffset.Now.Hour;
        var upcomingEvents = await _eventService.GetUpcomingForDashboardAsync(user.Id, 3);

        var model = new StudentDashboardViewModel
        {
            FullName = shell.FullName,
            Email = shell.Email,
            StudentId = shell.StudentId,
            Initials = shell.Initials,
            DepartmentLabel = shell.DepartmentLabel,
            SemesterLabel = shell.SemesterLabel,
            HasProfileImage = shell.HasProfileImage,
            ProfileImageVersion = shell.ProfileImageVersion,
            Greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening",
            UpcomingEvents = upcomingEvents.Select(item => new DashboardEventViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Type = item.Type.ToString(),
                Location = item.LocationType switch
                {
                    EventLocationType.OnCampus => item.Venue ?? "Campus venue",
                    EventLocationType.Online => "Online event",
                    _ => string.IsNullOrWhiteSpace(item.Venue) ? "Hybrid event" : $"{item.Venue} + Online"
                },
                StartAt = item.StartAt,
                IsRegistered = item.Registrations.Any(registration =>
                    registration.ApplicationUserId == user.Id && registration.CancelledAt == null)
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var profile = await FindProfileAsync(user.Id);
        if (profile is null)
        {
            return Problem("A Student profile is not linked to this account. Please contact an administrator.");
        }

        return View(BuildProfileViewModel(user, profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxProfileImageBytes + 200_000)]
    public async Task<IActionResult> Profile(StudentProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var profile = await FindProfileAsync(user.Id);
        if (profile is null)
        {
            return Problem("A Student profile is not linked to this account. Please contact an administrator.");
        }

        ValidateSelections(model);

        byte[]? uploadedImage = null;
        string? uploadedImageContentType = null;

        if (model.ProfileImage is not null)
        {
            (uploadedImage, uploadedImageContentType) = await ValidateAndReadImageAsync(model.ProfileImage);
        }

        if (!ModelState.IsValid)
        {
            PopulateFixedFields(model, user, profile);
            model.CompletionPercentage = CalculateCompletion(model, profile.ProfileImageData is not null);
            return View(model);
        }

        profile.Department = model.Department.Trim();
        profile.Semester = model.Semester;
        profile.Batch = model.Batch.Trim();
        profile.PhoneNumber = NormalizeOptional(model.PhoneNumber);
        profile.GuardianPhoneNumber = NormalizeOptional(model.GuardianPhoneNumber);
        profile.PresentAddress = NormalizeOptional(model.PresentAddress);
        profile.City = NormalizeOptional(model.City);
        profile.DateOfBirth = model.DateOfBirth;
        profile.Gender = NormalizeOptional(model.Gender);
        profile.BloodGroup = NormalizeOptional(model.BloodGroup);
        profile.Bio = NormalizeOptional(model.Bio);

        if (uploadedImage is not null && uploadedImageContentType is not null)
        {
            profile.ProfileImageData = uploadedImage;
            profile.ProfileImageContentType = uploadedImageContentType;
        }
        else if (model.RemoveProfileImage)
        {
            profile.ProfileImageData = null;
            profile.ProfileImageContentType = null;
        }

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        TempData["ProfileSuccess"] = "Your profile was updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> ProfileImage()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var image = await _dbContext.StudentProfiles
            .AsNoTracking()
            .Where(profile => profile.ApplicationUserId == userId)
            .Select(profile => new
            {
                profile.ProfileImageData,
                profile.ProfileImageContentType
            })
            .SingleOrDefaultAsync();

        if (image?.ProfileImageData is null || string.IsNullOrWhiteSpace(image.ProfileImageContentType))
        {
            return NotFound();
        }

        return File(image.ProfileImageData, image.ProfileImageContentType);
    }

    private Task<StudentProfile?> FindProfileAsync(string userId) =>
        _dbContext.StudentProfiles.SingleOrDefaultAsync(profile => profile.ApplicationUserId == userId);

    private static StudentShellViewModel BuildShell(ApplicationUser user, StudentProfile profile)
    {
        var fullName = string.IsNullOrWhiteSpace(user.FullName)
            ? user.Email ?? "Student"
            : user.FullName;

        return new StudentShellViewModel
        {
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

    private static StudentProfileViewModel BuildProfileViewModel(ApplicationUser user, StudentProfile profile)
    {
        var shell = BuildShell(user, profile);
        var model = new StudentProfileViewModel
        {
            FullName = shell.FullName,
            Email = shell.Email,
            StudentId = shell.StudentId,
            Initials = shell.Initials,
            DepartmentLabel = shell.DepartmentLabel,
            SemesterLabel = shell.SemesterLabel,
            HasProfileImage = shell.HasProfileImage,
            ProfileImageVersion = shell.ProfileImageVersion,
            Department = profile.Department ?? string.Empty,
            Semester = profile.Semester,
            Batch = profile.Batch ?? string.Empty,
            PhoneNumber = profile.PhoneNumber,
            GuardianPhoneNumber = profile.GuardianPhoneNumber,
            PresentAddress = profile.PresentAddress,
            City = profile.City,
            DateOfBirth = profile.DateOfBirth,
            Gender = profile.Gender,
            BloodGroup = profile.BloodGroup,
            Bio = profile.Bio
        };

        model.CompletionPercentage = CalculateCompletion(model, shell.HasProfileImage);
        return model;
    }

    private static void PopulateFixedFields(
        StudentProfileViewModel model,
        ApplicationUser user,
        StudentProfile profile)
    {
        var shell = BuildShell(user, profile);
        model.FullName = shell.FullName;
        model.Email = shell.Email;
        model.StudentId = shell.StudentId;
        model.Initials = shell.Initials;
        model.DepartmentLabel = shell.DepartmentLabel;
        model.SemesterLabel = shell.SemesterLabel;
        model.HasProfileImage = shell.HasProfileImage;
        model.ProfileImageVersion = shell.ProfileImageVersion;
    }

    private void ValidateSelections(StudentProfileViewModel model)
    {
        if (model.DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dateOfBirth = model.DateOfBirth.Value;
            if (dateOfBirth > today || dateOfBirth < new DateOnly(1900, 1, 1))
            {
                ModelState.AddModelError(nameof(model.DateOfBirth), "Please enter a valid date of birth.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Gender) && !AllowedGenders.Contains(model.Gender))
        {
            ModelState.AddModelError(nameof(model.Gender), "Please select a valid gender option.");
        }

        if (!string.IsNullOrWhiteSpace(model.BloodGroup) && !AllowedBloodGroups.Contains(model.BloodGroup))
        {
            ModelState.AddModelError(nameof(model.BloodGroup), "Please select a valid blood group.");
        }
    }

    private async Task<(byte[]? Data, string? ContentType)> ValidateAndReadImageAsync(IFormFile image)
    {
        if (image.Length == 0 || image.Length > MaxProfileImageBytes)
        {
            ModelState.AddModelError(nameof(StudentProfileViewModel.ProfileImage), "Choose a JPG, PNG or WebP image smaller than 2 MB.");
            return (null, null);
        }

        await using var stream = new MemoryStream();
        await image.CopyToAsync(stream);

        if (stream.Length > MaxProfileImageBytes)
        {
            ModelState.AddModelError(nameof(StudentProfileViewModel.ProfileImage), "The selected image is larger than 2 MB.");
            return (null, null);
        }

        var data = stream.ToArray();
        var detectedContentType = DetectImageContentType(data);
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var expectedContentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (detectedContentType is null || expectedContentType != detectedContentType)
        {
            ModelState.AddModelError(nameof(StudentProfileViewModel.ProfileImage), "The file is not a valid JPG, PNG or WebP image.");
            return (null, null);
        }

        return (data, detectedContentType);
    }

    private static string? DetectImageContentType(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (data.Length >= pngSignature.Length && data[..pngSignature.Length].SequenceEqual(pngSignature))
        {
            return "image/png";
        }

        if (data.Length >= 12 &&
            data[..4].SequenceEqual("RIFF"u8) &&
            data.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    private static int CalculateCompletion(StudentProfileViewModel model, bool hasImage)
    {
        var completed = 0;
        completed += !string.IsNullOrWhiteSpace(model.Department) ? 1 : 0;
        completed += model.Semester.HasValue ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(model.Batch) ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(model.PhoneNumber) ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(model.PresentAddress) ? 1 : 0;
        completed += model.DateOfBirth.HasValue ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(model.Gender) ? 1 : 0;
        completed += !string.IsNullOrWhiteSpace(model.Bio) ? 1 : 0;
        completed += hasImage ? 1 : 0;
        return (int)Math.Round(completed / 9d * 100);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string CreateInitials(string fullName)
    {
        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
