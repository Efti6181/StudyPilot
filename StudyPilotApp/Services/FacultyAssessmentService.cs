using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class FacultyAssessmentService : IFacultyAssessmentService
{
    private readonly ApplicationDbContext _db;
    public FacultyAssessmentService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FacultyAssessmentData>> SearchAsync(string userId, string? search, int? assignmentId, FacultyAssessmentStatus? status, CancellationToken cancellationToken = default)
    {
        var query = Owned(userId).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.FacultyCourseAssignment.CatalogCourse.Code, pattern));
        }
        if (assignmentId.HasValue) query = query.Where(x => x.FacultyCourseAssignmentId == assignmentId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return await query.OrderBy(x => x.Status).ThenBy(x => x.DueDate).Select(ToData()).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FacultyAssessmentCourseOption>> GetCourseOptionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.FacultyCourseAssignments.AsNoTracking()
            .Where(x => x.IsActive && x.FacultyProfile.ApplicationUserId == userId)
            .OrderByDescending(x => x.AcademicPeriod.IsCurrent).ThenBy(x => x.CatalogCourse.Code)
            .Select(x => new { x.Id, x.CatalogCourse.Code, x.CatalogCourse.Name, x.AcademicPeriod.Term, x.AcademicPeriod.AcademicYear, x.Section })
            .ToListAsync(cancellationToken);
        return rows.Select(x => new FacultyAssessmentCourseOption(x.Id, $"{x.Code} — {x.Name} · {x.Term} {x.AcademicYear} · Section {x.Section}")).ToList();
    }

    public Task<FacultyAssessment?> GetOwnedAsync(string userId, int id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = Owned(userId).Include(x => x.FacultyCourseAssignment).ThenInclude(x => x.CatalogCourse)
            .Include(x => x.FacultyCourseAssignment).ThenInclude(x => x.AcademicPeriod);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> CreateAsync(string userId, FacultyAssessmentInput input, byte[]? attachment, string? fileName, string? contentType, CancellationToken cancellationToken = default)
    {
        if (!await OwnsAssignment(userId, input.FacultyCourseAssignmentId, cancellationToken)) return 0;
        var item = new FacultyAssessment { Status = FacultyAssessmentStatus.Draft };
        Apply(item, input, attachment, fileName, contentType, false);
        _db.FacultyAssessments.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(string userId, int id, FacultyAssessmentInput input, byte[]? attachment, string? fileName, string? contentType, bool removeAttachment, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(userId, id, true, cancellationToken);
        if (item is null || item.Status == FacultyAssessmentStatus.Closed ||
            (item.Status == FacultyAssessmentStatus.Published && item.FacultyCourseAssignmentId != input.FacultyCourseAssignmentId) ||
            !await OwnsAssignment(userId, input.FacultyCourseAssignmentId, cancellationToken)) return false;
        var oldDue = item.DueDate;
        Apply(item, input, attachment, fileName, contentType, removeAttachment);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        if (item.Status == FacultyAssessmentStatus.Published)
        {
            var copies = await _db.Assessments.Where(x => x.FacultyAssessmentId == item.Id).ToListAsync(cancellationToken);
            foreach (var copy in copies) ApplyShared(copy, item);
            if (oldDue != item.DueDate)
            {
                foreach (var copy in copies) _db.AppNotifications.Add(new AppNotification { ApplicationUserId = copy.ApplicationUserId, Title = "Assessment deadline changed", Message = $"The deadline for {item.Title} is now {item.DueDate:dd MMM yyyy, h:mm tt}.", Type = NotificationType.Assessment, RelatedUrl = $"/Assessments/Details/{copy.Id}" });
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FacultyAssessmentPublishResult> PublishAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(userId, id, true, cancellationToken);
        if (item is null) return new(false, 0, "Assessment not found.");
        if (item.Status != FacultyAssessmentStatus.Draft) return new(false, 0, "Only draft assessments can be published.");
        if (item.DueDate <= DateTime.Now) return new(false, 0, "The due date must be in the future before publishing.");

        var assignment = item.FacultyCourseAssignment;
        var courses = await _db.Courses.Where(x => x.Status == CourseStatus.Active && x.FacultyCourseAssignmentId == assignment.Id).ToListAsync(cancellationToken);
        var existing = await _db.Assessments.Where(x => x.FacultyAssessmentId == item.Id).Select(x => x.CourseId).ToListAsync(cancellationToken);
        var copies = courses.Where(x => !existing.Contains(x.Id)).Select(course =>
        {
            var copy = new Assessment { ApplicationUserId = course.ApplicationUserId, CourseId = course.Id, FacultyAssessmentId = item.Id, Status = AssessmentStatus.NotStarted };
            ApplyShared(copy, item);
            return copy;
        }).ToList();
        _db.Assessments.AddRange(copies);
        item.Status = FacultyAssessmentStatus.Published;
        item.PublishedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var copy in copies) _db.AppNotifications.Add(new AppNotification { ApplicationUserId = copy.ApplicationUserId, Title = "New assessment published", Message = $"{assignment.CatalogCourse.Code}: {item.Title} is due {item.DueDate:dd MMM yyyy, h:mm tt}.", Type = NotificationType.Assessment, RelatedUrl = $"/Assessments/Details/{copy.Id}" });
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, copies.Count);
    }

    public async Task<bool> CloseAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(userId, id, true, cancellationToken);
        if (item is null || item.Status != FacultyAssessmentStatus.Published) return false;
        item.Status = FacultyAssessmentStatus.Closed; item.ClosedAt = DateTimeOffset.UtcNow; item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<bool> DeleteDraftAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(userId, id, true, cancellationToken);
        if (item is null || item.Status != FacultyAssessmentStatus.Draft) return false;
        _db.FacultyAssessments.Remove(item); await _db.SaveChangesAsync(cancellationToken); return true;
    }

    private IQueryable<FacultyAssessment> Owned(string userId) => _db.FacultyAssessments.Where(x => x.FacultyCourseAssignment.FacultyProfile.ApplicationUserId == userId);
    private Task<bool> OwnsAssignment(string userId, int id, CancellationToken token) => _db.FacultyCourseAssignments.AnyAsync(x => x.Id == id && x.IsActive && x.FacultyProfile.ApplicationUserId == userId, token);
    private static void Apply(FacultyAssessment item, FacultyAssessmentInput input, byte[]? data, string? name, string? type, bool remove)
    {
        item.FacultyCourseAssignmentId = input.FacultyCourseAssignmentId; item.Title = input.Title.Trim(); item.Type = input.Type;
        item.Description = Clean(input.Description); item.Instructions = Clean(input.Instructions); item.AssignedDate = input.AssignedDate;
        item.DueDate = DateTime.SpecifyKind(input.DueDate, DateTimeKind.Unspecified); item.TotalMarks = input.TotalMarks;
        item.WeightPercentage = input.WeightPercentage; item.Difficulty = input.Difficulty;
        if (data is not null) { item.AttachmentData = data; item.AttachmentFileName = name; item.AttachmentContentType = type; }
        else if (remove) { item.AttachmentData = null; item.AttachmentFileName = null; item.AttachmentContentType = null; }
    }
    private static void ApplyShared(Assessment copy, FacultyAssessment source)
    {
        copy.Title = source.Title; copy.Type = source.Type; copy.Description = source.Description; copy.Instructions = source.Instructions;
        copy.AssignedDate = source.AssignedDate; copy.DueDate = source.DueDate; copy.TotalMarks = source.TotalMarks;
        copy.WeightPercentage = source.WeightPercentage; copy.Difficulty = source.Difficulty; copy.UpdatedAt = DateTimeOffset.UtcNow;
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static System.Linq.Expressions.Expression<Func<FacultyAssessment, FacultyAssessmentData>> ToData() => x => new(x.Id, x.FacultyCourseAssignmentId, x.FacultyCourseAssignment.CatalogCourse.Code, x.FacultyCourseAssignment.CatalogCourse.Name, x.FacultyCourseAssignment.AcademicPeriod.Term, x.FacultyCourseAssignment.AcademicPeriod.AcademicYear, x.FacultyCourseAssignment.Section, x.Title, x.Type, x.AssignedDate, x.DueDate, x.TotalMarks, x.WeightPercentage, x.Difficulty, x.Status, x.PublishedAt, x.StudentAssessments.Count, x.AttachmentData != null, x.AttachmentFileName);
}
