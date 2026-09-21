namespace StudyPilotApp.Services;

public interface IAdminAuditService
{
    Task WriteAsync(AdminAuditWriteData input, CancellationToken cancellationToken = default);
    Task<AdminAuditSearchData> SearchAsync(
        string? search,
        string? controller,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AdminExportFile> ExportAsync(
        string? search,
        string? controller,
        string status,
        CancellationToken cancellationToken = default);
}

public sealed record AdminAuditWriteData(
    string AdminUserId,
    string AdminName,
    string Action,
    string Controller,
    string HttpMethod,
    string RequestPath,
    string? EntityId,
    string? IpAddress,
    string? UserAgent,
    bool Succeeded,
    int StatusCode,
    string? Details);

public sealed record AdminAuditSearchData(
    int TotalCount,
    int TodayCount,
    int FailedCount,
    int UniqueAdminCount,
    int FilteredCount,
    int Page,
    int TotalPages,
    IReadOnlyList<string> Controllers,
    IReadOnlyList<AdminAuditRowData> Items);

public sealed record AdminAuditRowData(
    long Id,
    string AdminName,
    string Action,
    string Controller,
    string HttpMethod,
    string RequestPath,
    string? EntityId,
    string? IpAddress,
    bool Succeeded,
    int StatusCode,
    string? Details,
    DateTimeOffset CreatedAt);
