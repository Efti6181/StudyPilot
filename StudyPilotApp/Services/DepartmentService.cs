using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _dbContext;

    public DepartmentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DepartmentListData> SearchAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var all = _dbContext.Departments.AsNoTracking();

        var totalCount = await all.CountAsync(cancellationToken);
        var activeCount = await all.CountAsync(item => item.IsActive, cancellationToken);
        var inactiveCount = totalCount - activeCount;

        IQueryable<Department> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, pattern) ||
                EF.Functions.ILike(item.Name, pattern) ||
                (item.Description != null && EF.Functions.ILike(item.Description, pattern)));
        }

        if (isActive.HasValue)
            query = query.Where(item => item.IsActive == isActive.Value);

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var items = await query
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new DepartmentData(
                item.Id,
                item.Code,
                item.Name,
                item.Description,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new DepartmentListData(
            items,
            totalCount,
            filteredCount,
            activeCount,
            inactiveCount,
            page,
            pageSize);
    }

    public Task<DepartmentData?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new DepartmentData(
                item.Id,
                item.Code,
                item.Name,
                item.Description,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<DepartmentSaveResult> CreateAsync(
        DepartmentInput input,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = Normalize(input.Code);
        var normalizedName = Normalize(input.Name);
        var duplicate = await FindDuplicateAsync(normalizedCode, normalizedName, null, cancellationToken);
        if (duplicate is not null) return DepartmentSaveResult.Failure(duplicate);

        var department = new Department
        {
            Code = input.Code.Trim().ToUpperInvariant(),
            NormalizedCode = normalizedCode,
            Name = input.Name.Trim(),
            NormalizedName = normalizedName,
            Description = CleanOptional(input.Description),
            IsActive = input.IsActive
        };

        _dbContext.Departments.Add(department);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return DepartmentSaveResult.Success(department.Id);
        }
        catch (DbUpdateException)
        {
            return DepartmentSaveResult.Failure("A department with this code or name already exists.");
        }
    }

    public async Task<DepartmentSaveResult> UpdateAsync(
        int id,
        DepartmentInput input,
        CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (department is null) return DepartmentSaveResult.Failure("Department not found.");

        var normalizedCode = Normalize(input.Code);
        var normalizedName = Normalize(input.Name);
        var duplicate = await FindDuplicateAsync(normalizedCode, normalizedName, id, cancellationToken);
        if (duplicate is not null) return DepartmentSaveResult.Failure(duplicate);

        department.Code = input.Code.Trim().ToUpperInvariant();
        department.NormalizedCode = normalizedCode;
        department.Name = input.Name.Trim();
        department.NormalizedName = normalizedName;
        department.Description = CleanOptional(input.Description);
        department.IsActive = input.IsActive;
        department.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return DepartmentSaveResult.Success(department.Id);
        }
        catch (DbUpdateException)
        {
            return DepartmentSaveResult.Failure("A department with this code or name already exists.");
        }
    }

    public async Task<DepartmentSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (department is null) return DepartmentSaveResult.Failure("Department not found.");
        if (department.IsActive == isActive) return DepartmentSaveResult.Success(id);

        if (!isActive && await _dbContext.AcademicPrograms
                .AnyAsync(item => item.DepartmentId == id && item.IsActive, cancellationToken))
        {
            return DepartmentSaveResult.Failure(
                "Deactivate this department's active programs before deactivating the department.");
        }

        if (!isActive && await _dbContext.UniversityMembers
                .AnyAsync(item => item.DepartmentId == id && item.IsActive, cancellationToken))
        {
            return DepartmentSaveResult.Failure(
                "Deactivate this department's active registration authorizations before deactivating it.");
        }

        if (!isActive && await _dbContext.CatalogCourses
                .AnyAsync(item => item.DepartmentId == id && item.IsActive, cancellationToken))
        {
            return DepartmentSaveResult.Failure(
                "Archive this department's active catalog courses before deactivating it.");
        }

        department.IsActive = isActive;
        department.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return DepartmentSaveResult.Success(id);
    }

    private async Task<string?> FindDuplicateAsync(
        string normalizedCode,
        string normalizedName,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.Departments.AsNoTracking()
            .Where(item => !excludedId.HasValue || item.Id != excludedId.Value)
            .Where(item => item.NormalizedCode == normalizedCode || item.NormalizedName == normalizedName)
            .Select(item => new { item.NormalizedCode })
            .FirstOrDefaultAsync(cancellationToken);

        return duplicate is null
            ? null
            : duplicate.NormalizedCode == normalizedCode
                ? "A department with this code already exists."
                : "A department with this name already exists.";
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
