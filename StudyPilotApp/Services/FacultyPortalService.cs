using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class FacultyPortalService : IFacultyPortalService
{
    private readonly ApplicationDbContext _dbContext;

    public FacultyPortalService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FacultyDashboardData?> GetDashboardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var profileId = await _dbContext.FacultyProfiles.AsNoTracking()
            .Where(item => item.ApplicationUserId == userId)
            .Select(item => (int?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!profileId.HasValue) return null;

        var assignments = BaseCourseQuery(userId);
        var assignedCourses = await assignments.CountAsync(cancellationToken);
        var activeCourses = await assignments.CountAsync(item => item.IsActive, cancellationToken);
        var credits = await assignments.Where(item => item.IsActive)
            .SumAsync(item => (decimal?)item.CatalogCourse.CreditHours, cancellationToken) ?? 0m;
        var courses = await assignments.Where(item => item.IsActive)
            .OrderByDescending(item => item.AcademicPeriod.IsCurrent)
            .ThenBy(item => item.CatalogCourse.Code)
            .Take(5).Select(CourseProjection()).ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var localNow = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        var upcomingEvents = await _dbContext.CampusEvents.AsNoTracking()
            .Where(item => item.IsPublished && item.EndAt >= now)
            .OrderBy(item => item.StartAt).Take(4)
            .Select(item => new FacultyDashboardEventData(
                item.Id, item.Title, item.Type, item.StartAt,
                item.LocationType == EventLocationType.Online ? "Online" : item.Venue ?? item.LocationType.ToString()))
            .ToListAsync(cancellationToken);

        var announcements = await _dbContext.Announcements.AsNoTracking()
            .Where(item => item.IsPublished &&
                (item.Audience == AnnouncementAudience.Everyone || item.Audience == AnnouncementAudience.Faculty) &&
                (!item.ExpiresAt.HasValue || item.ExpiresAt >= now))
            .OrderByDescending(item => item.PublishedAt).Take(4)
            .Select(item => new FacultyDashboardAnnouncementData(
                item.Id, item.Title, item.Priority, item.PublishedAt ?? item.CreatedAt))
            .ToListAsync(cancellationToken);

        var community = await _dbContext.CommunityPosts.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt).Take(4)
            .Select(item => new FacultyDashboardCommunityData(
                item.Id, item.Title, item.Category,
                item.ApplicationUser.FullName, item.CreatedAt))
            .ToListAsync(cancellationToken);

        var recentCutoff = now.AddDays(-30);
        var recentCommunityCount = await _dbContext.CommunityPosts.AsNoTracking()
            .CountAsync(item => item.CreatedAt >= recentCutoff, cancellationToken);
        var activeAnnouncementCount = await _dbContext.Announcements.AsNoTracking()
            .CountAsync(item => item.IsPublished &&
                (item.Audience == AnnouncementAudience.Everyone || item.Audience == AnnouncementAudience.Faculty) &&
                (!item.ExpiresAt.HasValue || item.ExpiresAt >= now), cancellationToken);

        var upcomingAssessmentCount = await _dbContext.FacultyAssessments.AsNoTracking()
            .CountAsync(item => item.FacultyCourseAssignment.FacultyProfile.ApplicationUserId == userId &&
                item.Status == FacultyAssessmentStatus.Published && item.DueDate >= localNow, cancellationToken);
        var recentlyPublishedCount = await _dbContext.FacultyAssessments.AsNoTracking()
            .CountAsync(item => item.FacultyCourseAssignment.FacultyProfile.ApplicationUserId == userId &&
                item.PublishedAt >= recentCutoff, cancellationToken);
        var publishedResourceCount = await _dbContext.FacultyResources.AsNoTracking()
            .CountAsync(item => item.FacultyCourseAssignment.FacultyProfile.ApplicationUserId == userId &&
                item.Status == FacultyResourceStatus.Published, cancellationToken);
        var publishedCourseAnnouncementCount = await _dbContext.FacultyAnnouncements.AsNoTracking()
            .CountAsync(item => item.FacultyCourseAssignment.FacultyProfile.ApplicationUserId == userId &&
                item.Status == FacultyAnnouncementStatus.Published &&
                (!item.ExpiresAt.HasValue || item.ExpiresAt >= now), cancellationToken);

        return new FacultyDashboardData(
            assignedCourses, activeCourses, credits, upcomingEvents.Count,
            activeAnnouncementCount, recentCommunityCount, upcomingAssessmentCount, recentlyPublishedCount,
            publishedResourceCount, publishedCourseAnnouncementCount, courses,
            upcomingEvents, announcements, community);
    }

    public async Task<FacultyCourseListData> SearchCoursesAsync(
        string userId, string? search, int? academicPeriodId, bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var all = BaseCourseQuery(userId);
        var totalCount = await all.CountAsync(cancellationToken);
        var activeCount = await all.CountAsync(item => item.IsActive, cancellationToken);
        var query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.CatalogCourse.Code, pattern) ||
                                        EF.Functions.ILike(item.CatalogCourse.Name, pattern) ||
                                        EF.Functions.ILike(item.Section, pattern));
        }
        if (academicPeriodId.HasValue) query = query.Where(item => item.AcademicPeriodId == academicPeriodId.Value);
        if (activeOnly) query = query.Where(item => item.IsActive);
        var items = await query.OrderByDescending(item => item.AcademicPeriod.IsCurrent)
            .ThenByDescending(item => item.AcademicPeriod.AcademicYear)
            .ThenBy(item => item.CatalogCourse.Code)
            .Select(CourseProjection()).ToListAsync(cancellationToken);
        return new FacultyCourseListData(items, totalCount, activeCount);
    }

    public Task<FacultyCourseData?> GetCourseAsync(
        string userId, int assignmentId, bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = BaseCourseQuery(userId, trackChanges);
        return query.Where(item => item.Id == assignmentId)
            .Select(CourseProjection()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateCourseOverviewAsync(
        string userId, int assignmentId, string? overview,
        CancellationToken cancellationToken = default)
    {
        var item = await BaseCourseQuery(userId, true)
            .SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (item is null) return false;
        item.CourseOverview = string.IsNullOrWhiteSpace(overview) ? null : overview.Trim();
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<FacultyPeriodOptionData>> GetPeriodsAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var periods = await BaseCourseQuery(userId)
            .Select(item => new
            {
                item.AcademicPeriod.Id,
                item.AcademicPeriod.Term,
                item.AcademicPeriod.AcademicYear,
                item.AcademicPeriod.IsCurrent
            })
            .Distinct()
            .OrderByDescending(item => item.IsCurrent)
            .ThenByDescending(item => item.AcademicYear)
            .ToListAsync(cancellationToken);

        return periods.Select(item => new FacultyPeriodOptionData(
            item.Id, $"{item.Term} {item.AcademicYear}", item.IsCurrent)).ToList();
    }

    private IQueryable<FacultyCourseAssignment> BaseCourseQuery(string userId, bool trackChanges = false)
    {
        var query = _dbContext.FacultyCourseAssignments
            .Where(item => item.FacultyProfile.ApplicationUserId == userId);
        return trackChanges ? query : query.AsNoTracking();
    }

    private static System.Linq.Expressions.Expression<Func<FacultyCourseAssignment, FacultyCourseData>> CourseProjection() => item => new(
        item.Id, item.CatalogCourseId, item.CatalogCourse.Code, item.CatalogCourse.Name,
        item.CatalogCourse.Department.Code, item.CatalogCourse.Department.Name,
        item.CatalogCourse.Program == null ? null : item.CatalogCourse.Program.Name,
        item.CatalogCourse.CreditHours, item.CatalogCourse.CourseType,
        item.CatalogCourse.Difficulty, item.CatalogCourse.CareerRelevance,
        item.CatalogCourse.Description, item.CatalogCourse.Prerequisites,
        item.AcademicPeriodId, item.AcademicPeriod.Term, item.AcademicPeriod.AcademicYear,
        item.Section, item.CourseOverview, item.IsActive, item.AssignedAt);
}
