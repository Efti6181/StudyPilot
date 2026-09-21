using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class PlatformSettingsService : IPlatformSettingsService
{
    private const int SingletonId = 1;
    private readonly ApplicationDbContext _dbContext;

    public PlatformSettingsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformSettingsData> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.PlatformSettings
            .AsNoTracking()
            .Include(item => item.UpdatedByUser)
            .SingleOrDefaultAsync(item => item.Id == SingletonId, cancellationToken);

        if (settings is null)
        {
            settings = CreateDefaults();
            _dbContext.PlatformSettings.Add(settings);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToData(settings);
    }

    public async Task<PlatformSettingsData> UpdateAsync(
        PlatformSettingsInput input,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.PlatformSettings
            .Include(item => item.UpdatedByUser)
            .SingleOrDefaultAsync(item => item.Id == SingletonId, cancellationToken);
        if (settings is null)
        {
            settings = CreateDefaults();
            _dbContext.PlatformSettings.Add(settings);
        }

        settings.InstitutionName = input.InstitutionName.Trim();
        settings.SupportEmail = input.SupportEmail.Trim();
        settings.TimeZoneId = input.TimeZoneId.Trim();
        settings.DateFormat = input.DateFormat.Trim();
        settings.DefaultPageSize = input.DefaultPageSize;
        settings.AuditRetentionDays = input.AuditRetentionDays;
        settings.StudentRegistrationEnabled = input.StudentRegistrationEnabled;
        settings.FacultyRegistrationEnabled = input.FacultyRegistrationEnabled;
        settings.AcademicAiEnabled = input.AcademicAiEnabled;
        settings.CommunityEnabled = input.CommunityEnabled;
        settings.EventsEnabled = input.EventsEnabled;
        settings.MaintenanceNoticeEnabled = input.MaintenanceNoticeEnabled;
        settings.MaintenanceNotice = string.IsNullOrWhiteSpace(input.MaintenanceNotice) ? null : input.MaintenanceNotice.Trim();
        settings.UpdatedByUserId = adminUserId;
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(settings).Reference(item => item.UpdatedByUser).LoadAsync(cancellationToken);
        return ToData(settings);
    }

    private static PlatformSetting CreateDefaults() => new()
    {
        Id = SingletonId,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static PlatformSettingsData ToData(PlatformSetting item) => new(
        item.InstitutionName, item.SupportEmail, item.TimeZoneId, item.DateFormat,
        item.DefaultPageSize, item.AuditRetentionDays,
        item.StudentRegistrationEnabled, item.FacultyRegistrationEnabled,
        item.AcademicAiEnabled, item.CommunityEnabled, item.EventsEnabled,
        item.MaintenanceNoticeEnabled, item.MaintenanceNotice, item.UpdatedAt,
        item.UpdatedByUser?.FullName);
}
