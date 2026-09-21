using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using System.Linq.Expressions;

namespace StudyPilotApp.Services;

public sealed class AdminEventService : IAdminEventService
{
    private static readonly Expression<Func<CampusEvent, AdminManagedEventData>> Projection = item => new(
        item.Id,
        item.Title,
        item.ShortDescription,
        item.Description,
        item.Type,
        item.LocationType,
        item.Venue,
        item.OnlineUrl,
        item.OrganizerName,
        item.StartAt,
        item.EndAt,
        item.RegistrationDeadline,
        item.Capacity,
        item.IsPublished,
        item.CreatedByUser.FullName,
        item.CreatedAt,
        item.UpdatedAt,
        item.Registrations.Count(registration => registration.CancelledAt == null),
        item.Registrations.Count(registration => registration.CancelledAt != null),
        item.SavedByStudents.Count);

    private readonly ApplicationDbContext _dbContext;

    public AdminEventService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminEventListData> SearchAsync(
        string? search,
        CampusEventType? type,
        EventLocationType? locationType,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var now = DateTimeOffset.UtcNow;
        var all = _dbContext.CampusEvents.AsNoTracking();
        var totalCount = await all.CountAsync(cancellationToken);
        var publishedCount = await all.CountAsync(item => item.IsPublished, cancellationToken);
        var upcomingCount = await all.CountAsync(item => item.EndAt >= now, cancellationToken);
        var activeRegistrationCount = await _dbContext.EventRegistrations.AsNoTracking()
            .CountAsync(item => item.CancelledAt == null, cancellationToken);

        IQueryable<CampusEvent> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Title, pattern) ||
                EF.Functions.ILike(item.ShortDescription, pattern) ||
                EF.Functions.ILike(item.OrganizerName, pattern) ||
                (item.Venue != null && EF.Functions.ILike(item.Venue, pattern)));
        }

        if (type.HasValue) query = query.Where(item => item.Type == type.Value);
        if (locationType.HasValue) query = query.Where(item => item.LocationType == locationType.Value);
        query = status switch
        {
            "published" => query.Where(item => item.IsPublished),
            "draft" => query.Where(item => !item.IsPublished),
            "upcoming" => query.Where(item => item.StartAt > now),
            "ongoing" => query.Where(item => item.StartAt <= now && item.EndAt >= now),
            "ended" => query.Where(item => item.EndAt < now),
            _ => query
        };

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query
            .OrderByDescending(item => item.StartAt >= now)
            .ThenBy(item => item.StartAt >= now ? item.StartAt : DateTimeOffset.MaxValue)
            .ThenByDescending(item => item.StartAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new AdminEventListData(
            items, totalCount, publishedCount, totalCount - publishedCount,
            upcomingCount, activeRegistrationCount, filteredCount, page, pageSize);
    }

    public Task<AdminManagedEventData?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.CampusEvents.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminEventRegistrantData>> GetRegistrantsAsync(
        int eventId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.EventRegistrations.AsNoTracking()
            .Where(item => item.CampusEventId == eventId)
            .OrderBy(item => item.CancelledAt != null)
            .ThenBy(item => item.RegisteredAt)
            .Select(item => new AdminEventRegistrantData(
                item.ApplicationUserId,
                item.ApplicationUser.FullName,
                item.ApplicationUser.Email ?? string.Empty,
                item.RegisteredAt,
                item.CancelledAt))
            .ToListAsync(cancellationToken);

    public async Task<AdminEventSaveResult> CreateAsync(
        AdminEventInput input,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminUserId))
            return AdminEventSaveResult.Failure("The administrator account could not be verified.");
        var error = Validate(input, 0);
        if (error is not null) return AdminEventSaveResult.Failure(error);

        var item = new CampusEvent { CreatedByUserId = adminUserId };
        Apply(item, input);
        _dbContext.CampusEvents.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AdminEventSaveResult.Success(item.Id);
    }

    public async Task<AdminEventSaveResult> UpdateAsync(
        int id,
        AdminEventInput input,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.CampusEvents.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AdminEventSaveResult.Failure("Event not found.");
        var registrationCount = await _dbContext.EventRegistrations.AsNoTracking()
            .CountAsync(x => x.CampusEventId == id && x.CancelledAt == null, cancellationToken);
        var error = Validate(input, registrationCount);
        if (error is not null) return AdminEventSaveResult.Failure(error);

        Apply(item, input);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AdminEventSaveResult.Success(id);
    }

    public async Task<AdminEventSaveResult> SetPublishedAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.CampusEvents.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AdminEventSaveResult.Failure("Event not found.");
        if (item.IsPublished == isPublished) return AdminEventSaveResult.Success(id);
        if (isPublished)
        {
            var registrations = await _dbContext.EventRegistrations.AsNoTracking()
                .CountAsync(x => x.CampusEventId == id && x.CancelledAt == null, cancellationToken);
            var error = Validate(ToInput(item, true), registrations);
            if (error is not null) return AdminEventSaveResult.Failure(error);
        }

        item.IsPublished = isPublished;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AdminEventSaveResult.Success(id);
    }

    public async Task<AdminEventSaveResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.CampusEvents.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AdminEventSaveResult.Failure("Event not found.");
        var activeRegistrations = await _dbContext.EventRegistrations.AsNoTracking()
            .CountAsync(x => x.CampusEventId == id && x.CancelledAt == null, cancellationToken);
        if (activeRegistrations > 0)
            return AdminEventSaveResult.Failure("This event has active registrations. Unpublish it instead of deleting it.");

        _dbContext.CampusEvents.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AdminEventSaveResult.Success(id);
    }

    private static string? Validate(AdminEventInput input, int activeRegistrations)
    {
        if (!Enum.IsDefined(typeof(CampusEventType), input.Type)) return "Select a valid event type.";
        if (!Enum.IsDefined(typeof(EventLocationType), input.LocationType)) return "Select a valid event format.";
        if (input.EndAt <= input.StartAt) return "The end time must be after the start time.";
        if (input.RegistrationDeadline > input.StartAt) return "Registration must close before the event starts.";
        if (input.IsPublished && input.EndAt <= DateTimeOffset.UtcNow) return "An ended event cannot be published.";
        if (input.Capacity.HasValue && input.Capacity.Value < activeRegistrations)
            return $"Capacity cannot be lower than the {activeRegistrations} active registrations.";
        if (input.LocationType is EventLocationType.OnCampus or EventLocationType.Hybrid &&
            string.IsNullOrWhiteSpace(input.Venue))
            return "A venue is required for on-campus and hybrid events.";
        if (input.LocationType is EventLocationType.Online or EventLocationType.Hybrid)
        {
            if (!Uri.TryCreate(input.OnlineUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                return "Provide a valid HTTP or HTTPS online event URL.";
        }
        return null;
    }

    private static void Apply(CampusEvent item, AdminEventInput input)
    {
        item.Title = input.Title.Trim();
        item.ShortDescription = input.ShortDescription.Trim();
        item.Description = input.Description.Trim();
        item.Type = input.Type;
        item.LocationType = input.LocationType;
        item.Venue = input.LocationType == EventLocationType.Online ? null : Clean(input.Venue);
        item.OnlineUrl = input.LocationType == EventLocationType.OnCampus ? null : Clean(input.OnlineUrl);
        item.OrganizerName = input.OrganizerName.Trim();
        item.StartAt = input.StartAt;
        item.EndAt = input.EndAt;
        item.RegistrationDeadline = input.RegistrationDeadline;
        item.Capacity = input.Capacity;
        item.IsPublished = input.IsPublished;
    }

    private static AdminEventInput ToInput(CampusEvent item, bool isPublished) => new(
        item.Title, item.ShortDescription, item.Description, item.Type, item.LocationType,
        item.Venue, item.OnlineUrl, item.OrganizerName, item.StartAt, item.EndAt,
        item.RegistrationDeadline, item.Capacity, isPublished);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
