using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AdminReportsService : IAdminReportsService
{
    private readonly ApplicationDbContext _dbContext;

    public AdminReportsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminReportsData> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var campusNow = DateTime.SpecifyKind(
            DateTime.UtcNow.AddHours(6),
            DateTimeKind.Unspecified);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-5);

        var roleRows = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            group role by role.Name into grouped
            select new { Role = grouped.Key ?? "Unknown", Count = grouped.Count() })
            .ToListAsync(cancellationToken);

        var growthRows = await (
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where user.CreatedAt >= monthStart && (role.Name == "Student" || role.Name == "Faculty")
            group user by new { user.CreatedAt.Year, user.CreatedAt.Month, Role = role.Name } into grouped
            select new { grouped.Key.Year, grouped.Key.Month, Role = grouped.Key.Role!, Count = grouped.Count() })
            .ToListAsync(cancellationToken);

        var months = Enumerable.Range(0, 6).Select(monthStart.AddMonths).ToList();
        var totalUsers = await _dbContext.Users.AsNoTracking().CountAsync(cancellationToken);
        var activeUsers = await _dbContext.Users.AsNoTracking().CountAsync(item => item.IsActive, cancellationToken);
        var totalCourses = await _dbContext.Courses.AsNoTracking().CountAsync(cancellationToken);
        var totalAssessments = await _dbContext.Assessments.AsNoTracking().CountAsync(cancellationToken);
        var completedAssessments = await _dbContext.Assessments.AsNoTracking()
            .CountAsync(item => item.Status == AssessmentStatus.Completed, cancellationToken);
        var overdueAssessments = await _dbContext.Assessments.AsNoTracking()
            .CountAsync(item => item.Status != AssessmentStatus.Completed && item.DueDate < campusNow, cancellationToken);
        var averageProgress = await _dbContext.Courses.AsNoTracking()
            .Select(item => (decimal?)item.ProgressPercentage)
            .AverageAsync(cancellationToken) ?? 0m;
        var upcomingEvents = await _dbContext.CampusEvents.AsNoTracking()
            .CountAsync(item => item.IsPublished && item.EndAt >= now, cancellationToken);
        var eventRegistrations = await _dbContext.EventRegistrations.AsNoTracking()
            .CountAsync(item => item.CancelledAt == null, cancellationToken);
        var publishedAnnouncements = await _dbContext.Announcements.AsNoTracking()
            .CountAsync(item => item.IsPublished, cancellationToken);
        var deliveredNotifications = await _dbContext.Announcements.AsNoTracking()
            .Where(item => item.IsPublished)
            .SumAsync(item => (int?)item.RecipientCount, cancellationToken) ?? 0;
        var postCount = await _dbContext.CommunityPosts.AsNoTracking().CountAsync(cancellationToken);
        var commentCount = await _dbContext.CommunityComments.AsNoTracking().CountAsync(cancellationToken);
        var postLikeCount = await _dbContext.CommunityPostLikes.AsNoTracking().CountAsync(cancellationToken);
        var commentLikeCount = await _dbContext.CommunityCommentLikes.AsNoTracking().CountAsync(cancellationToken);
        var resourceCount = await _dbContext.StudyResources.AsNoTracking().CountAsync(cancellationToken);
        var moderationCount = await _dbContext.ContentModerationRecords.AsNoTracking().CountAsync(cancellationToken);
        var interactions = postCount + commentCount + postLikeCount + commentLikeCount;

        var completionRate = totalAssessments == 0 ? 0m : completedAssessments * 100m / totalAssessments;
        var rows = new List<AdminReportSummaryRow>
        {
            new("Users", "Active accounts", activeUsers.ToString("N0"), $"{totalUsers:N0} total accounts"),
            new("Academics", "Assessment completion", $"{completionRate:N1}%", $"{completedAssessments:N0} of {totalAssessments:N0} completed"),
            new("Academics", "Overdue assessments", overdueAssessments.ToString("N0"), "Across all student course records"),
            new("Academics", "Average course progress", $"{averageProgress:N1}%", $"{totalCourses:N0} student course records"),
            new("Community", "Interactions", interactions.ToString("N0"), $"{postCount:N0} posts and {commentCount:N0} comments"),
            new("Events", "Active registrations", eventRegistrations.ToString("N0"), $"{upcomingEvents:N0} upcoming published events"),
            new("Communication", "Notifications delivered", deliveredNotifications.ToString("N0"), $"{publishedAnnouncements:N0} published announcements"),
            new("Governance", "Content removed", moderationCount.ToString("N0"), "Recorded moderation actions")
        };

        return new AdminReportsData(
            totalUsers, activeUsers, totalCourses, completedAssessments, totalAssessments,
            upcomingEvents, publishedAnnouncements, interactions, averageProgress,
            months.Select(item => item.ToString("MMM yyyy", CultureInfo.InvariantCulture)).ToList(),
            months.Select(month => growthRows.Where(item => item.Year == month.Year && item.Month == month.Month && item.Role == "Student").Sum(item => item.Count)).ToList(),
            months.Select(month => growthRows.Where(item => item.Year == month.Year && item.Month == month.Month && item.Role == "Faculty").Sum(item => item.Count)).ToList(),
            new[] { "Students", "Faculty", "Admins" },
            new[] {
                roleRows.Where(item => item.Role == "Student").Sum(item => item.Count),
                roleRows.Where(item => item.Role == "Faculty").Sum(item => item.Count),
                roleRows.Where(item => item.Role == "Admin").Sum(item => item.Count)
            },
            new[] { "Posts", "Comments", "Resources", "Event registrations" },
            new[] { postCount, commentCount, resourceCount, eventRegistrations },
            rows);
    }

    public async Task<AdminExportFile?> ExportAsync(string report, CancellationToken cancellationToken = default)
    {
        report = report.Trim().ToLowerInvariant();
        var rows = new List<IReadOnlyList<string>>();
        string fileName;

        switch (report)
        {
            case "users":
                rows.Add(new[] { "Full name", "Email", "Role", "Status", "Created (UTC)" });
                var users = await (
                    from user in _dbContext.Users.AsNoTracking()
                    join userRole in _dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                    join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    orderby user.CreatedAt descending
                    select new { user.FullName, user.Email, Role = role.Name, user.IsActive, user.CreatedAt })
                    .ToListAsync(cancellationToken);
                rows.AddRange(users.Select(item => (IReadOnlyList<string>)new[]
                {
                    item.FullName, item.Email ?? string.Empty, item.Role ?? string.Empty,
                    item.IsActive ? "Active" : "Inactive", item.CreatedAt.ToString("O")
                }));
                fileName = "studypilot-users.csv";
                break;
            case "academics":
                rows.Add(new[] { "Course code", "Course", "Semester", "Year", "Progress", "Assessments" });
                var courses = await _dbContext.Courses.AsNoTracking().OrderBy(item => item.CourseCode)
                    .Select(item => new { item.CourseCode, item.CourseName, item.Semester, item.AcademicYear, item.ProgressPercentage, Assessments = item.Assessments.Count })
                    .ToListAsync(cancellationToken);
                rows.AddRange(courses.Select(item => (IReadOnlyList<string>)new[]
                {
                    item.CourseCode, item.CourseName, item.Semester.ToString(), item.AcademicYear.ToString(),
                    item.ProgressPercentage.ToString(CultureInfo.InvariantCulture), item.Assessments.ToString()
                }));
                fileName = "studypilot-academics.csv";
                break;
            case "engagement":
                rows.Add(new[] { "Area", "Metric", "Value" });
                var data = await GetAsync(cancellationToken);
                rows.AddRange(data.SummaryRows.Select(item => (IReadOnlyList<string>)new[] { item.Area, item.Metric, item.Value }));
                fileName = "studypilot-engagement.csv";
                break;
            case "events":
                rows.Add(new[] { "Title", "Type", "Start (UTC)", "Published", "Active registrations", "Capacity" });
                var events = await _dbContext.CampusEvents.AsNoTracking().OrderByDescending(item => item.StartAt)
                    .Select(item => new { item.Title, item.Type, item.StartAt, item.IsPublished, Registrations = item.Registrations.Count(entry => entry.CancelledAt == null), item.Capacity })
                    .ToListAsync(cancellationToken);
                rows.AddRange(events.Select(item => (IReadOnlyList<string>)new[]
                {
                    item.Title, item.Type.ToString(), item.StartAt.ToString("O"), item.IsPublished ? "Yes" : "No",
                    item.Registrations.ToString(), item.Capacity?.ToString() ?? "Unlimited"
                }));
                fileName = "studypilot-events.csv";
                break;
            default:
                return null;
        }

        return new AdminExportFile(fileName, BuildCsv(rows));
    }

    private static byte[] BuildCsv(IEnumerable<IReadOnlyList<string>> rows)
    {
        static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
        var text = string.Join("\r\n", rows.Select(row => string.Join(",", row.Select(Escape))));
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(text + "\r\n");
    }
}
