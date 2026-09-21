namespace StudyPilotApp.Services;

public interface IPlatformSettingsService
{
    Task<PlatformSettingsData> GetAsync(CancellationToken cancellationToken = default);
    Task<PlatformSettingsData> UpdateAsync(
        PlatformSettingsInput input,
        string adminUserId,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformSettingsInput(
    string InstitutionName,
    string SupportEmail,
    string TimeZoneId,
    string DateFormat,
    int DefaultPageSize,
    int AuditRetentionDays,
    bool StudentRegistrationEnabled,
    bool FacultyRegistrationEnabled,
    bool AcademicAiEnabled,
    bool CommunityEnabled,
    bool EventsEnabled,
    bool MaintenanceNoticeEnabled,
    string? MaintenanceNotice);

public sealed record PlatformSettingsData(
    string InstitutionName,
    string SupportEmail,
    string TimeZoneId,
    string DateFormat,
    int DefaultPageSize,
    int AuditRetentionDays,
    bool StudentRegistrationEnabled,
    bool FacultyRegistrationEnabled,
    bool AcademicAiEnabled,
    bool CommunityEnabled,
    bool EventsEnabled,
    bool MaintenanceNoticeEnabled,
    string? MaintenanceNotice,
    DateTimeOffset UpdatedAt,
    string? UpdatedByName);
