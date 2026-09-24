using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using StudyPilotApp.Services;
using StudyPilotApp.ViewModels;

namespace StudyPilotApp.Controllers;

[Authorize(Roles = "Faculty")]
public sealed class FacultyAssessmentsController : Controller
{
    private const int MaxAttachmentBytes = 5 * 1024 * 1024;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly IFacultyAssessmentService _service;
    public FacultyAssessmentsController(UserManager<ApplicationUser> users, ApplicationDbContext db, IFacultyAssessmentService service) { _users = users; _db = db; _service = service; }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? assignmentId, FacultyAssessmentStatus? status, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(100, search.Trim().Length)];
        if (status.HasValue && !Enum.IsDefined(status.Value)) status = null;
        var courses = await _service.GetCourseOptionsAsync(context.Value.User.Id, token);
        if (assignmentId.HasValue && courses.All(x => x.AssignmentId != assignmentId)) assignmentId = null;
        var model = new FacultyAssessmentIndexViewModel { Search = search, AssignmentId = assignmentId, Status = status, Courses = courses, Items = await _service.SearchAsync(context.Value.User.Id, search, assignmentId, status, token) };
        Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        var item = await _service.GetOwnedAsync(context.Value.User.Id, id, cancellationToken: token); if (item is null) return NotFound();
        var model = new FacultyAssessmentDetailsViewModel { Assessment = item, StudentCount = await _db.Assessments.CountAsync(x => x.FacultyAssessmentId == id, token) };
        Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? assignmentId, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        var model = new FacultyAssessmentFormViewModel { FacultyCourseAssignmentId = assignmentId, Type = AssessmentType.Assignment, Difficulty = AssessmentDifficulty.Medium, AssignedDate = DateOnly.FromDateTime(DateTime.Today), DueDate = DateTime.Today.AddDays(7).AddHours(23).AddMinutes(59) };
        await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxAttachmentBytes + 300_000)]
    public async Task<IActionResult> Create(FacultyAssessmentFormViewModel model, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        Validate(model); var file = await ReadAttachment(model.Attachment, token);
        if (!ModelState.IsValid) { await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model); }
        var id = await _service.CreateAsync(context.Value.User.Id, Input(model), file.Data, file.Name, file.Type, token);
        if (id == 0) return NotFound(); TempData["FacultySuccess"] = "Assessment draft created."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken token)
    {
        var context = await Context(token); if (context is null) return Challenge();
        var item = await _service.GetOwnedAsync(context.Value.User.Id, id, cancellationToken: token); if (item is null) return NotFound(); if (item.Status == FacultyAssessmentStatus.Closed) return BadRequest();
        var model = new FacultyAssessmentFormViewModel { Id = item.Id, FacultyCourseAssignmentId = item.FacultyCourseAssignmentId, Title = item.Title, Type = item.Type, Description = item.Description, Instructions = item.Instructions, AssignedDate = item.AssignedDate, DueDate = item.DueDate, TotalMarks = item.TotalMarks, WeightPercentage = item.WeightPercentage, Difficulty = item.Difficulty, HasAttachment = item.AttachmentData is not null, AttachmentFileName = item.AttachmentFileName, CurrentStatus = item.Status };
        await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(MaxAttachmentBytes + 300_000)]
    public async Task<IActionResult> Edit(int id, FacultyAssessmentFormViewModel model, CancellationToken token)
    {
        if (model.Id != id) return BadRequest(); var context = await Context(token); if (context is null) return Challenge();
        var current = await _service.GetOwnedAsync(context.Value.User.Id, id, cancellationToken: token); if (current is null) return NotFound();
        model.CurrentStatus = current.Status; model.HasAttachment = current.AttachmentData is not null; model.AttachmentFileName = current.AttachmentFileName;
        Validate(model); var file = await ReadAttachment(model.Attachment, token);
        if (!ModelState.IsValid) { await Prepare(model, context.Value.User.Id, token); Shell(model, context.Value.User, context.Value.Profile); return View(model); }
        if (!await _service.UpdateAsync(context.Value.User.Id, id, Input(model), file.Data, file.Name, file.Type, model.RemoveAttachment, token)) return NotFound();
        TempData["FacultySuccess"] = "Assessment updated successfully."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, CancellationToken token)
    {
        var userId = _users.GetUserId(User); if (userId is null) return Challenge(); var result = await _service.PublishAsync(userId, id, token);
        TempData[result.Succeeded ? "FacultySuccess" : "FacultyError"] = result.Succeeded ? $"Assessment published to {result.StudentCount} matching student course(s)." : result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, CancellationToken token) { var userId = _users.GetUserId(User); if (userId is null) return Challenge(); if (!await _service.CloseAsync(userId, id, token)) return NotFound(); TempData["FacultySuccess"] = "Assessment closed."; return RedirectToAction(nameof(Details), new { id }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken token) { var userId = _users.GetUserId(User); if (userId is null) return Challenge(); if (!await _service.DeleteDraftAsync(userId, id, token)) return BadRequest(); TempData["FacultySuccess"] = "Draft deleted."; return RedirectToAction(nameof(Index)); }

    [HttpGet]
    public async Task<IActionResult> Attachment(int id, CancellationToken token) { var userId = _users.GetUserId(User); if (userId is null) return Challenge(); var item = await _service.GetOwnedAsync(userId, id, cancellationToken: token); return item?.AttachmentData is null ? NotFound() : File(item.AttachmentData, item.AttachmentContentType!, item.AttachmentFileName); }

    private async Task Prepare(FacultyAssessmentFormViewModel model, string userId, CancellationToken token) => model.Courses = await _service.GetCourseOptionsAsync(userId, token);
    private void Validate(FacultyAssessmentFormViewModel model)
    {
        if (model.Type.HasValue && !Enum.IsDefined(model.Type.Value)) ModelState.AddModelError(nameof(model.Type), "Select a valid type.");
        if (model.Difficulty.HasValue && !Enum.IsDefined(model.Difficulty.Value)) ModelState.AddModelError(nameof(model.Difficulty), "Select a valid difficulty.");
        if (model.AssignedDate.HasValue && model.DueDate.HasValue && model.DueDate.Value < model.AssignedDate.Value.ToDateTime(TimeOnly.MinValue)) ModelState.AddModelError(nameof(model.DueDate), "Due date cannot be earlier than assigned date.");
    }
    private static FacultyAssessmentInput Input(FacultyAssessmentFormViewModel m) => new(m.FacultyCourseAssignmentId!.Value, m.Title, m.Type!.Value, m.Description, m.Instructions, m.AssignedDate!.Value, m.DueDate!.Value, m.TotalMarks, m.WeightPercentage, m.Difficulty!.Value);
    private async Task<(byte[]? Data, string? Name, string? Type)> ReadAttachment(IFormFile? file, CancellationToken token)
    {
        if (file is null) return (null, null, null); var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var types = new Dictionary<string, string> { [".pdf"]="application/pdf", [".docx"]="application/vnd.openxmlformats-officedocument.wordprocessingml.document", [".pptx"]="application/vnd.openxmlformats-officedocument.presentationml.presentation", [".zip"]="application/zip", [".png"]="image/png", [".jpg"]="image/jpeg", [".jpeg"]="image/jpeg" };
        if (file.Length is <= 0 or > MaxAttachmentBytes || !types.TryGetValue(ext, out var contentType)) { ModelState.AddModelError(nameof(FacultyAssessmentFormViewModel.Attachment), "Use a PDF, DOCX, PPTX, ZIP, PNG or JPG file up to 5 MB."); return (null,null,null); }
        await using var memory = new MemoryStream(); await file.CopyToAsync(memory, token); var data = memory.ToArray();
        var valid = ext switch { ".pdf" => data.AsSpan().StartsWith("%PDF"u8), ".png" => data.Length >= 8 && data.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}), ".jpg" or ".jpeg" => data.Length >= 3 && data[0]==255 && data[1]==216 && data[2]==255, _ => data.Length >= 4 && data[0]==80 && data[1]==75 && data[2]==3 && data[3]==4 };
        if (!valid) { ModelState.AddModelError(nameof(FacultyAssessmentFormViewModel.Attachment), "The attachment content does not match its extension."); return (null,null,null); }
        var safeName = Path.GetFileName(file.FileName);
        if (safeName.Length > 255) safeName = safeName[..255];
        return (data, safeName, contentType);
    }
    private async Task<(ApplicationUser User, FacultyProfile Profile)?> Context(CancellationToken token) { var user = await _users.GetUserAsync(User); if (user is null) return null; var profile = await _db.FacultyProfiles.AsNoTracking().Include(x=>x.Department).SingleOrDefaultAsync(x=>x.ApplicationUserId==user.Id, token); return profile is null ? null : (user,profile); }
    private static void Shell(FacultyShellViewModel m, ApplicationUser u, FacultyProfile p) { var name=string.IsNullOrWhiteSpace(u.FullName)?u.Email??"Faculty":u.FullName; var parts=name.Split(' ',StringSplitOptions.RemoveEmptyEntries); m.FullName=name;m.Email=u.Email??"";m.FacultyId=p.FacultyId;m.DepartmentLabel=p.Department?.Name??"Department not assigned";m.DesignationLabel=p.Designation??"Faculty member";m.HasProfileImage=p.ProfileImageData is not null;m.ProfileImageVersion=(p.UpdatedAt??p.CreatedAt).ToUnixTimeSeconds();m.Initials=parts.Length>1?$"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant():name[..Math.Min(2,name.Length)].ToUpperInvariant(); }
}
