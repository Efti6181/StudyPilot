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
public sealed class CoursesController : Controller
{
    private static readonly HashSet<string> AllowedGrades = new(StringComparer.OrdinalIgnoreCase)
        { "A+", "A", "A-", "B+", "B", "B-", "C+", "C", "D", "F" };
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
        { "code", "name", "credits", "progress", "newest" };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICourseService _courseService;

    public CoursesController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ICourseService courseService)
    { _userManager = userManager; _dbContext = dbContext; _courseService = courseService; }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CourseStatus? status, CourseType? type, string sort = "code")
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge();
        search = NormalizeSearch(search); status = IsDefined(status) ? status : null; type = IsDefined(type) ? type : null;
        sort = AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "code";
        var courses = await _courseService.GetOwnedCoursesAsync(user.Id, search, status, type, sort);
        var summary = await _courseService.GetSummaryAsync(user.Id);
        var model = new CourseListViewModel
        {
            Courses = courses.Select(ToCard).ToList(), Search = search, Status = status, Type = type, Sort = sort,
            ActiveCourses = summary.ActiveCourses, ActiveCredits = summary.ActiveCredits,
            AverageProgress = summary.AverageProgress, NeedsAttention = summary.NeedsAttention
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile(); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge();
        var course = await _courseService.GetOwnedCourseAsync(user.Id, id); if (course is null) return NotFound();
        var model = new CourseDetailsViewModel { Course = ToCard(course), AssessmentCount = await _courseService.GetAssessmentCountAsync(user.Id, id), CourseGradeCount = await _courseService.GetCourseGradeCountAsync(user.Id, id) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile(); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge();
        var model = new CourseFormViewModel { Status = CourseStatus.Active, Color = CourseColor.Blue, ProgressPercentage = 0 };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        await PopulateEnrollmentOptionsAsync(model, user.Id); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge();
        ValidateStudentOptions(model);
        var assignment = await ResolveActiveAssignmentAsync(model, user.Id);
        if (assignment is not null) ApplyCanonicalToModel(model, assignment);
        if (ModelState.IsValid && await EnrollmentExistsAsync(user.Id, model.FacultyCourseAssignmentId!.Value))
            ModelState.AddModelError(nameof(model.FacultyCourseAssignmentId), "You already added this course with the selected instructor.");
        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            await PopulateEnrollmentOptionsAsync(model, user.Id); return View(model);
        }

        var course = new Course { ApplicationUserId = user.Id };
        ApplyCatalog(course, assignment!); ApplyStudentFields(course, model);
        await _courseService.AddAsync(course);
        try { await _courseService.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The course could not be saved. It may already exist in your course list."); if (!await PopulateShellAsync(model, user)) return MissingStudentProfile(); await PopulateEnrollmentOptionsAsync(model, user.Id); return View(model); }
        TempData["CourseSuccess"] = $"{course.CourseCode} was added with {course.Instructor}.";
        return RedirectToAction(nameof(Details), new { id = course.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge();
        var course = await _courseService.GetOwnedCourseAsync(user.Id, id); if (course is null) return NotFound();
        var model = ToForm(course); if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        await PopulateEnrollmentOptionsAsync(model, user.Id); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel model)
    {
        if (model.Id != id) return BadRequest(); var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge();
        var course = await _courseService.GetOwnedCourseAsync(user.Id, id, true); if (course is null) return NotFound();
        model.IsCatalogLocked = course.FacultyCourseAssignmentId.HasValue;
        FacultyCourseAssignment? assignment;
        if (model.IsCatalogLocked)
        {
            model.CatalogCourseId = course.CatalogCourseId; model.FacultyCourseAssignmentId = course.FacultyCourseAssignmentId;
            assignment = await GetAssignmentAsync(course.FacultyCourseAssignmentId!.Value);
            if (assignment is null) ModelState.AddModelError(string.Empty, "The linked Admin course assignment no longer exists. Contact an administrator.");
        }
        else assignment = await ResolveActiveAssignmentAsync(model, user.Id);
        ValidateStudentOptions(model);
        if (assignment is not null) ApplyCanonicalToModel(model, assignment);
        if (ModelState.IsValid && await EnrollmentExistsAsync(user.Id, model.FacultyCourseAssignmentId!.Value, id))
            ModelState.AddModelError(nameof(model.FacultyCourseAssignmentId), "You already added this course with the selected instructor.");
        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            await PopulateEnrollmentOptionsAsync(model, user.Id); return View(model);
        }
        ApplyCatalog(course, assignment!); ApplyStudentFields(course, model); course.UpdatedAt = DateTimeOffset.UtcNow;
        try { await _courseService.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The course could not be updated."); if (!await PopulateShellAsync(model, user)) return MissingStudentProfile(); await PopulateEnrollmentOptionsAsync(model, user.Id); return View(model); }
        TempData["CourseSuccess"] = $"{course.CourseCode} was updated successfully."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Instructors(int catalogCourseId)
    {
        var userId = _userManager.GetUserId(User); if (userId is null) return Challenge();
        var profile = await _dbContext.StudentProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == userId);
        if (profile is null || !await IsCatalogEligibleAsync(profile, catalogCourseId)) return Json(Array.Empty<object>());
        var rows = await ActiveAssignments(catalogCourseId)
            .OrderBy(x => x.FacultyProfile.ApplicationUser.FullName)
            .Select(x => new
            {
                x.Id,
                FacultyName = x.FacultyProfile.ApplicationUser.FullName,
                x.AcademicPeriod.Term,
                x.AcademicPeriod.AcademicYear,
                x.Section
            })
            .ToListAsync();

        return Json(rows.Select(x => new
        {
            id = x.Id,
            label = $"{x.FacultyName} · {x.Term} {x.AcademicYear} · Section {x.Section}",
            term = x.Term.ToString(),
            year = x.AcademicYear,
            section = x.Section
        }));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge(); var course = await _courseService.GetOwnedCourseAsync(user.Id, id); if (course is null) return NotFound();
        var model = new CourseDeleteViewModel { Course = ToCard(course), RelatedAssessmentCount = await _courseService.GetAssessmentCountAsync(user.Id, id), RelatedCourseGradeCount = await _courseService.GetCourseGradeCountAsync(user.Id, id) };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile(); return View(model);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User); if (user is null) return Challenge(); var course = await _courseService.GetOwnedCourseAsync(user.Id, id, true); if (course is null) return NotFound();
        var assessments = await _courseService.GetAssessmentCountAsync(user.Id, id); var grades = await _courseService.GetCourseGradeCountAsync(user.Id, id);
        if (assessments > 0 || grades > 0) { TempData["CourseError"] = $"This course has {assessments} assessment(s) and {grades} grade record(s). Remove those records before deleting it."; return RedirectToAction(nameof(Delete), new { id }); }
        _courseService.Remove(course); await _courseService.SaveChangesAsync(); TempData["CourseSuccess"] = $"{course.CourseCode} was deleted."; return RedirectToAction(nameof(Index));
    }

    private async Task<FacultyCourseAssignment?> ResolveActiveAssignmentAsync(CourseFormViewModel model, string userId)
    {
        if (!model.CatalogCourseId.HasValue || !model.FacultyCourseAssignmentId.HasValue) return null;
        var profile = await _dbContext.StudentProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == userId);
        if (profile is null || !profile.DepartmentId.HasValue || !profile.AcademicProgramId.HasValue)
        {
            ModelState.AddModelError(nameof(model.CatalogCourseId), "Select your Admin-managed Department and Program in My Profile first."); return null;
        }
        var assignment = await ActiveAssignments(model.CatalogCourseId.Value).SingleOrDefaultAsync(x => x.Id == model.FacultyCourseAssignmentId.Value);
        if (assignment is null) { ModelState.AddModelError(nameof(model.FacultyCourseAssignmentId), "Select an active instructor assigned to this course."); return null; }
        if (assignment.CatalogCourse.DepartmentId != profile.DepartmentId || (assignment.CatalogCourse.ProgramId.HasValue && assignment.CatalogCourse.ProgramId != profile.AcademicProgramId))
        { ModelState.AddModelError(nameof(model.CatalogCourseId), "This course is not available for your selected Department and Program."); return null; }
        if (!assignment.CatalogCourse.RecommendedSemester.HasValue)
        { ModelState.AddModelError(nameof(model.CatalogCourseId), "The administrator must configure this course's semester before students can add it."); return null; }
        return assignment;
    }

    private Task<FacultyCourseAssignment?> GetAssignmentAsync(int id) => _dbContext.FacultyCourseAssignments.Include(x => x.CatalogCourse).Include(x => x.AcademicPeriod).Include(x => x.FacultyProfile).ThenInclude(x => x.ApplicationUser).SingleOrDefaultAsync(x => x.Id == id);
    private IQueryable<FacultyCourseAssignment> ActiveAssignments(int catalogCourseId) => _dbContext.FacultyCourseAssignments.AsNoTracking().Include(x => x.CatalogCourse).Include(x => x.AcademicPeriod).Include(x => x.FacultyProfile).ThenInclude(x => x.ApplicationUser).Where(x => x.IsActive && x.CatalogCourseId == catalogCourseId && x.CatalogCourse.IsActive && x.AcademicPeriod.IsActive && x.FacultyProfile.ApplicationUser.IsActive);
    private async Task<bool> IsCatalogEligibleAsync(StudentProfile profile, int catalogId) => profile.DepartmentId.HasValue && profile.AcademicProgramId.HasValue && await _dbContext.CatalogCourses.AnyAsync(x => x.Id == catalogId && x.IsActive && x.DepartmentId == profile.DepartmentId && (!x.ProgramId.HasValue || x.ProgramId == profile.AcademicProgramId));
    private Task<bool> EnrollmentExistsAsync(string userId, int assignmentId, int? excludedId = null) => _dbContext.Courses.AnyAsync(x => x.ApplicationUserId == userId && x.FacultyCourseAssignmentId == assignmentId && (!excludedId.HasValue || x.Id != excludedId));

    private async Task PopulateEnrollmentOptionsAsync(CourseFormViewModel model, string userId)
    {
        var profile = await _dbContext.StudentProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == userId);
        if (profile?.DepartmentId is null || profile.AcademicProgramId is null) { model.CatalogOptions = []; model.InstructorOptions = []; return; }
        var catalogs = await _dbContext.CatalogCourses.AsNoTracking()
            .Where(x => x.IsActive && x.RecommendedSemester.HasValue && x.DepartmentId == profile.DepartmentId && (!x.ProgramId.HasValue || x.ProgramId == profile.AcademicProgramId) && x.FacultyAssignments.Any(a => a.IsActive && a.AcademicPeriod.IsActive && a.FacultyProfile.ApplicationUser.IsActive))
            .OrderBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Name, x.CreditHours, x.CourseType, Semester = x.RecommendedSemester!.Value, x.Description }).ToListAsync();
        model.CatalogOptions = catalogs.Select(x => new CatalogCourseSelectionOption(x.Id, $"{x.Code} — {x.Name}", x.Code, x.Name, x.CreditHours, x.CourseType, x.Semester, x.Description)).ToList();
        if (model.CatalogCourseId.HasValue)
        {
            var assignments = await ActiveAssignments(model.CatalogCourseId.Value).OrderBy(x => x.FacultyProfile.ApplicationUser.FullName).ToListAsync();
            model.InstructorOptions = assignments.Select(x => new SelectListItem($"{x.FacultyProfile.ApplicationUser.FullName} · {x.AcademicPeriod.Term} {x.AcademicPeriod.AcademicYear} · Section {x.Section}", x.Id.ToString(), x.Id == model.FacultyCourseAssignmentId)).ToList();
        }
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user)
    {
        var profile = await _dbContext.StudentProfiles.AsNoTracking().Where(x => x.ApplicationUserId == user.Id).Select(x => new { x.StudentId, x.Department, x.Semester, HasImage = x.ProfileImageData != null, x.CreatedAt, x.UpdatedAt }).SingleOrDefaultAsync();
        if (profile is null) return false; var name = string.IsNullOrWhiteSpace(user.FullName) ? user.Email ?? "Student" : user.FullName;
        model.FullName = name; model.Email = user.Email ?? ""; model.StudentId = profile.StudentId; model.Initials = Initials(name); model.DepartmentLabel = profile.Department ?? "Department not set"; model.SemesterLabel = profile.Semester.HasValue ? $"Semester {profile.Semester}" : "Semester not set"; model.HasProfileImage = profile.HasImage; model.ProfileImageVersion = (profile.UpdatedAt ?? profile.CreatedAt).ToUnixTimeSeconds(); return true;
    }

    private void ValidateStudentOptions(CourseFormViewModel model)
    {
        if (model.Status.HasValue && !Enum.IsDefined(model.Status.Value)) ModelState.AddModelError(nameof(model.Status), "Select a valid status.");
        if (model.Color.HasValue && !Enum.IsDefined(model.Color.Value)) ModelState.AddModelError(nameof(model.Color), "Select a valid color.");
        model.TargetGrade = NormalizeOptional(model.TargetGrade)?.ToUpperInvariant(); if (model.TargetGrade is not null && !AllowedGrades.Contains(model.TargetGrade)) ModelState.AddModelError(nameof(model.TargetGrade), "Select a valid target grade.");
    }

    private static void ApplyCanonicalToModel(CourseFormViewModel model, FacultyCourseAssignment assignment) { var c = assignment.CatalogCourse; model.CatalogCourseId = c.Id; model.FacultyCourseAssignmentId = assignment.Id; model.CourseCode = c.Code; model.CourseName = c.Name; model.CreditHours = c.CreditHours; model.Instructor = assignment.FacultyProfile.ApplicationUser.FullName; model.Semester = c.RecommendedSemester; model.AcademicTerm = assignment.AcademicPeriod.Term; model.AcademicYear = assignment.AcademicPeriod.AcademicYear; model.CourseType = c.CourseType; model.Description = c.Description; }
    private static void ApplyCatalog(Course course, FacultyCourseAssignment assignment) { var c = assignment.CatalogCourse; course.CatalogCourseId = c.Id; course.FacultyCourseAssignmentId = assignment.Id; course.CourseCode = c.Code; course.CourseName = c.Name; course.CreditHours = c.CreditHours; course.Instructor = assignment.FacultyProfile.ApplicationUser.FullName; course.Semester = c.RecommendedSemester!.Value; course.AcademicTerm = assignment.AcademicPeriod.Term; course.AcademicYear = assignment.AcademicPeriod.AcademicYear; course.CourseType = c.CourseType; course.Description = c.Description; }
    private static void ApplyStudentFields(Course course, CourseFormViewModel model) { course.TargetGrade = NormalizeOptional(model.TargetGrade)?.ToUpperInvariant(); course.Status = model.Status!.Value; course.Color = model.Color!.Value; course.ProgressPercentage = model.ProgressPercentage!.Value; }
    private static CourseFormViewModel ToForm(Course c) => new() { Id = c.Id, CatalogCourseId = c.CatalogCourseId, FacultyCourseAssignmentId = c.FacultyCourseAssignmentId, IsCatalogLocked = c.FacultyCourseAssignmentId.HasValue, CourseCode = c.CourseCode, CourseName = c.CourseName, CreditHours = c.CreditHours, Instructor = c.Instructor, Semester = c.Semester, AcademicTerm = c.AcademicTerm, AcademicYear = c.AcademicYear, CourseType = c.CourseType, Description = c.Description, TargetGrade = c.TargetGrade, Status = c.Status, Color = c.Color, ProgressPercentage = c.ProgressPercentage };
    private static CourseCardViewModel ToCard(Course c) => new() { Id = c.Id, CourseCode = c.CourseCode, CourseName = c.CourseName, CreditHours = c.CreditHours, Instructor = c.Instructor ?? "Instructor not set", InstructorInitials = Initials(c.Instructor ?? "Instructor"), Semester = c.Semester, AcademicTerm = c.AcademicTerm, AcademicYear = c.AcademicYear, CourseType = c.CourseType, Status = c.Status, Color = c.Color, ProgressPercentage = c.ProgressPercentage, TargetGrade = c.TargetGrade, Description = c.Description };
    private static string? NormalizeSearch(string? value) { value = value?.Trim(); return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(100, value.Length)]; }
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsDefined<T>(T? value) where T : struct, Enum => !value.HasValue || Enum.IsDefined(value.Value);
    private static string Initials(string value) { var p = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); return p.Length == 0 ? "IN" : p.Length == 1 ? p[0][..Math.Min(2, p[0].Length)].ToUpperInvariant() : $"{p[0][0]}{p[^1][0]}".ToUpperInvariant(); }
    private static ObjectResult MissingStudentProfile() => new(new ProblemDetails { Title = "Student profile unavailable", Detail = "A Student profile is not linked to this account. Please contact an administrator.", Status = 500 }) { StatusCode = 500 };
}
