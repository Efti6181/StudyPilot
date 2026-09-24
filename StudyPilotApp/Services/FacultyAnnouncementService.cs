using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class FacultyAnnouncementService : IFacultyAnnouncementService
{
    private readonly ApplicationDbContext _db;
    public FacultyAnnouncementService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FacultyAnnouncementData>> SearchAsync(string facultyUserId, string? search, int? assignmentId, AnnouncementPriority? priority, FacultyAnnouncementStatus? status, CancellationToken cancellationToken = default)
    {
        var query = Owned(facultyUserId).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Summary, pattern) || EF.Functions.ILike(x.FacultyCourseAssignment.CatalogCourse.Code, pattern));
        }
        if (assignmentId.HasValue) query = query.Where(x => x.FacultyCourseAssignmentId == assignmentId.Value);
        if (priority.HasValue) query = query.Where(x => x.Priority == priority.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return await query.OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .Select(x => new FacultyAnnouncementData(x.Id, x.FacultyCourseAssignmentId, x.FacultyCourseAssignment.CatalogCourse.Code, x.FacultyCourseAssignment.CatalogCourse.Name, x.FacultyCourseAssignment.AcademicPeriod.Term, x.FacultyCourseAssignment.AcademicPeriod.AcademicYear, x.FacultyCourseAssignment.Section, x.Title, x.Summary, x.Priority, x.Status, x.ExpiresAt, x.PublishedAt, x.Recipients.Count, x.Recipients.Count(r => r.IsRead), x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<FacultyAnnouncement?> GetOwnedAsync(string facultyUserId, int id, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        var query = Owned(facultyUserId)
            .Include(x => x.FacultyCourseAssignment).ThenInclude(x => x.CatalogCourse)
            .Include(x => x.FacultyCourseAssignment).ThenInclude(x => x.AcademicPeriod);
        return (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> CreateAsync(string facultyUserId, FacultyAnnouncementInput input, CancellationToken cancellationToken = default)
    {
        if (!await OwnsAssignment(facultyUserId, input.AssignmentId, cancellationToken)) return 0;
        var item = new FacultyAnnouncement();
        Apply(item, input);
        _db.FacultyAnnouncements.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(string facultyUserId, int id, FacultyAnnouncementInput input, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(facultyUserId, id, true, cancellationToken);
        if (item is null || item.Status == FacultyAnnouncementStatus.Archived ||
            (item.Status == FacultyAnnouncementStatus.Published && item.FacultyCourseAssignmentId != input.AssignmentId) ||
            !await OwnsAssignment(facultyUserId, input.AssignmentId, cancellationToken) ||
            (item.Status == FacultyAnnouncementStatus.Published && input.ExpiresAt.HasValue && input.ExpiresAt <= DateTimeOffset.UtcNow)) return false;
        Apply(item, input);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<FacultyAnnouncementPublishResult> PublishAsync(string facultyUserId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(facultyUserId, id, true, cancellationToken);
        if (item is null) return new(false, 0, "Announcement not found.");
        if (item.Status != FacultyAnnouncementStatus.Draft) return new(false, 0, "Only draft announcements can be published.");
        if (item.ExpiresAt.HasValue && item.ExpiresAt <= DateTimeOffset.UtcNow) return new(false, 0, "The expiry time must be in the future.");

        var assignment = item.FacultyCourseAssignment;
        var courses = await _db.Courses.AsNoTracking()
            .Where(x => x.Status == CourseStatus.Active && x.FacultyCourseAssignmentId == assignment.Id)
            .Select(x => new { x.Id, x.ApplicationUserId }).ToListAsync(cancellationToken);
        var existing = await _db.FacultyAnnouncementRecipients.Where(x => x.FacultyAnnouncementId == id).Select(x => x.CourseId).ToListAsync(cancellationToken);
        var recipients = courses.Where(x => !existing.Contains(x.Id)).Select(x => new FacultyAnnouncementRecipient { FacultyAnnouncementId = id, CourseId = x.Id, ApplicationUserId = x.ApplicationUserId }).ToList();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.FacultyAnnouncementRecipients.AddRange(recipients);
        item.Status = FacultyAnnouncementStatus.Published;
        item.PublishedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        foreach (var recipient in recipients)
        {
            _db.AppNotifications.Add(new AppNotification
            {
                ApplicationUserId = recipient.ApplicationUserId,
                Title = NotificationTitle(item),
                Message = $"{assignment.CatalogCourse.Code}: {item.Summary}",
                Type = NotificationType.Academic,
                RelatedUrl = $"/CourseAnnouncements/Details/{recipient.Id}"
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(true, recipients.Count);
    }

    public async Task<bool> ArchiveAsync(string facultyUserId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(facultyUserId, id, true, cancellationToken);
        if (item is null || item.Status != FacultyAnnouncementStatus.Published) return false;
        item.Status = FacultyAnnouncementStatus.Archived;
        item.ArchivedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteDraftAsync(string facultyUserId, int id, CancellationToken cancellationToken = default)
    {
        var item = await GetOwnedAsync(facultyUserId, id, true, cancellationToken);
        if (item is null || item.Status != FacultyAnnouncementStatus.Draft) return false;
        _db.FacultyAnnouncements.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StudentCourseAnnouncementData>> SearchForStudentAsync(string studentUserId, string? search, int? courseId, AnnouncementPriority? priority, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var query = StudentRows(studentUserId).AsNoTracking().Where(x => x.FacultyAnnouncement.Status != FacultyAnnouncementStatus.Draft);
        if (activeOnly) query = query.Where(x => x.FacultyAnnouncement.Status == FacultyAnnouncementStatus.Published && (!x.FacultyAnnouncement.ExpiresAt.HasValue || x.FacultyAnnouncement.ExpiresAt >= now));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.FacultyAnnouncement.Title, pattern) || EF.Functions.ILike(x.FacultyAnnouncement.Summary, pattern) || EF.Functions.ILike(x.Course.CourseCode, pattern));
        }
        if (courseId.HasValue) query = query.Where(x => x.CourseId == courseId.Value);
        if (priority.HasValue) query = query.Where(x => x.FacultyAnnouncement.Priority == priority.Value);
        return await query.OrderBy(x => x.IsRead).ThenByDescending(x => x.FacultyAnnouncement.Priority).ThenByDescending(x => x.FacultyAnnouncement.PublishedAt)
            .Select(StudentProjection()).ToListAsync(cancellationToken);
    }

    public async Task<StudentCourseAnnouncementData?> GetForStudentAsync(string studentUserId, int recipientId, bool markRead, CancellationToken cancellationToken = default)
    {
        var query = StudentRows(studentUserId).Where(x => x.Id == recipientId && x.FacultyAnnouncement.Status != FacultyAnnouncementStatus.Draft);
        if (!markRead) return await query.AsNoTracking().Select(StudentProjection()).SingleOrDefaultAsync(cancellationToken);
        var recipient = await query.SingleOrDefaultAsync(cancellationToken);
        if (recipient is null) return null;
        if (!recipient.IsRead) { recipient.IsRead = true; recipient.ReadAt = DateTimeOffset.UtcNow; await _db.SaveChangesAsync(cancellationToken); }
        return await StudentRows(studentUserId).AsNoTracking().Where(x => x.Id == recipientId).Select(StudentProjection()).SingleAsync(cancellationToken);
    }

    private IQueryable<FacultyAnnouncement> Owned(string userId) => _db.FacultyAnnouncements.Where(x => x.FacultyCourseAssignment.FacultyProfile.ApplicationUserId == userId);
    private IQueryable<FacultyAnnouncementRecipient> StudentRows(string userId) => _db.FacultyAnnouncementRecipients.Where(x => x.ApplicationUserId == userId);
    private Task<bool> OwnsAssignment(string userId, int id, CancellationToken token) => _db.FacultyCourseAssignments.AnyAsync(x => x.Id == id && x.IsActive && x.FacultyProfile.ApplicationUserId == userId, token);
    private static void Apply(FacultyAnnouncement item, FacultyAnnouncementInput input) { item.FacultyCourseAssignmentId = input.AssignmentId; item.Title = input.Title.Trim(); item.Summary = input.Summary.Trim(); item.Content = input.Content.Trim(); item.Priority = input.Priority; item.ExpiresAt = input.ExpiresAt; }
    private static string NotificationTitle(FacultyAnnouncement item) { var title = item.Priority == AnnouncementPriority.Urgent ? $"Urgent: {item.Title}" : item.Title; return title[..Math.Min(title.Length, 160)]; }
    private static System.Linq.Expressions.Expression<Func<FacultyAnnouncementRecipient, StudentCourseAnnouncementData>> StudentProjection() => x => new(x.Id, x.FacultyAnnouncementId, x.CourseId, x.Course.CourseCode, x.Course.CourseName, x.FacultyAnnouncement.FacultyCourseAssignment.FacultyProfile.ApplicationUser.FullName, x.FacultyAnnouncement.Title, x.FacultyAnnouncement.Summary, x.FacultyAnnouncement.Content, x.FacultyAnnouncement.Priority, x.FacultyAnnouncement.Status, x.FacultyAnnouncement.PublishedAt!.Value, x.FacultyAnnouncement.ExpiresAt, x.IsRead, x.DeliveredAt);
}
