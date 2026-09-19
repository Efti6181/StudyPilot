using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class PriorityService : IPriorityService
{
    private readonly ApplicationDbContext _dbContext;

    public PriorityService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnsureSettingsAsync(string userId)
    {
        if (await _dbContext.PriorityWeightSettings.AnyAsync(item => item.ApplicationUserId == userId))
        {
            return;
        }

        await _dbContext.PriorityWeightSettings.AddAsync(new PriorityWeightSettings
        {
            ApplicationUserId = userId
        });
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PriorityWeightSettings> GetSettingsAsync(string userId, bool trackChanges = false)
    {
        var query = _dbContext.PriorityWeightSettings
            .Where(item => item.ApplicationUserId == userId);
        var settings = trackChanges
            ? await query.SingleAsync()
            : await query.AsNoTracking().SingleAsync();
        return settings;
    }

    public async Task<IReadOnlyList<Course>> GetOwnedCoursesAsync(string userId) =>
        await _dbContext.Courses
            .AsNoTracking()
            .Include(course => course.Assessments)
            .Include(course => course.CourseGrades)
            .Include(course => course.PriorityPreference)
            .Where(course =>
                course.ApplicationUserId == userId &&
                course.Status == CourseStatus.Active)
            .OrderBy(course => course.CourseCode)
            .ToListAsync();

    public Task<Course?> GetOwnedCourseAsync(string userId, int courseId) =>
        _dbContext.Courses
            .AsNoTracking()
            .Include(course => course.Assessments)
            .Include(course => course.CourseGrades)
            .Include(course => course.PriorityPreference)
            .SingleOrDefaultAsync(course =>
                course.Id == courseId &&
                course.ApplicationUserId == userId);

    public Task<CoursePriorityPreference?> GetPreferenceAsync(
        string userId,
        int courseId,
        bool trackChanges = false)
    {
        var query = _dbContext.CoursePriorityPreferences
            .Where(item =>
                item.CourseId == courseId &&
                item.ApplicationUserId == userId);

        return trackChanges
            ? query.SingleOrDefaultAsync()
            : query.AsNoTracking().SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<GradingScaleEntry>> GetGradingScaleAsync(string userId) =>
        await _dbContext.GradingScaleEntries
            .AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync();

    public async Task AddPreferenceAsync(CoursePriorityPreference preference) =>
        await _dbContext.CoursePriorityPreferences.AddAsync(preference);

    public void RemovePreference(CoursePriorityPreference preference) =>
        _dbContext.CoursePriorityPreferences.Remove(preference);

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
