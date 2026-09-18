using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AssessmentService : IAssessmentService
{
    private readonly ApplicationDbContext _dbContext;

    public AssessmentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Assessment>> GetOwnedAssessmentsAsync(
        string userId,
        string? search,
        int? courseId,
        AssessmentStatus? status,
        AssessmentType? type,
        bool overdueOnly,
        string sort)
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var query = _dbContext.Assessments
            .AsNoTracking()
            .Include(item => item.Course)
            .Where(item => item.ApplicationUserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.Course.CourseCode, pattern) ||
                EF.Functions.ILike(item.Course.CourseName, pattern));
        }

        if (courseId.HasValue)
        {
            query = query.Where(item => item.CourseId == courseId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(item => item.Type == type.Value);
        }

        if (overdueOnly)
        {
            query = query.Where(item =>
                item.Status != AssessmentStatus.Completed &&
                item.DueDate < now);
        }

        query = sort switch
        {
            "latest" => query.OrderByDescending(item => item.DueDate),
            "course" => query.OrderBy(item => item.Course.CourseCode).ThenBy(item => item.DueDate),
            "status" => query.OrderBy(item => item.Status).ThenBy(item => item.DueDate),
            "created" => query.OrderByDescending(item => item.CreatedAt),
            _ => query.OrderBy(item => item.DueDate)
        };

        return await query.ToListAsync();
    }

    public async Task<AssessmentSummary> GetSummaryAsync(string userId)
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var weekEnd = now.AddDays(7);
        var query = _dbContext.Assessments
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == userId);

        return new AssessmentSummary(
            await query.CountAsync(),
            await query.CountAsync(item => item.Status == AssessmentStatus.Completed),
            await query.CountAsync(item => item.Status != AssessmentStatus.Completed && item.DueDate >= now),
            await query.CountAsync(item => item.Status != AssessmentStatus.Completed && item.DueDate < now),
            await query.CountAsync(item =>
                item.Status != AssessmentStatus.Completed &&
                item.DueDate >= now &&
                item.DueDate <= weekEnd));
    }

    public Task<Assessment?> GetOwnedAssessmentAsync(
        string userId,
        int assessmentId,
        bool trackChanges = false)
    {
        var query = _dbContext.Assessments
            .Include(item => item.Course)
            .Where(item => item.Id == assessmentId && item.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public Task<bool> OwnsCourseAsync(string userId, int courseId) =>
        _dbContext.Courses.AnyAsync(course =>
            course.Id == courseId &&
            course.ApplicationUserId == userId);

    public async Task<IReadOnlyList<Course>> GetCourseOptionsAsync(string userId) =>
        await _dbContext.Courses
            .AsNoTracking()
            .Where(course => course.ApplicationUserId == userId)
            .OrderBy(course => course.CourseCode)
            .ToListAsync();

    public async Task AddAsync(Assessment assessment) =>
        await _dbContext.Assessments.AddAsync(assessment);

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    public void Remove(Assessment assessment) => _dbContext.Assessments.Remove(assessment);
}
