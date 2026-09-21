using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StudyPilotApp.Services;

namespace StudyPilotApp.Filters;

public sealed class AdminAuditActionFilter : IAsyncActionFilter
{
    private readonly IAdminAuditService _auditService;
    private readonly ILogger<AdminAuditActionFilter> _logger;

    public AdminAuditActionFilter(IAdminAuditService auditService, ILogger<AdminAuditActionFilter> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!ShouldAudit(context.HttpContext))
        {
            await next();
            return;
        }

        ActionExecutedContext? executed = null;
        Exception? thrown = null;
        try
        {
            executed = await next();
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        var http = context.HttpContext;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var action = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        var statusCode = thrown is not null ? StatusCodes.Status500InternalServerError : ResolveStatusCode(executed, http.Response.StatusCode);
        var succeeded = thrown is null && statusCode < 400 && executed?.Canceled != true && executed?.Exception is null;
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                await _auditService.WriteAsync(new AdminAuditWriteData(
                    userId,
                    http.User.FindFirstValue(ClaimTypes.Name) ?? http.User.Identity?.Name ?? "Administrator",
                    action,
                    controller,
                    http.Request.Method,
                    http.Request.Path.Value ?? string.Empty,
                    context.RouteData.Values["id"]?.ToString(),
                    http.Connection.RemoteIpAddress?.ToString(),
                    http.Request.Headers["User-Agent"].ToString(),
                    succeeded,
                    statusCode,
                    $"Admin action {controller}.{action}"),
                    http.RequestAborted);
            }
            catch (Exception auditException)
            {
                _logger.LogWarning(auditException, "StudyPilot could not persist an admin audit entry for {Controller}.{Action}.", controller, action);
            }
        }

        if (thrown is not null) throw thrown;
    }

    private static bool ShouldAudit(HttpContext context) =>
        string.Equals(context.Request.RouteValues["area"]?.ToString(), "Admin", StringComparison.OrdinalIgnoreCase) &&
        (HttpMethods.IsPost(context.Request.Method) ||
         HttpMethods.IsPut(context.Request.Method) ||
         HttpMethods.IsPatch(context.Request.Method) ||
         HttpMethods.IsDelete(context.Request.Method));

    private static int ResolveStatusCode(ActionExecutedContext? context, int responseStatus)
    {
        return context?.Result switch
        {
            StatusCodeResult result => result.StatusCode,
            ObjectResult result when result.StatusCode.HasValue => result.StatusCode.Value,
            _ => responseStatus == 0 ? StatusCodes.Status200OK : responseStatus
        };
    }
}
