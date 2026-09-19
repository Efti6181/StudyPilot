using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class CourseService : ICourseService
{
    private readonly ApplicationDbContext _dbContext;

    public CourseService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Course>> GetOwnedCoursesAsync(
        string userId,
        string? search,
        CourseStatus? status,
        CourseType? type,
        string sort)
    {
        var query = _dbContext.Courses
            .AsNoTracking()
            .Where(course => course.ApplicationUserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(course =>
                EF.Functions.ILike(course.CourseCode, pattern) ||
                EF.Functions.ILike(course.CourseName, pattern) ||
                (course.Instructor != null && EF.Functions.ILike(course.Instructor, pattern)));
        }

        if (status.HasValue)
        {
            query = query.Where(course => course.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(course => course.CourseType == type.Value);
        }

        query = sort switch
        {
            "name" => query.OrderBy(course => course.CourseName),
            "credits" => query.OrderByDescending(course => course.CreditHours)
                .ThenBy(course => course.CourseCode),
            "progress" => query.OrderByDescending(course => course.ProgressPercentage)
                .ThenBy(course => course.CourseCode),
            "newest" => query.OrderByDescending(course => course.CreatedAt),
            _ => query.OrderBy(course => course.CourseCode)
        };

        return await query.ToListAsync();
    }

    public async Task<CourseSummary> GetSummaryAsync(string userId)
    {
        var activeQuery = _dbContext.Courses
            .AsNoTracking()
            .Where(course =>
                course.ApplicationUserId == userId &&
                course.Status == CourseStatus.Active);

        var summary = await activeQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                ActiveCourses = group.Count(),
                ActiveCredits = group.Sum(course => course.CreditHours),
                AverageProgress = group.Average(course => course.ProgressPercentage),
                NeedsAttention = group.Count(course => course.ProgressPercentage < 60)
            })
            .SingleOrDefaultAsync();

        return summary is null
            ? new CourseSummary(0, 0m, 0d, 0)
            : new CourseSummary(
                summary.ActiveCourses,
                summary.ActiveCredits,
                summary.AverageProgress,
                summary.NeedsAttention);
    }

    public Task<Course?> GetOwnedCourseAsync(
        string userId,
        int courseId,
        bool trackChanges = false)
    {
        var query = _dbContext.Courses
            .Where(course =>
                course.Id == courseId &&
                course.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public Task<int> GetAssessmentCountAsync(string userId, int courseId) =>
        _dbContext.Assessments.CountAsync(assessment =>
            assessment.ApplicationUserId == userId &&
            assessment.CourseId == courseId);

    public Task<int> GetCourseGradeCountAsync(string userId, int courseId) =>
        _dbContext.CourseGrades.CountAsync(grade =>
            grade.ApplicationUserId == userId &&
            grade.CourseId == courseId);

    public Task<bool> DuplicateExistsAsync(
        string userId,
        string courseCode,
        int semester,
        AcademicTerm academicTerm,
        int academicYear,
        int? excludedCourseId = null)
    {
        var normalizedCode = courseCode.Trim().ToUpperInvariant();

        return _dbContext.Courses.AnyAsync(course =>
            course.ApplicationUserId == userId &&
            course.CourseCode == normalizedCode &&
            course.Semester == semester &&
            course.AcademicTerm == academicTerm &&
            course.AcademicYear == academicYear &&
            (!excludedCourseId.HasValue || course.Id != excludedCourseId.Value));
    }

    public async Task AddAsync(Course course)
    {
        await _dbContext.Courses.AddAsync(course);
    }

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    public void Remove(Course course)
    {
        _dbContext.Courses.Remove(course);
    }
}
