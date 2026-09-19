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
public sealed class GpaController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IGpaService _gpaService;

    public GpaController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IGpaService gpaService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _gpaService = gpaService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        decimal? goalCurrentCgpa,
        decimal? goalCompletedCredits,
        decimal? targetCgpa,
        decimal? futureCredits)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        var scale = await _gpaService.GetScaleAsync(user.Id);
        var semesters = await _gpaService.GetSemestersAsync(user.Id);
        var lookup = BuildScaleLookup(scale);

        var actual = CalculateAcross(semesters, lookup, useExpectedFallback: false);
        var projected = CalculateAcross(semesters, lookup, useExpectedFallback: true);
        var maximum = scale.Count == 0 ? 4m : scale.Max(item => item.GradePoint);

        var model = new GpaDashboardViewModel
        {
            Semesters = semesters.Select(item => ToSummary(item, lookup)).ToList(),
            CurrentCgpa = actual.Gpa,
            ProjectedCgpa = projected.Gpa,
            CompletedCredits = actual.Credits,
            CompletedCourses = semesters.SelectMany(item => item.CourseGrades).Count(item => !string.IsNullOrWhiteSpace(item.ActualLetterGrade)),
            MaximumGradePoint = maximum,
            GoalCurrentCgpa = goalCurrentCgpa ?? actual.Gpa,
            GoalCompletedCredits = goalCompletedCredits ?? actual.Credits,
            TargetCgpa = targetCgpa,
            FutureCredits = futureCredits
        };

        if (targetCgpa.HasValue || futureCredits.HasValue)
        {
            model.GoalCalculated = true;
            CalculateGoal(model, maximum);
        }

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateSemester()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var model = new SemesterFormViewModel
        {
            AcademicYear = DateTime.UtcNow.Year,
            AcademicTerm = CurrentTerm()
        };
        if (!await PopulateShellAsync(model, user, useProfileSemester: true)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSemester(SemesterFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        ValidateSemester(model);
        if (ModelState.IsValid && await _gpaService.SemesterDuplicateExistsAsync(
                user.Id, model.SemesterNumber!.Value, model.AcademicTerm!.Value, model.AcademicYear!.Value))
        {
            ModelState.AddModelError(string.Empty, "This semester record already exists.");
        }

        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        var semester = new SemesterResult
        {
            ApplicationUserId = user.Id,
            SemesterNumber = model.SemesterNumber!.Value,
            AcademicTerm = model.AcademicTerm!.Value,
            AcademicYear = model.AcademicYear!.Value,
            Notes = NormalizeOptional(model.Notes)
        };
        await _gpaService.AddSemesterAsync(semester);
        await _gpaService.SaveChangesAsync();

        TempData["GpaSuccess"] = "Semester created. Now add its course grades.";
        return RedirectToAction(nameof(Semester), new { id = semester.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditSemester(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var semester = await _gpaService.GetSemesterAsync(user.Id, id);
        if (semester is null) return NotFound();

        var model = new SemesterFormViewModel
        {
            Id = semester.Id,
            SemesterNumber = semester.SemesterNumber,
            AcademicTerm = semester.AcademicTerm,
            AcademicYear = semester.AcademicYear,
            Notes = semester.Notes
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSemester(int id, SemesterFormViewModel model)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var semester = await _gpaService.GetSemesterAsync(user.Id, id, trackChanges: true);
        if (semester is null) return NotFound();

        ValidateSemester(model);
        if (ModelState.IsValid && await _gpaService.SemesterDuplicateExistsAsync(
                user.Id, model.SemesterNumber!.Value, model.AcademicTerm!.Value, model.AcademicYear!.Value, id))
        {
            ModelState.AddModelError(string.Empty, "This semester record already exists.");
        }

        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        semester.SemesterNumber = model.SemesterNumber!.Value;
        semester.AcademicTerm = model.AcademicTerm!.Value;
        semester.AcademicYear = model.AcademicYear!.Value;
        semester.Notes = NormalizeOptional(model.Notes);
        semester.UpdatedAt = DateTimeOffset.UtcNow;
        await _gpaService.SaveChangesAsync();

        TempData["GpaSuccess"] = "Semester information was updated.";
        return RedirectToAction(nameof(Semester), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Semester(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);

        var semester = await _gpaService.GetSemesterAsync(user.Id, id);
        if (semester is null) return NotFound();
        var scale = await _gpaService.GetScaleAsync(user.Id);
        var lookup = BuildScaleLookup(scale);
        var actual = CalculateSemester(semester, lookup, false);
        var projected = CalculateSemester(semester, lookup, true);

        var model = new SemesterDetailsViewModel
        {
            Id = semester.Id,
            SemesterNumber = semester.SemesterNumber,
            AcademicTerm = semester.AcademicTerm,
            AcademicYear = semester.AcademicYear,
            Notes = semester.Notes,
            SemesterGpa = actual.Gpa,
            ProjectedGpa = projected.Gpa,
            ActualCredits = actual.Credits,
            PlannedCredits = semester.CourseGrades.Sum(item => item.Course.CreditHours),
            Grades = semester.CourseGrades
                .OrderBy(item => item.Course.CourseCode)
                .Select(item => ToGradeRow(item, lookup))
                .ToList()
        };

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AddGrade(int semesterId, int? courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);

        var semester = await _gpaService.GetSemesterAsync(user.Id, semesterId);
        if (semester is null) return NotFound();

        if (courseId.HasValue && (!await _gpaService.OwnsCourseAsync(user.Id, courseId.Value) ||
            await _gpaService.CourseAlreadyAddedAsync(user.Id, semesterId, courseId.Value)))
        {
            courseId = null;
        }

        var model = new CourseGradeFormViewModel
        {
            SemesterResultId = semester.Id,
            SemesterLabel = SemesterLabel(semester),
            CourseId = courseId
        };

        await PopulateGradeFormAsync(model, user.Id);
        if (courseId.HasValue)
        {
            var course = (await _gpaService.GetAvailableCoursesAsync(user.Id, semesterId, courseId))
                .SingleOrDefault(item => item.Id == courseId);
            if (course is not null) model.ExpectedLetterGrade = course.TargetGrade ?? string.Empty;
        }
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGrade(CourseGradeFormViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);

        var semester = await _gpaService.GetSemesterAsync(user.Id, model.SemesterResultId);
        if (semester is null) return NotFound();
        model.SemesterLabel = SemesterLabel(semester);

        await ValidateGradeFormAsync(model, user.Id);
        if (!ModelState.IsValid)
        {
            await PopulateGradeFormAsync(model, user.Id);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        var grade = new CourseGrade
        {
            ApplicationUserId = user.Id,
            SemesterResultId = model.SemesterResultId,
            CourseId = model.CourseId!.Value,
            ExpectedLetterGrade = NormalizeGrade(model.ExpectedLetterGrade)!,
            ActualLetterGrade = NormalizeGrade(model.ActualLetterGrade),
            Notes = NormalizeOptional(model.Notes)
        };
        await _gpaService.AddCourseGradeAsync(grade);
        await _gpaService.SaveChangesAsync();

        TempData["GpaSuccess"] = "Course grade was added.";
        return RedirectToAction(nameof(Semester), new { id = model.SemesterResultId });
    }

    [HttpGet]
    public async Task<IActionResult> EditGrade(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        var grade = await _gpaService.GetCourseGradeAsync(user.Id, id);
        if (grade is null) return NotFound();

        var model = new CourseGradeFormViewModel
        {
            Id = grade.Id,
            SemesterResultId = grade.SemesterResultId,
            SemesterLabel = SemesterLabel(grade.SemesterResult),
            CourseId = grade.CourseId,
            ExpectedLetterGrade = grade.ExpectedLetterGrade,
            ActualLetterGrade = grade.ActualLetterGrade,
            Notes = grade.Notes
        };
        await PopulateGradeFormAsync(model, user.Id, grade.CourseId);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGrade(int id, CourseGradeFormViewModel model)
    {
        if (model.Id != id) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        var grade = await _gpaService.GetCourseGradeAsync(user.Id, id, trackChanges: true);
        if (grade is null) return NotFound();

        model.SemesterResultId = grade.SemesterResultId;
        model.SemesterLabel = SemesterLabel(grade.SemesterResult);
        await ValidateGradeFormAsync(model, user.Id, id);
        if (!ModelState.IsValid)
        {
            await PopulateGradeFormAsync(model, user.Id, grade.CourseId);
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        grade.CourseId = model.CourseId!.Value;
        grade.ExpectedLetterGrade = NormalizeGrade(model.ExpectedLetterGrade)!;
        grade.ActualLetterGrade = NormalizeGrade(model.ActualLetterGrade);
        grade.Notes = NormalizeOptional(model.Notes);
        grade.UpdatedAt = DateTimeOffset.UtcNow;
        await _gpaService.SaveChangesAsync();

        TempData["GpaSuccess"] = "Course grade was updated.";
        return RedirectToAction(nameof(Semester), new { id = grade.SemesterResultId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGrade(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var grade = await _gpaService.GetCourseGradeAsync(user.Id, id, trackChanges: true);
        if (grade is null) return NotFound();
        var semesterId = grade.SemesterResultId;
        _gpaService.RemoveCourseGrade(grade);
        await _gpaService.SaveChangesAsync();
        TempData["GpaSuccess"] = "Course grade was removed.";
        return RedirectToAction(nameof(Semester), new { id = semesterId });
    }

    [HttpGet]
    public async Task<IActionResult> DeleteSemester(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var semester = await _gpaService.GetSemesterAsync(user.Id, id);
        if (semester is null) return NotFound();
        var model = new SemesterDeleteViewModel
        {
            Id = semester.Id,
            SemesterLabel = SemesterLabel(semester),
            CourseCount = semester.CourseGrades.Count
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost, ActionName("DeleteSemester")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSemesterConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var semester = await _gpaService.GetSemesterAsync(user.Id, id, trackChanges: true);
        if (semester is null) return NotFound();
        _gpaService.RemoveSemester(semester);
        await _gpaService.SaveChangesAsync();
        TempData["GpaSuccess"] = "Semester and its saved course grades were deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Calculator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        var model = new GpaCalculatorViewModel
        {
            Rows = Enumerable.Range(0, 6).Select(_ => new GpaCalculatorRowViewModel()).ToList()
        };
        await PopulateCalculatorOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculator(GpaCalculatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        model.Rows = (model.Rows ?? []).Take(12).ToList();
        var scale = await _gpaService.GetScaleAsync(user.Id);
        var lookup = BuildScaleLookup(scale);
        var inputs = new List<(decimal Credits, decimal GradePoint)>();

        for (var index = 0; index < model.Rows.Count; index++)
        {
            var row = model.Rows[index];
            var hasAny = !string.IsNullOrWhiteSpace(row.CourseName) || row.Credits.HasValue || !string.IsNullOrWhiteSpace(row.LetterGrade);
            if (!hasAny) continue;
            if (!row.Credits.HasValue || row.Credits.Value <= 0)
                ModelState.AddModelError($"Rows[{index}].Credits", "Enter valid credits.");
            var normalized = NormalizeGrade(row.LetterGrade);
            if (normalized is null || !lookup.TryGetValue(normalized, out var point))
                ModelState.AddModelError($"Rows[{index}].LetterGrade", "Select a valid grade.");
            else if (row.Credits.HasValue && row.Credits.Value > 0)
                inputs.Add((row.Credits.Value, point));
        }

        if (inputs.Count == 0) ModelState.AddModelError(string.Empty, "Enter at least one complete course row.");
        if (ModelState.IsValid)
        {
            var result = GpaCalculator.Calculate(inputs);
            model.HasResult = true;
            model.CalculatedGpa = result.Gpa;
            model.TotalCredits = result.Credits;
        }

        while (model.Rows.Count < 6) model.Rows.Add(new GpaCalculatorRowViewModel());
        await PopulateCalculatorOptionsAsync(model, user.Id);
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CgpaCalculator()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);

        var scale = await _gpaService.GetScaleAsync(user.Id);
        var maximum = scale.Count == 0 ? 4m : scale.Max(item => item.GradePoint);
        var lookup = BuildScaleLookup(scale);
        var semesters = await _gpaService.GetSemestersAsync(user.Id);
        var savedRows = semesters
            .OrderBy(item => item.AcademicYear)
            .ThenBy(item => item.AcademicTerm)
            .ThenBy(item => item.SemesterNumber)
            .Select(item => new
            {
                Semester = item,
                Result = CalculateSemester(item, lookup, useExpectedFallback: false)
            })
            .Where(item => item.Result.Credits > 0)
            .Select(item => new CgpaSemesterRowViewModel
            {
                SemesterName = SemesterLabel(item.Semester),
                Credits = item.Result.Credits,
                Gpa = item.Result.Gpa
            })
            .Take(16)
            .ToList();

        var model = new CgpaCalculatorViewModel
        {
            Rows = savedRows,
            LoadedSavedSemesters = savedRows.Count,
            MaximumGradePoint = maximum
        };
        var desiredRows = Math.Min(16, Math.Max(6, savedRows.Count + 2));
        while (model.Rows.Count < desiredRows) model.Rows.Add(new CgpaSemesterRowViewModel());

        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CgpaCalculator(CgpaCalculatorViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);

        var scale = await _gpaService.GetScaleAsync(user.Id);
        model.MaximumGradePoint = scale.Count == 0 ? 4m : scale.Max(item => item.GradePoint);
        model.Rows = (model.Rows ?? []).Take(16).ToList();
        var inputs = new List<(decimal Credits, decimal GradePoint)>();

        for (var index = 0; index < model.Rows.Count; index++)
        {
            var row = model.Rows[index];
            var hasAny = !string.IsNullOrWhiteSpace(row.SemesterName) || row.Credits.HasValue || row.Gpa.HasValue;
            if (!hasAny) continue;

            if (!row.Credits.HasValue || row.Credits.Value is < 0.5m or > 100m)
                ModelState.AddModelError($"Rows[{index}].Credits", "Enter credits between 0.5 and 100.");
            if (!row.Gpa.HasValue || row.Gpa.Value < 0 || row.Gpa.Value > model.MaximumGradePoint)
                ModelState.AddModelError($"Rows[{index}].Gpa", $"Enter a GPA between 0 and {model.MaximumGradePoint:0.00}.");

            if (row.Credits.HasValue && row.Credits.Value is >= 0.5m and <= 100m &&
                row.Gpa.HasValue && row.Gpa.Value >= 0m && row.Gpa.Value <= model.MaximumGradePoint)
                inputs.Add((row.Credits.Value, row.Gpa.Value));
        }

        if (inputs.Count == 0)
            ModelState.AddModelError(string.Empty, "Enter at least one complete semester row.");

        if (ModelState.IsValid)
        {
            var result = GpaCalculator.Calculate(inputs);
            model.HasResult = true;
            model.CalculatedCgpa = result.Gpa;
            model.TotalCredits = result.Credits;
            model.TotalQualityPoints = result.QualityPoints;
        }

        while (model.Rows.Count < 6) model.Rows.Add(new CgpaSemesterRowViewModel());
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GradingScale()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        await _gpaService.EnsureDefaultScaleAsync(user.Id);
        var entries = await _gpaService.GetScaleAsync(user.Id);
        var model = new GradingScaleViewModel
        {
            Entries = entries.Select(item => new GradingScaleRowViewModel
            {
                Id = item.Id,
                LetterGrade = item.LetterGrade,
                MinimumPercentage = item.MinimumPercentage,
                GradePoint = item.GradePoint
            }).ToList()
        };
        if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GradingScale(GradingScaleViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var owned = await _gpaService.GetScaleAsync(user.Id, trackChanges: true);

        model.Entries ??= [];
        if (model.Entries.Count != owned.Count || model.Entries.Select(item => item.Id).Distinct().Count() != owned.Count)
            ModelState.AddModelError(string.Empty, "The grading scale submission is invalid.");

        var submitted = model.Entries
            .GroupBy(item => item.Id)
            .ToDictionary(group => group.Key, group => group.First());
        var ownedIds = owned.Select(item => item.Id).ToHashSet();
        if (!ownedIds.SetEquals(submitted.Keys))
            ModelState.AddModelError(string.Empty, "The grading scale contains an entry that does not belong to your account.");

        foreach (var entry in owned)
        {
            if (!submitted.TryGetValue(entry.Id, out var row)) continue;
            row.LetterGrade = entry.LetterGrade;
            entry.MinimumPercentage = row.MinimumPercentage;
            entry.GradePoint = row.GradePoint;
        }

        var orderedMinimums = owned.OrderBy(item => item.SortOrder).Select(item => submitted.TryGetValue(item.Id, out var row) ? row.MinimumPercentage : item.MinimumPercentage).ToList();
        for (var i = 1; i < orderedMinimums.Count; i++)
        {
            if (orderedMinimums[i] >= orderedMinimums[i - 1])
                ModelState.AddModelError(string.Empty, "Minimum percentages must decrease from the highest grade to the lowest grade.");
        }

        if (!ModelState.IsValid)
        {
            if (!await PopulateShellAsync(model, user)) return MissingStudentProfile();
            return View(model);
        }

        await _gpaService.SaveChangesAsync();
        TempData["GpaSuccess"] = "Grading scale updated. All GPA calculations now use these values.";
        return RedirectToAction(nameof(GradingScale));
    }

    private async Task ValidateGradeFormAsync(CourseGradeFormViewModel model, string userId, int? excludedId = null)
    {
        if (model.CourseId.HasValue)
        {
            if (!await _gpaService.OwnsCourseAsync(userId, model.CourseId.Value))
                ModelState.AddModelError(nameof(model.CourseId), "Please select one of your own courses.");
            else if (await _gpaService.CourseAlreadyAddedAsync(userId, model.SemesterResultId, model.CourseId.Value, excludedId))
                ModelState.AddModelError(nameof(model.CourseId), "This course is already in the semester result.");
        }

        var scale = await _gpaService.GetScaleAsync(userId);
        var valid = scale.Select(item => item.LetterGrade).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!valid.Contains(model.ExpectedLetterGrade?.Trim() ?? string.Empty))
            ModelState.AddModelError(nameof(model.ExpectedLetterGrade), "Select a grade from your grading scale.");
        if (!string.IsNullOrWhiteSpace(model.ActualLetterGrade) && !valid.Contains(model.ActualLetterGrade.Trim()))
            ModelState.AddModelError(nameof(model.ActualLetterGrade), "Select a grade from your grading scale.");
    }

    private void ValidateSemester(SemesterFormViewModel model)
    {
        if (model.AcademicTerm.HasValue && !Enum.IsDefined(typeof(AcademicTerm), model.AcademicTerm.Value))
            ModelState.AddModelError(nameof(model.AcademicTerm), "Select a valid academic term.");
    }

    private async Task PopulateGradeFormAsync(CourseGradeFormViewModel model, string userId, int? includeCourseId = null)
    {
        var courses = await _gpaService.GetAvailableCoursesAsync(userId, model.SemesterResultId, includeCourseId);
        model.CourseOptions = courses.Select(item => new SelectListItem
        {
            Value = item.Id.ToString(),
            Text = $"{item.CourseCode} — {item.CourseName} ({item.CreditHours:0.#} credits)",
            Selected = item.Id == model.CourseId
        }).ToList();
        var scale = await _gpaService.GetScaleAsync(userId);
        model.GradeOptions = scale.Select(item => new SelectListItem
        {
            Value = item.LetterGrade,
            Text = $"{item.LetterGrade} — {item.GradePoint:0.00}",
        }).ToList();
    }

    private async Task PopulateCalculatorOptionsAsync(GpaCalculatorViewModel model, string userId)
    {
        var scale = await _gpaService.GetScaleAsync(userId);
        model.GradeOptions = scale.Select(item => new SelectListItem
        {
            Value = item.LetterGrade,
            Text = $"{item.LetterGrade} ({item.GradePoint:0.00})"
        }).ToList();
    }

    private static Dictionary<string, decimal> BuildScaleLookup(IEnumerable<GradingScaleEntry> scale) =>
        scale.ToDictionary(item => item.LetterGrade, item => item.GradePoint, StringComparer.OrdinalIgnoreCase);

    private static GpaCalculation CalculateAcross(
        IEnumerable<SemesterResult> semesters,
        IReadOnlyDictionary<string, decimal> scale,
        bool useExpectedFallback) =>
        GpaCalculator.Calculate(semesters.SelectMany(item => GradeInputs(item, scale, useExpectedFallback)));

    private static GpaCalculation CalculateSemester(
        SemesterResult semester,
        IReadOnlyDictionary<string, decimal> scale,
        bool useExpectedFallback) =>
        GpaCalculator.Calculate(GradeInputs(semester, scale, useExpectedFallback));

    private static IEnumerable<(decimal Credits, decimal GradePoint)> GradeInputs(
        SemesterResult semester,
        IReadOnlyDictionary<string, decimal> scale,
        bool useExpectedFallback)
    {
        foreach (var grade in semester.CourseGrades)
        {
            var letter = useExpectedFallback
                ? grade.ActualLetterGrade ?? grade.ExpectedLetterGrade
                : grade.ActualLetterGrade;
            if (letter is not null && scale.TryGetValue(letter, out var point))
                yield return (grade.Course.CreditHours, point);
        }
    }

    private static SemesterSummaryViewModel ToSummary(
        SemesterResult semester,
        IReadOnlyDictionary<string, decimal> scale)
    {
        var actual = CalculateSemester(semester, scale, false);
        var projected = CalculateSemester(semester, scale, true);
        return new SemesterSummaryViewModel
        {
            Id = semester.Id,
            SemesterNumber = semester.SemesterNumber,
            AcademicTerm = semester.AcademicTerm,
            AcademicYear = semester.AcademicYear,
            CourseCount = semester.CourseGrades.Count,
            ActualCredits = actual.Credits,
            SemesterGpa = actual.Gpa,
            ProjectedGpa = projected.Gpa,
            HasActualResults = actual.Credits > 0
        };
    }

    private static CourseGradeRowViewModel ToGradeRow(
        CourseGrade grade,
        IReadOnlyDictionary<string, decimal> scale) => new()
    {
        Id = grade.Id,
        CourseId = grade.CourseId,
        CourseCode = grade.Course.CourseCode,
        CourseName = grade.Course.CourseName,
        CreditHours = grade.Course.CreditHours,
        ExpectedLetterGrade = grade.ExpectedLetterGrade,
        ActualLetterGrade = grade.ActualLetterGrade,
        ExpectedGradePoint = scale.GetValueOrDefault(grade.ExpectedLetterGrade),
        ActualGradePoint = grade.ActualLetterGrade is not null && scale.TryGetValue(grade.ActualLetterGrade, out var point) ? point : null,
        Notes = grade.Notes
    };

    private static void CalculateGoal(GpaDashboardViewModel model, decimal maximum)
    {
        if (!model.GoalCurrentCgpa.HasValue || !model.GoalCompletedCredits.HasValue ||
            !model.TargetCgpa.HasValue || !model.FutureCredits.HasValue)
        {
            model.GoalMessage = "Enter all four values to calculate your target path.";
            return;
        }
        if (model.GoalCurrentCgpa.Value < 0 || model.GoalCurrentCgpa.Value > maximum ||
            model.TargetCgpa.Value < 0 || model.TargetCgpa.Value > maximum ||
            model.GoalCompletedCredits.Value < 0)
        {
            model.GoalMessage = $"CGPA values must be between 0 and {maximum:0.00}, and completed credits cannot be negative.";
            return;
        }

        var result = GpaCalculator.CalculateRequiredGpa(
            model.GoalCurrentCgpa.Value,
            model.GoalCompletedCredits.Value,
            model.TargetCgpa.Value,
            model.FutureCredits.Value,
            maximum);
        model.GoalIsPossible = result.IsPossible;
        model.RequiredGpa = result.RequiredGpa;
        model.GoalMessage = result.Message;
    }

    private async Task<bool> PopulateShellAsync(StudentShellViewModel model, ApplicationUser user, bool useProfileSemester = false)
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
        if (useProfileSemester && model is SemesterFormViewModel form && !form.SemesterNumber.HasValue)
            form.SemesterNumber = profile.Semester;
        return true;
    }

    private static string SemesterLabel(SemesterResult item) =>
        $"Semester {item.SemesterNumber} · {item.AcademicTerm} {item.AcademicYear}";

    private static AcademicTerm CurrentTerm()
    {
        var month = DateTime.UtcNow.Month;
        return month <= 4 ? AcademicTerm.Spring : month <= 8 ? AcademicTerm.Summer : AcademicTerm.Fall;
    }

    private static string? NormalizeGrade(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
