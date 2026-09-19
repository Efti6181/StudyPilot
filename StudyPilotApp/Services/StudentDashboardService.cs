using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class StudentDashboardService : IStudentDashboardService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IProgressService _progressService;

    public StudentDashboardService(
        ApplicationDbContext dbContext,
        IProgressService progressService)
    {
        _dbContext = dbContext;
        _progressService = progressService;
    }

    public async Task<StudentDashboardData> GetAsync(string userId)
    {
        var analytics = await _progressService.GetAnalyticsAsync(userId, null);
        var upcomingAssessments = await _dbContext.Assessments
            .AsNoTracking()
            .Include(item => item.Course)
            .Where(item =>
                item.ApplicationUserId == userId &&
                item.Status != AssessmentStatus.Completed)
            .OrderBy(item => item.DueDate)
            .Take(5)
            .ToListAsync();

        return new StudentDashboardData(
            analytics.Overview,
            analytics.GpaTrend,
            analytics.Courses,
            upcomingAssessments);
    }

    public Task<AcademicProgressSnapshot> CaptureTodayAsync(string userId) =>
        _progressService.CaptureTodayAsync(userId);
}
