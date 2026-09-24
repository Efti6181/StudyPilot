using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StudyPilotApp.Services;

namespace StudyPilotApp.Filters;

public sealed class PlatformFeatureGateFilter : IAsyncActionFilter
{
    private readonly IPlatformSettingsService _settingsService;
    private readonly ITempDataDictionaryFactory _tempDataFactory;

    public PlatformFeatureGateFilter(
        IPlatformSettingsService settingsService,
        ITempDataDictionaryFactory tempDataFactory)
    {
        _settingsService = settingsService;
        _tempDataFactory = tempDataFactory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        if (context.HttpContext.User.IsInRole("Student") &&
            controller is "Community" or "Events" or "AcademicAI")
        {
            var settings = await _settingsService.GetAsync(context.HttpContext.RequestAborted);
            var enabled = controller switch
            {
                "Community" => settings.CommunityEnabled,
                "Events" => settings.EventsEnabled,
                "AcademicAI" => settings.AcademicAiEnabled,
                _ => true
            };
            if (!enabled)
            {
                var label = controller == "AcademicAI" ? "Academic AI" : controller;
                _tempDataFactory.GetTempData(context.HttpContext)["DashboardError"] =
                    $"{label} is temporarily unavailable. Please contact the StudyPilot administrator.";
                context.Result = new RedirectToActionResult("Index", "Student", null);
                return;
            }
        }

        if (context.HttpContext.User.IsInRole("Faculty") && controller == "FacultyAI")
        {
            var settings = await _settingsService.GetAsync(context.HttpContext.RequestAborted);
            if (!settings.AcademicAiEnabled)
            {
                _tempDataFactory.GetTempData(context.HttpContext)["FacultyError"] =
                    "Faculty AI is temporarily unavailable. Please contact the StudyPilot administrator.";
                context.Result = new RedirectToActionResult("Index", "Faculty", null);
                return;
            }
        }

        await next();
    }
}
