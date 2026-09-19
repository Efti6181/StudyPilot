using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public interface IPriorityService
{
    Task EnsureSettingsAsync(string userId);
    Task<PriorityWeightSettings> GetSettingsAsync(string userId, bool trackChanges = false);
    Task<IReadOnlyList<Course>> GetOwnedCoursesAsync(string userId);
    Task<Course?> GetOwnedCourseAsync(string userId, int courseId);
    Task<CoursePriorityPreference?> GetPreferenceAsync(string userId, int courseId, bool trackChanges = false);
    Task<IReadOnlyList<GradingScaleEntry>> GetGradingScaleAsync(string userId);
    Task AddPreferenceAsync(CoursePriorityPreference preference);
    void RemovePreference(CoursePriorityPreference preference);
    Task SaveChangesAsync();
}
