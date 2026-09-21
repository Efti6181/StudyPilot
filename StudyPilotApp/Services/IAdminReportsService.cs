namespace StudyPilotApp.Services;

public interface IAdminReportsService
{
    Task<AdminReportsData> GetAsync(CancellationToken cancellationToken = default);
    Task<AdminExportFile?> ExportAsync(string report, CancellationToken cancellationToken = default);
}

public sealed record AdminReportsData(
    int TotalUsers,
    int ActiveUsers,
    int TotalCourses,
    int CompletedAssessments,
    int TotalAssessments,
    int UpcomingEvents,
    int PublishedAnnouncements,
    int CommunityInteractions,
    decimal AverageCourseProgress,
    IReadOnlyList<string> GrowthLabels,
    IReadOnlyList<int> StudentGrowth,
    IReadOnlyList<int> FacultyGrowth,
    IReadOnlyList<string> RoleLabels,
    IReadOnlyList<int> RoleCounts,
    IReadOnlyList<string> ContentLabels,
    IReadOnlyList<int> ContentCounts,
    IReadOnlyList<AdminReportSummaryRow> SummaryRows);

public sealed record AdminReportSummaryRow(string Area, string Metric, string Value, string Context);

public sealed record AdminExportFile(string FileName, byte[] Content);
