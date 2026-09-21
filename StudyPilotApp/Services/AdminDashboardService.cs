using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public AdminDashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardData> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var startMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero)
            .AddMonths(-5);

        var roleCounts = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == "Student" || role.Name == "Faculty"
            group role by role.Name into grouped
            select new { Role = grouped.Key!, Count = grouped.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        var activeMemberCounts = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join user in _dbContext.Users.AsNoTracking() on userRole.UserId equals user.Id
            where user.IsActive && (role.Name == "Student" || role.Name == "Faculty")
            group user by role.Name into grouped
            select new { Role = grouped.Key!, Count = grouped.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        var pendingRegistrations = await _dbContext.UniversityMembers.AsNoTracking()
            .CountAsync(item => item.IsActive && !item.IsClaimed &&
                                (item.Role == "Student" || item.Role == "Faculty"),
                cancellationToken);
        var studentCourseRecords = await _dbContext.Courses.AsNoTracking()
            .CountAsync(cancellationToken);
        var upcomingEvents = await _dbContext.CampusEvents.AsNoTracking()
            .CountAsync(item => item.IsPublished && item.EndAt >= now, cancellationToken);
        var unpublishedEvents = await _dbContext.CampusEvents.AsNoTracking()
            .CountAsync(item => !item.IsPublished && item.EndAt >= now, cancellationToken);
        var communityPosts = await _dbContext.CommunityPosts.AsNoTracking()
            .CountAsync(cancellationToken);
        var resources = await _dbContext.StudyResources.AsNoTracking()
            .CountAsync(cancellationToken);

        var recentRegistrations = await (
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join memberRecord in _dbContext.UniversityMembers.AsNoTracking()
                on user.Id equals memberRecord.ApplicationUserId into memberRecords
            from member in memberRecords.DefaultIfEmpty()
            where role.Name == "Student" || role.Name == "Faculty"
            orderby user.CreatedAt descending
            select new AdminRegistrationData(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                role.Name!,
                user.IsActive,
                user.CreatedAt))
            .Take(6)
            .ToListAsync(cancellationToken);

        var upcomingEventRows = await _dbContext.CampusEvents
            .AsNoTracking()
            .Where(item => item.IsPublished && item.EndAt >= now)
            .OrderBy(item => item.StartAt)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.StartAt,
                item.Type,
                item.LocationType,
                item.Venue,
                RegistrationCount = item.Registrations.Count(registration => registration.CancelledAt == null),
                item.Capacity
            })
            .Take(5)
            .ToListAsync(cancellationToken);
        var upcomingEventItems = upcomingEventRows.Select(item => new AdminEventData(
            item.Id,
            item.Title,
            item.Type.ToString(),
            item.StartAt,
            item.LocationType == EventLocationType.Online
                ? "Online"
                : item.Venue ?? item.LocationType.ToString(),
            item.RegistrationCount,
            item.Capacity)).ToList();

        var recentUserActivity = await _dbContext.Users.AsNoTracking()
            .Where(item => item.CreatedAt >= startMonth)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new AdminActivityData(
                "Registration",
                item.FullName,
                "New StudyPilot account registered",
                item.CreatedAt,
                "bi-person-plus"))
            .Take(5)
            .ToListAsync(cancellationToken);
        var recentPostActivity = await _dbContext.CommunityPosts.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new AdminActivityData(
                "Community",
                item.Title,
                "Community post published",
                item.CreatedAt,
                "bi-chat-square-text"))
            .Take(5)
            .ToListAsync(cancellationToken);
        var recentResourceActivity = await _dbContext.StudyResources.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new AdminActivityData(
                "Resource",
                item.Title,
                "Study resource added",
                item.CreatedAt,
                "bi-collection"))
            .Take(5)
            .ToListAsync(cancellationToken);
        var recentEventActivity = await _dbContext.CampusEvents.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new AdminActivityData(
                "Event",
                item.Title,
                item.IsPublished ? "Campus event published" : "Event draft created",
                item.CreatedAt,
                "bi-calendar-event"))
            .Take(5)
            .ToListAsync(cancellationToken);

        var growthRows = await (
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where user.CreatedAt >= startMonth && (role.Name == "Student" || role.Name == "Faculty")
            group user by new { user.CreatedAt.Year, user.CreatedAt.Month, Role = role.Name } into grouped
            select new
            {
                grouped.Key.Year,
                grouped.Key.Month,
                Role = grouped.Key.Role!,
                Count = grouped.Count()
            })
            .ToListAsync(cancellationToken);

        var growth = Enumerable.Range(0, 6)
            .Select(offset => startMonth.AddMonths(offset))
            .Select(month => new AdminGrowthData(
                month.ToString("MMM yyyy"),
                growthRows.Where(item => item.Year == month.Year && item.Month == month.Month && item.Role == "Student")
                    .Sum(item => item.Count),
                growthRows.Where(item => item.Year == month.Year && item.Month == month.Month && item.Role == "Faculty")
                    .Sum(item => item.Count)))
            .ToList();

        var recentActivity = recentUserActivity
            .Concat(recentPostActivity)
            .Concat(recentResourceActivity)
            .Concat(recentEventActivity)
            .OrderByDescending(item => item.OccurredAt)
            .Take(8)
            .ToList();

        return new AdminDashboardData(
            roleCounts.GetValueOrDefault("Student"),
            roleCounts.GetValueOrDefault("Faculty"),
            activeMemberCounts.GetValueOrDefault("Student"),
            activeMemberCounts.GetValueOrDefault("Faculty"),
            pendingRegistrations,
            studentCourseRecords,
            upcomingEvents,
            communityPosts,
            resources,
            unpublishedEvents,
            recentRegistrations,
            upcomingEventItems,
            recentActivity,
            growth);
    }
}
