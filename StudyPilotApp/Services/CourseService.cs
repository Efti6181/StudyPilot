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

    public async Task SaveChangesAsync()
    {
        var addedCourses = _dbContext.ChangeTracker.Entries<Course>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        await _dbContext.SaveChangesAsync();
        foreach (var course in addedCourses)
        {
            if (!course.FacultyCourseAssignmentId.HasValue) continue;
            var assignmentId = course.FacultyCourseAssignmentId.Value;
            var published = await _dbContext.FacultyAssessments.AsNoTracking()
                .Where(item => item.Status == FacultyAssessmentStatus.Published &&
                    item.FacultyCourseAssignmentId == assignmentId)
                .ToListAsync();

            foreach (var source in published)
            {
                var copy = new Assessment
                {
                    ApplicationUserId = course.ApplicationUserId,
                    CourseId = course.Id,
                    FacultyAssessmentId = source.Id,
                    Title = source.Title,
                    Type = source.Type,
                    Description = source.Description,
                    Instructions = source.Instructions,
                    AssignedDate = source.AssignedDate,
                    DueDate = source.DueDate,
                    TotalMarks = source.TotalMarks,
                    WeightPercentage = source.WeightPercentage,
                    Difficulty = source.Difficulty,
                    Status = AssessmentStatus.NotStarted
                };
                _dbContext.Assessments.Add(copy);
                await _dbContext.SaveChangesAsync();
                _dbContext.AppNotifications.Add(new AppNotification
                {
                    ApplicationUserId = course.ApplicationUserId,
                    Title = "New assessment published",
                    Message = $"{course.CourseCode}: {source.Title} is due {source.DueDate:dd MMM yyyy, h:mm tt}.",
                    Type = NotificationType.Assessment,
                    RelatedUrl = $"/Assessments/Details/{copy.Id}"
                });
            }

            var publishedResources = await _dbContext.FacultyResources.AsNoTracking()
                .Where(item => item.Status == FacultyResourceStatus.Published &&
                    item.FacultyCourseAssignmentId == assignmentId)
                .ToListAsync();
            foreach (var source in publishedResources)
            {
                var copy = new StudyResource { ApplicationUserId = course.ApplicationUserId, CourseId = course.Id, FacultyResourceId = source.Id, Title = source.Title, Description = source.Description, Kind = source.Kind, Category = source.Category, Tags = source.Tags, ExternalUrl = source.ExternalUrl, OriginalFileName = source.OriginalFileName, StoredFileName = source.StoredFileName, ContentType = source.ContentType, FileSizeBytes = source.FileSizeBytes };
                _dbContext.StudyResources.Add(copy);
                await _dbContext.SaveChangesAsync();
                _dbContext.AppNotifications.Add(new AppNotification { ApplicationUserId = course.ApplicationUserId, Title = "New learning resource", Message = $"{course.CourseCode}: {source.Title} is now available.", Type = NotificationType.Academic, RelatedUrl = $"/Resources/Details/{copy.Id}" });
            }

            var publishedAnnouncements = await _dbContext.FacultyAnnouncements.AsNoTracking()
                .Where(item => item.Status == FacultyAnnouncementStatus.Published &&
                    (!item.ExpiresAt.HasValue || item.ExpiresAt >= DateTimeOffset.UtcNow) &&
                    item.FacultyCourseAssignmentId == assignmentId)
                .ToListAsync();
            foreach (var source in publishedAnnouncements)
            {
                var recipient = new FacultyAnnouncementRecipient { FacultyAnnouncementId = source.Id, CourseId = course.Id, ApplicationUserId = course.ApplicationUserId };
                _dbContext.FacultyAnnouncementRecipients.Add(recipient);
                await _dbContext.SaveChangesAsync();
                var title = source.Priority == AnnouncementPriority.Urgent ? $"Urgent: {source.Title}" : source.Title;
                _dbContext.AppNotifications.Add(new AppNotification { ApplicationUserId = course.ApplicationUserId, Title = title[..Math.Min(title.Length, 160)], Message = $"{course.CourseCode}: {source.Summary}", Type = NotificationType.Academic, RelatedUrl = $"/CourseAnnouncements/Details/{recipient.Id}" });
            }
        }
        if (addedCourses.Count > 0) await _dbContext.SaveChangesAsync();
    }

    public void Remove(Course course)
    {
        _dbContext.Courses.Remove(course);
    }
}
