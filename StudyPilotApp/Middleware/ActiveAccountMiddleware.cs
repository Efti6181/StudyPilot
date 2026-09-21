using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using System.Security.Claims;

namespace StudyPilotApp.Middleware;

public sealed class ActiveAccountMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveAccountMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext dbContext,
        SignInManager<ApplicationUser> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isActive = userId is not null && await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == userId)
                .Select(item => item.IsActive)
                .SingleOrDefaultAsync(context.RequestAborted);

            if (!isActive)
            {
                await signInManager.SignOutAsync();
                if (IsApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                var returnUrl = $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
                var loginUrl = $"/Account/Login?disabled=true&returnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(loginUrl);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsApiRequest(HttpRequest request) =>
        request.Path.StartsWithSegments("/api") ||
        request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
