using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IFacultyAssessmentService
{
    Task<IReadOnlyList<FacultyAssessmentData>> SearchAsync(string userId, string? search, int? assignmentId, FacultyAssessmentStatus? status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FacultyAssessmentCourseOption>> GetCourseOptionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<FacultyAssessment?> GetOwnedAsync(string userId, int id, bool trackChanges = false, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(string userId, FacultyAssessmentInput input, byte[]? attachment, string? fileName, string? contentType, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string userId, int id, FacultyAssessmentInput input, byte[]? attachment, string? fileName, string? contentType, bool removeAttachment, CancellationToken cancellationToken = default);
    Task<FacultyAssessmentPublishResult> PublishAsync(string userId, int id, CancellationToken cancellationToken = default);
    Task<bool> CloseAsync(string userId, int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteDraftAsync(string userId, int id, CancellationToken cancellationToken = default);
}

public sealed record FacultyAssessmentInput(int FacultyCourseAssignmentId, string Title, AssessmentType Type, string? Description, string? Instructions, DateOnly AssignedDate, DateTime DueDate, decimal? TotalMarks, decimal? WeightPercentage, AssessmentDifficulty Difficulty);
public sealed record FacultyAssessmentCourseOption(int AssignmentId, string Label);
public sealed record FacultyAssessmentData(int Id, int AssignmentId, string CourseCode, string CourseName, AcademicTerm Term, int AcademicYear, string Section, string Title, AssessmentType Type, DateOnly AssignedDate, DateTime DueDate, decimal? TotalMarks, decimal? WeightPercentage, AssessmentDifficulty Difficulty, FacultyAssessmentStatus Status, DateTimeOffset? PublishedAt, int StudentCount, bool HasAttachment, string? AttachmentFileName);
public sealed record FacultyAssessmentPublishResult(bool Succeeded, int StudentCount, string? Error = null);
