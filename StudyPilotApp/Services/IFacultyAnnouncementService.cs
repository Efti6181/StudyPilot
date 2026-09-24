using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IFacultyAnnouncementService
{
    Task<IReadOnlyList<FacultyAnnouncementData>> SearchAsync(string facultyUserId, string? search, int? assignmentId, AnnouncementPriority? priority, FacultyAnnouncementStatus? status, CancellationToken cancellationToken = default);
    Task<FacultyAnnouncement?> GetOwnedAsync(string facultyUserId, int id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(string facultyUserId, FacultyAnnouncementInput input, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string facultyUserId, int id, FacultyAnnouncementInput input, CancellationToken cancellationToken = default);
    Task<FacultyAnnouncementPublishResult> PublishAsync(string facultyUserId, int id, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(string facultyUserId, int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteDraftAsync(string facultyUserId, int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCourseAnnouncementData>> SearchForStudentAsync(string studentUserId, string? search, int? courseId, AnnouncementPriority? priority, bool activeOnly, CancellationToken cancellationToken = default);
    Task<StudentCourseAnnouncementData?> GetForStudentAsync(string studentUserId, int recipientId, bool markRead, CancellationToken cancellationToken = default);
}

public sealed record FacultyAnnouncementInput(int AssignmentId, string Title, string Summary, string Content, AnnouncementPriority Priority, DateTimeOffset? ExpiresAt);
public sealed record FacultyAnnouncementData(int Id, int AssignmentId, string CourseCode, string CourseName, AcademicTerm Term, int AcademicYear, string Section, string Title, string Summary, AnnouncementPriority Priority, FacultyAnnouncementStatus Status, DateTimeOffset? ExpiresAt, DateTimeOffset? PublishedAt, int RecipientCount, int ReadCount, DateTimeOffset CreatedAt);
public sealed record FacultyAnnouncementPublishResult(bool Succeeded, int RecipientCount, string? Error = null);
public sealed record StudentCourseAnnouncementData(int RecipientId, int AnnouncementId, int CourseId, string CourseCode, string CourseName, string FacultyName, string Title, string Summary, string Content, AnnouncementPriority Priority, FacultyAnnouncementStatus Status, DateTimeOffset PublishedAt, DateTimeOffset? ExpiresAt, bool IsRead, DateTimeOffset DeliveredAt);
