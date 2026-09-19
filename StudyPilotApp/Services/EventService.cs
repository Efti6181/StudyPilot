using System.Data;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class EventService : IEventService
{
    private readonly ApplicationDbContext _dbContext;

    public EventService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventPage> GetEventsAsync(
        string userId,
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string scope,
        string sort,
        int page,
        int pageSize)
    {
        var now = DateTimeOffset.UtcNow;
        var query = _dbContext.CampusEvents.AsNoTracking()
            .Where(item => item.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.ShortDescription, pattern) ||
                EF.Functions.ILike(item.Description, pattern) ||
                EF.Functions.ILike(item.OrganizerName, pattern) ||
                (item.Venue != null && EF.Functions.ILike(item.Venue, pattern)));
        }

        if (type.HasValue) query = query.Where(item => item.Type == type.Value);
        if (locationType.HasValue) query = query.Where(item => item.LocationType == locationType.Value);

        query = scope switch
        {
            "registered" => query.Where(item =>
                item.EndAt >= now && item.Registrations.Any(registration =>
                    registration.ApplicationUserId == userId && registration.CancelledAt == null)),
            "saved" => query.Where(item =>
                item.EndAt >= now && item.SavedByStudents.Any(saved => saved.ApplicationUserId == userId)),
            "past" => query.Where(item => item.EndAt < now),
            "all" => query,
            _ => query.Where(item => item.EndAt >= now)
        };

        var totalCount = await query.CountAsync();
        query = sort switch
        {
            "popular" => query.OrderByDescending(item =>
                                  item.Registrations.Count(registration => registration.CancelledAt == null))
                              .ThenBy(item => item.StartAt),
            "newest" => query.OrderByDescending(item => item.CreatedAt),
            "latest" => query.OrderByDescending(item => item.StartAt),
            _ => query.OrderBy(item => item.StartAt)
        };

        var items = await query
            .Include(item => item.Registrations)
            .Include(item => item.SavedByStudents)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return new EventPage(items, totalCount);
    }

    public async Task<EventSummary> GetSummaryAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var registered = await _dbContext.EventRegistrations.AsNoTracking()
            .CountAsync(item => item.ApplicationUserId == userId &&
                                item.CancelledAt == null &&
                                item.CampusEvent.IsPublished && item.CampusEvent.EndAt >= now);
        var saved = await _dbContext.SavedEvents.AsNoTracking()
            .CountAsync(item => item.ApplicationUserId == userId &&
                                item.CampusEvent.IsPublished && item.CampusEvent.EndAt >= now);
        return new EventSummary(registered, saved);
    }

    public Task<CampusEvent?> GetEventAsync(int eventId) =>
        _dbContext.CampusEvents.AsNoTracking()
            .Include(item => item.Registrations)
            .Include(item => item.SavedByStudents)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == eventId && item.IsPublished);

    public async Task<IReadOnlyList<CampusEvent>> GetUpcomingForDashboardAsync(string userId, int count)
    {
        var now = DateTimeOffset.UtcNow;
        return await _dbContext.CampusEvents.AsNoTracking()
            .Where(item => item.IsPublished && item.EndAt >= now)
            .Include(item => item.Registrations.Where(registration =>
                registration.ApplicationUserId == userId && registration.CancelledAt == null))
            .OrderBy(item => item.StartAt)
            .Take(Math.Clamp(count, 1, 10))
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<EventRegistrationResult> RegisterAsync(int eventId, string userId)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var campusEvent = await _dbContext.CampusEvents
            .FromSqlInterpolated($"SELECT * FROM \"CampusEvents\" WHERE \"Id\" = {eventId} FOR UPDATE")
            .SingleOrDefaultAsync();

        if (campusEvent is null || !campusEvent.IsPublished)
            return EventRegistrationResult.NotFound;

        var now = DateTimeOffset.UtcNow;
        if (campusEvent.EndAt <= now) return EventRegistrationResult.EventEnded;
        if (campusEvent.RegistrationDeadline < now || campusEvent.StartAt <= now)
            return EventRegistrationResult.RegistrationClosed;

        var existingRegistration = await _dbContext.EventRegistrations.SingleOrDefaultAsync(item =>
            item.CampusEventId == eventId && item.ApplicationUserId == userId);
        if (existingRegistration is not null && existingRegistration.CancelledAt is null)
            return EventRegistrationResult.AlreadyRegistered;

        if (campusEvent.Capacity.HasValue)
        {
            var registrationCount = await _dbContext.EventRegistrations
                .CountAsync(item => item.CampusEventId == eventId && item.CancelledAt == null);
            if (registrationCount >= campusEvent.Capacity.Value)
                return EventRegistrationResult.Full;
        }

        if (existingRegistration is null)
        {
            await _dbContext.EventRegistrations.AddAsync(new EventRegistration
            {
                CampusEventId = eventId,
                ApplicationUserId = userId
            });
        }
        else
        {
            existingRegistration.RegisteredAt = DateTimeOffset.UtcNow;
            existingRegistration.CancelledAt = null;
        }
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return EventRegistrationResult.Registered;
    }

    public async Task<bool> CancelRegistrationAsync(int eventId, string userId)
    {
        var registration = await _dbContext.EventRegistrations.SingleOrDefaultAsync(item =>
            item.CampusEventId == eventId && item.ApplicationUserId == userId);
        if (registration is null || registration.CancelledAt.HasValue) return false;
        registration.CancelledAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool?> ToggleSavedAsync(int eventId, string userId)
    {
        if (!await _dbContext.CampusEvents.AnyAsync(item => item.Id == eventId && item.IsPublished))
            return null;

        var saved = await _dbContext.SavedEvents.SingleOrDefaultAsync(item =>
            item.CampusEventId == eventId && item.ApplicationUserId == userId);
        if (saved is null)
        {
            await _dbContext.SavedEvents.AddAsync(new SavedEvent
            {
                CampusEventId = eventId,
                ApplicationUserId = userId
            });
            await _dbContext.SaveChangesAsync();
            return true;
        }

        _dbContext.SavedEvents.Remove(saved);
        await _dbContext.SaveChangesAsync();
        return false;
    }
}
