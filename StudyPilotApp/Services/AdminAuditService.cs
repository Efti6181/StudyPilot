using System.Text;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AdminAuditService : IAdminAuditService
{
    private readonly ApplicationDbContext _dbContext;

    public AdminAuditService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(AdminAuditWriteData input, CancellationToken cancellationToken = default)
    {
        _dbContext.AdminAuditLogs.Add(new AdminAuditLog
        {
            AdminUserId = Truncate(input.AdminUserId, 450),
            AdminName = Truncate(input.AdminName, 150),
            Action = Truncate(input.Action, 100),
            Controller = Truncate(input.Controller, 100),
            HttpMethod = Truncate(input.HttpMethod, 10),
            RequestPath = Truncate(input.RequestPath, 500),
            EntityId = TruncateNullable(input.EntityId, 100),
            IpAddress = TruncateNullable(input.IpAddress, 64),
            UserAgent = TruncateNullable(input.UserAgent, 500),
            Succeeded = input.Succeeded,
            StatusCode = input.StatusCode,
            Details = TruncateNullable(input.Details, 1000),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminAuditSearchData> SearchAsync(
        string? search,
        string? controller,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var source = _dbContext.AdminAuditLogs.AsNoTracking();

        var totalCount = await source.CountAsync(cancellationToken);
        var todayCount = await source.CountAsync(item => item.CreatedAt >= today, cancellationToken);
        var failedCount = await source.CountAsync(item => !item.Succeeded, cancellationToken);
        var uniqueAdminCount = await source.Select(item => item.AdminUserId).Distinct().CountAsync(cancellationToken);
        var controllers = await source.Select(item => item.Controller).Distinct().OrderBy(item => item).ToListAsync(cancellationToken);

        var query = ApplyFilters(source, search, controller, status);
        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query.OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new AdminAuditRowData(
                item.Id, item.AdminName, item.Action, item.Controller, item.HttpMethod,
                item.RequestPath, item.EntityId, item.IpAddress, item.Succeeded,
                item.StatusCode, item.Details, item.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminAuditSearchData(
            totalCount, todayCount, failedCount, uniqueAdminCount,
            filteredCount, page, totalPages, controllers, items);
    }

    public async Task<AdminExportFile> ExportAsync(
        string? search,
        string? controller,
        string status,
        CancellationToken cancellationToken = default)
    {
        var items = await ApplyFilters(_dbContext.AdminAuditLogs.AsNoTracking(), search, controller, status)
            .OrderByDescending(item => item.CreatedAt)
            .Take(10000)
            .Select(item => new
            {
                item.CreatedAt, item.AdminName, item.AdminUserId, item.Controller,
                item.Action, item.HttpMethod, item.EntityId, item.Succeeded,
                item.StatusCode, item.IpAddress, item.RequestPath, item.Details
            })
            .ToListAsync(cancellationToken);

        static string Escape(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        var lines = new List<string>
        {
            string.Join(",", new[] { "Created (UTC)", "Administrator", "Admin user ID", "Controller", "Action", "Method", "Entity ID", "Succeeded", "Status", "IP", "Path", "Details" }.Select(Escape))
        };
        lines.AddRange(items.Select(item => string.Join(",", new[]
        {
            item.CreatedAt.ToString("O"), item.AdminName, item.AdminUserId, item.Controller,
            item.Action, item.HttpMethod, item.EntityId, item.Succeeded ? "Yes" : "No",
            item.StatusCode.ToString(), item.IpAddress, item.RequestPath, item.Details
        }.Select(Escape))));
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(string.Join("\r\n", lines) + "\r\n");
        return new AdminExportFile("studypilot-admin-audit.csv", bytes);
    }

    private static IQueryable<AdminAuditLog> ApplyFilters(
        IQueryable<AdminAuditLog> query,
        string? search,
        string? controller,
        string status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLower();
            query = query.Where(item =>
                item.AdminName.ToLower().Contains(value) ||
                item.Action.ToLower().Contains(value) ||
                item.Controller.ToLower().Contains(value) ||
                item.RequestPath.ToLower().Contains(value) ||
                (item.EntityId != null && item.EntityId.ToLower().Contains(value)));
        }
        if (!string.IsNullOrWhiteSpace(controller)) query = query.Where(item => item.Controller == controller);
        if (status == "success") query = query.Where(item => item.Succeeded);
        if (status == "failed") query = query.Where(item => !item.Succeeded);
        return query;
    }

    private static string Truncate(string value, int length) => value[..Math.Min(value.Length, length)];
    private static string? TruncateNullable(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, length)];
}
