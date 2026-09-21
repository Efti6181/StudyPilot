using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : Controller
{
    public IActionResult Index() =>
        RedirectToAction("Index", "Dashboard", new { area = "Admin" });
}
