using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class GpaService : IGpaService
{
    private static readonly (string Grade, decimal Minimum, decimal Point)[] DefaultScale =
    [
        ("A+", 80m, 4.00m),
        ("A", 75m, 3.75m),
        ("A-", 70m, 3.50m),
        ("B+", 65m, 3.25m),
        ("B", 60m, 3.00m),
        ("B-", 55m, 2.75m),
        ("C+", 50m, 2.50m),
        ("C", 45m, 2.25m),
        ("D", 40m, 2.00m),
        ("F", 0m, 0.00m)
    ];

    private readonly ApplicationDbContext _dbContext;

    public GpaService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureDefaultScaleAsync(string userId)
    {
        if (await _dbContext.GradingScaleEntries.AnyAsync(item => item.ApplicationUserId == userId))
        {
            return;
        }

        var entries = DefaultScale.Select((item, index) => new GradingScaleEntry
        {
            ApplicationUserId = userId,
            LetterGrade = item.Grade,
            MinimumPercentage = item.Minimum,
            GradePoint = item.Point,
            SortOrder = index + 1
        });

        await _dbContext.GradingScaleEntries.AddRangeAsync(entries);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<GradingScaleEntry>> GetScaleAsync(string userId, bool trackChanges = false)
    {
        var query = _dbContext.GradingScaleEntries
            .Where(item => item.ApplicationUserId == userId)
            .OrderBy(item => item.SortOrder);

        return trackChanges
            ? await query.ToListAsync()
            : await query.AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<SemesterResult>> GetSemestersAsync(string userId) =>
        await _dbContext.SemesterResults
            .AsNoTracking()
            .Include(item => item.CourseGrades)
                .ThenInclude(item => item.Course)
            .Where(item => item.ApplicationUserId == userId)
            .OrderByDescending(item => item.AcademicYear)
            .ThenByDescending(item => item.AcademicTerm)
            .ThenByDescending(item => item.SemesterNumber)
            .ToListAsync();

    public Task<SemesterResult?> GetSemesterAsync(string userId, int semesterId, bool trackChanges = false)
    {
        var query = _dbContext.SemesterResults
            .Include(item => item.CourseGrades)
                .ThenInclude(item => item.Course)
            .Where(item => item.Id == semesterId && item.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public Task<CourseGrade?> GetCourseGradeAsync(string userId, int gradeId, bool trackChanges = false)
    {
        var query = _dbContext.CourseGrades
            .Include(item => item.Course)
            .Include(item => item.SemesterResult)
            .Where(item => item.Id == gradeId && item.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Course>> GetAvailableCoursesAsync(
        string userId,
        int semesterId,
        int? includeCourseId = null) =>
        await _dbContext.Courses
            .AsNoTracking()
            .Where(course =>
                course.ApplicationUserId == userId &&
                (course.Id == includeCourseId ||
                 !_dbContext.CourseGrades.Any(grade =>
                     grade.SemesterResultId == semesterId &&
                     grade.CourseId == course.Id)))
            .OrderBy(course => course.CourseCode)
            .ToListAsync();

    public Task<bool> OwnsCourseAsync(string userId, int courseId) =>
        _dbContext.Courses.AnyAsync(course =>
            course.Id == courseId && course.ApplicationUserId == userId);

    public Task<bool> SemesterDuplicateExistsAsync(
        string userId,
        int semesterNumber,
        AcademicTerm term,
        int year,
        int? excludedId = null) =>
        _dbContext.SemesterResults.AnyAsync(item =>
            item.ApplicationUserId == userId &&
            item.SemesterNumber == semesterNumber &&
            item.AcademicTerm == term &&
            item.AcademicYear == year &&
            (!excludedId.HasValue || item.Id != excludedId.Value));

    public Task<bool> CourseAlreadyAddedAsync(
        string userId,
        int semesterId,
        int courseId,
        int? excludedGradeId = null) =>
        _dbContext.CourseGrades.AnyAsync(item =>
            item.ApplicationUserId == userId &&
            item.SemesterResultId == semesterId &&
            item.CourseId == courseId &&
            (!excludedGradeId.HasValue || item.Id != excludedGradeId.Value));

    public Task<int> GetCourseGradeCountAsync(string userId, int courseId) =>
        _dbContext.CourseGrades.CountAsync(item =>
            item.ApplicationUserId == userId && item.CourseId == courseId);

    public async Task AddSemesterAsync(SemesterResult semester) =>
        await _dbContext.SemesterResults.AddAsync(semester);

    public async Task AddCourseGradeAsync(CourseGrade grade) =>
        await _dbContext.CourseGrades.AddAsync(grade);

    public void RemoveSemester(SemesterResult semester) =>
        _dbContext.SemesterResults.Remove(semester);

    public void RemoveCourseGrade(CourseGrade grade) =>
        _dbContext.CourseGrades.Remove(grade);

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
