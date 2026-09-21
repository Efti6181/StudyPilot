using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AcademicPeriodService : IAcademicPeriodService
{
    private readonly ApplicationDbContext _dbContext;

    public AcademicPeriodService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AcademicPeriodListData> SearchAsync(
        string? search,
        string status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = _dbContext.AcademicPeriods.AsNoTracking();

        var totalCount = await all.CountAsync(cancellationToken);
        var currentCount = await all.CountAsync(item => item.IsActive && item.IsCurrent, cancellationToken);
        var upcomingCount = await all.CountAsync(item => item.IsActive && item.StartDate > today, cancellationToken);
        var pastCount = await all.CountAsync(item => item.IsActive && item.EndDate < today, cancellationToken);
        var inactiveCount = await all.CountAsync(item => !item.IsActive, cancellationToken);

        IQueryable<AcademicPeriod> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var termNames = Enum.GetValues<AcademicTerm>()
                .Where(term => term.ToString().Contains(search.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var hasYear = int.TryParse(search.Trim(), out var year);
            query = query.Where(item =>
                termNames.Contains(item.Term) ||
                (hasYear && item.AcademicYear == year) ||
                (item.Notes != null && EF.Functions.ILike(item.Notes, $"%{search.Trim()}%")));
        }

        query = status switch
        {
            "current" => query.Where(item => item.IsActive && item.IsCurrent),
            "upcoming" => query.Where(item => item.IsActive && item.StartDate > today),
            "past" => query.Where(item => item.IsActive && item.EndDate < today),
            "inactive" => query.Where(item => !item.IsActive),
            "active" => query.Where(item => item.IsActive),
            _ => query
        };

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query
            .OrderByDescending(item => item.IsCurrent)
            .ThenByDescending(item => item.AcademicYear)
            .ThenByDescending(item => item.Term)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AcademicPeriodData(
                item.Id, item.Term, item.AcademicYear, item.StartDate, item.EndDate,
                item.RegistrationStartDate, item.RegistrationEndDate, item.Notes,
                item.IsCurrent, item.IsActive, item.CreatedAt, item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new AcademicPeriodListData(
            items, totalCount, currentCount, upcomingCount, pastCount, inactiveCount,
            filteredCount, page, pageSize);
    }

    public Task<AcademicPeriodData?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.AcademicPeriods.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new AcademicPeriodData(
                item.Id, item.Term, item.AcademicYear, item.StartDate, item.EndDate,
                item.RegistrationStartDate, item.RegistrationEndDate, item.Notes,
                item.IsCurrent, item.IsActive, item.CreatedAt, item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AcademicPeriodSaveResult> CreateAsync(
        AcademicPeriodInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);
        if (error is not null) return AcademicPeriodSaveResult.Failure(error);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (input.IsCurrent)
        {
            await ClearCurrentAsync(null, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var item = new AcademicPeriod
        {
            Term = input.Term,
            AcademicYear = input.AcademicYear,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            RegistrationStartDate = input.RegistrationStartDate,
            RegistrationEndDate = input.RegistrationEndDate,
            Notes = Clean(input.Notes),
            IsCurrent = input.IsCurrent,
            IsActive = input.IsActive
        };
        _dbContext.AcademicPeriods.Add(item);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AcademicPeriodSaveResult.Success(item.Id);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AcademicPeriodSaveResult.Failure("This academic term already exists or conflicts with the current-term rule.");
        }
    }

    public async Task<AcademicPeriodSaveResult> UpdateAsync(
        int id,
        AcademicPeriodInput input,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.AcademicPeriods.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AcademicPeriodSaveResult.Failure("Academic term not found.");
        var error = await ValidateAsync(input, id, cancellationToken);
        if (error is not null) return AcademicPeriodSaveResult.Failure(error);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (input.IsCurrent)
        {
            await ClearCurrentAsync(id, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        item.Term = input.Term;
        item.AcademicYear = input.AcademicYear;
        item.StartDate = input.StartDate;
        item.EndDate = input.EndDate;
        item.RegistrationStartDate = input.RegistrationStartDate;
        item.RegistrationEndDate = input.RegistrationEndDate;
        item.Notes = Clean(input.Notes);
        item.IsCurrent = input.IsCurrent;
        item.IsActive = input.IsActive;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AcademicPeriodSaveResult.Success(id);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AcademicPeriodSaveResult.Failure("This academic term already exists or conflicts with the current-term rule.");
        }
    }

    public async Task<AcademicPeriodSaveResult> SetCurrentAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.AcademicPeriods.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AcademicPeriodSaveResult.Failure("Academic term not found.");
        if (!item.IsActive) return AcademicPeriodSaveResult.Failure("Activate this academic term before making it current.");
        if (item.IsCurrent) return AcademicPeriodSaveResult.Success(id);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ClearCurrentAsync(id, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        item.IsCurrent = true;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AcademicPeriodSaveResult.Success(id);
    }

    public async Task<AcademicPeriodSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.AcademicPeriods.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return AcademicPeriodSaveResult.Failure("Academic term not found.");
        if (item.IsActive == isActive) return AcademicPeriodSaveResult.Success(id);
        if (!isActive && item.IsCurrent)
            return AcademicPeriodSaveResult.Failure("Choose another current term before deactivating this one.");

        item.IsActive = isActive;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AcademicPeriodSaveResult.Success(id);
    }

    private async Task<string?> ValidateAsync(
        AcademicPeriodInput input,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(AcademicTerm), input.Term)) return "Select a valid academic term.";
        if (input.AcademicYear is < 2000 or > 2100) return "Academic year must be between 2000 and 2100.";
        if (input.EndDate <= input.StartDate) return "End date must be after the start date.";
        if (input.IsCurrent && !input.IsActive) return "A current term must be active.";
        if (input.RegistrationStartDate.HasValue != input.RegistrationEndDate.HasValue)
            return "Provide both registration dates or leave both empty.";
        if (input.RegistrationStartDate.HasValue && input.RegistrationEndDate.HasValue)
        {
            if (input.RegistrationEndDate.Value < input.RegistrationStartDate.Value)
                return "Registration end date must be on or after its start date.";
            if (input.RegistrationEndDate.Value > input.EndDate)
                return "Registration cannot end after the academic term ends.";
        }

        var duplicate = await _dbContext.AcademicPeriods.AsNoTracking().AnyAsync(item =>
            (!excludedId.HasValue || item.Id != excludedId.Value) &&
            item.Term == input.Term && item.AcademicYear == input.AcademicYear,
            cancellationToken);
        if (duplicate) return $"{input.Term} {input.AcademicYear} already exists.";

        var overlap = await _dbContext.AcademicPeriods.AsNoTracking().AnyAsync(item =>
            (!excludedId.HasValue || item.Id != excludedId.Value) &&
            item.IsActive && input.IsActive &&
            item.StartDate <= input.EndDate && item.EndDate >= input.StartDate,
            cancellationToken);
        return overlap ? "The date range overlaps another active academic term." : null;
    }

    private async Task ClearCurrentAsync(int? exceptId, CancellationToken cancellationToken)
    {
        var currentItems = await _dbContext.AcademicPeriods
            .Where(item => item.IsCurrent && (!exceptId.HasValue || item.Id != exceptId.Value))
            .ToListAsync(cancellationToken);
        foreach (var current in currentItems)
        {
            current.IsCurrent = false;
            current.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
