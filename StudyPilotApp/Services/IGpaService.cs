using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IGpaService
{
    Task EnsureDefaultScaleAsync(string userId);
    Task<IReadOnlyList<GradingScaleEntry>> GetScaleAsync(string userId, bool trackChanges = false);
    Task<IReadOnlyList<SemesterResult>> GetSemestersAsync(string userId);
    Task<SemesterResult?> GetSemesterAsync(string userId, int semesterId, bool trackChanges = false);
    Task<CourseGrade?> GetCourseGradeAsync(string userId, int gradeId, bool trackChanges = false);
    Task<IReadOnlyList<Course>> GetAvailableCoursesAsync(string userId, int semesterId, int? includeCourseId = null);
    Task<bool> OwnsCourseAsync(string userId, int courseId);
    Task<bool> SemesterDuplicateExistsAsync(string userId, int semesterNumber, AcademicTerm term, int year, int? excludedId = null);
    Task<bool> CourseAlreadyAddedAsync(string userId, int semesterId, int courseId, int? excludedGradeId = null);
    Task<int> GetCourseGradeCountAsync(string userId, int courseId);
    Task AddSemesterAsync(SemesterResult semester);
    Task AddCourseGradeAsync(CourseGrade grade);
    void RemoveSemester(SemesterResult semester);
    void RemoveCourseGrade(CourseGrade grade);
    Task SaveChangesAsync();
}
