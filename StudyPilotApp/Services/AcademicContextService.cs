using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AcademicContextService : IAcademicContextService
{
    private readonly ApplicationDbContext _dbContext;

    public AcademicContextService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AcademicContextSnapshot> BuildAsync(
        string userId,
        int? courseId,
        int? assessmentId,
        CancellationToken cancellationToken = default)
    {
        var courses = await _dbContext.Courses.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId && item.Status == CourseStatus.Active)
            .Include(item => item.PriorityPreference)
            .OrderBy(item => item.CourseCode)
            .Take(12)
            .Select(item => new AcademicCourseContext(
                item.Id,
                item.CourseCode,
                item.CourseName,
                item.CreditHours,
                item.ProgressPercentage,
                item.TargetGrade,
                item.PriorityPreference == null ? null : item.PriorityPreference.ConfidenceRating,
                item.PriorityPreference == null ? null : item.PriorityPreference.TopicCompletionPercentage,
                item.PriorityPreference == null ? null : item.PriorityPreference.WorkloadRisk,
                item.PriorityPreference == null ? null : item.PriorityPreference.AvailableStudyHoursPerWeek))
            .ToListAsync(cancellationToken);

        var pendingAssessments = await _dbContext.Assessments.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId && item.Status != AssessmentStatus.Completed)
            .OrderBy(item => item.DueDate)
            .Take(20)
            .Select(item => new AcademicAssessmentContext(
                item.Id,
                item.CourseId,
                item.Course.CourseCode,
                item.Title,
                item.Type.ToString(),
                item.DueDate,
                item.Status.ToString(),
                item.Difficulty.ToString(),
                item.EstimatedStudyHours,
                item.Description == null ? null : item.Description.Substring(0, Math.Min(item.Description.Length, 400))))
            .ToListAsync(cancellationToken);

        AcademicCourseContext? selectedCourse = null;
        if (courseId.HasValue)
        {
            selectedCourse = courses.SingleOrDefault(item => item.Id == courseId.Value);
            if (selectedCourse is null)
            {
                selectedCourse = await _dbContext.Courses.AsNoTracking()
                    .Where(item => item.Id == courseId.Value && item.ApplicationUserId == userId)
                    .Select(item => new AcademicCourseContext(
                        item.Id, item.CourseCode, item.CourseName, item.CreditHours,
                        item.ProgressPercentage, item.TargetGrade, null, null, null, null))
                    .SingleOrDefaultAsync(cancellationToken);
            }
        }

        AcademicAssessmentContext? selectedAssessment = null;
        if (assessmentId.HasValue)
        {
            selectedAssessment = pendingAssessments.SingleOrDefault(item => item.Id == assessmentId.Value);
            if (selectedAssessment is null)
            {
                selectedAssessment = await _dbContext.Assessments.AsNoTracking()
                    .Where(item => item.Id == assessmentId.Value && item.ApplicationUserId == userId)
                    .Select(item => new AcademicAssessmentContext(
                        item.Id, item.CourseId, item.Course.CourseCode, item.Title,
                        item.Type.ToString(), item.DueDate, item.Status.ToString(),
                        item.Difficulty.ToString(), item.EstimatedStudyHours,
                        item.Description == null ? null : item.Description.Substring(0, Math.Min(item.Description.Length, 400))))
                    .SingleOrDefaultAsync(cancellationToken);
            }
        }

        var latestProgress = await _dbContext.AcademicProgressSnapshots.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .OrderByDescending(item => item.SnapshotDate)
            .Select(item => new
            {
                item.CurrentCgpa,
                item.AverageAssessmentScore,
                item.CompletedAssessments,
                item.PendingAssessments,
                item.OverdueAssessments
            })
            .FirstOrDefaultAsync(cancellationToken);

        var resourceTitles = await _dbContext.StudyResources.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId &&
                           (!courseId.HasValue || item.CourseId == courseId.Value))
            .OrderByDescending(item => item.IsFavorite)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => item.Title)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new AcademicContextSnapshot(
            courses,
            pendingAssessments,
            selectedCourse,
            selectedAssessment,
            latestProgress?.CurrentCgpa,
            latestProgress?.AverageAssessmentScore,
            latestProgress?.CompletedAssessments ?? 0,
            latestProgress?.PendingAssessments ?? pendingAssessments.Count,
            latestProgress?.OverdueAssessments ?? pendingAssessments.Count(item => item.DueDate < DateTime.Now),
            resourceTitles);
    }
}
