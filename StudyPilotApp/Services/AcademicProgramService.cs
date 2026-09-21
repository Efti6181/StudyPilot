using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using System.Linq.Expressions;

namespace StudyPilotApp.Services;

public sealed class AcademicProgramService : IAcademicProgramService
{
    private static readonly Expression<Func<AcademicProgram, AcademicProgramData>> ProgramProjection = item => new(
        item.Id,
        item.Code,
        item.Name,
        item.Description,
        item.DepartmentId,
        item.Department.Code,
        item.Department.Name,
        item.TotalCredits,
        item.DurationYears,
        item.IsActive,
        item.CreatedAt,
        item.UpdatedAt);

    private readonly ApplicationDbContext _dbContext;

    public AcademicProgramService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AcademicProgramListData> SearchAsync(
        string? search,
        int? departmentId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var all = _dbContext.AcademicPrograms.AsNoTracking();
        var totalCount = await all.CountAsync(cancellationToken);
        var activeCount = await all.CountAsync(item => item.IsActive, cancellationToken);

        IQueryable<AcademicProgram> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, pattern) ||
                EF.Functions.ILike(item.Name, pattern) ||
                EF.Functions.ILike(item.Department.Code, pattern) ||
                EF.Functions.ILike(item.Department.Name, pattern) ||
                (item.Description != null && EF.Functions.ILike(item.Description, pattern)));
        }

        if (departmentId.HasValue)
            query = query.Where(item => item.DepartmentId == departmentId.Value);
        if (isActive.HasValue)
            query = query.Where(item => item.IsActive == isActive.Value);

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var items = await query
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Department.Name)
            .ThenBy(item => item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProgramProjection)
            .ToListAsync(cancellationToken);

        return new AcademicProgramListData(
            items,
            totalCount,
            filteredCount,
            activeCount,
            totalCount - activeCount,
            page,
            pageSize);
    }

    public Task<AcademicProgramData?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.AcademicPrograms.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(ProgramProjection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProgramDepartmentData>> GetDepartmentsAsync(
        bool activeOnly,
        int? includeDepartmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Departments.AsNoTracking();
        if (activeOnly)
            query = query.Where(item => item.IsActive || item.Id == includeDepartmentId);

        return await query
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Name)
            .Select(item => new ProgramDepartmentData(item.Id, item.Code, item.Name, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<AcademicProgramSaveResult> CreateAsync(
        AcademicProgramInput input,
        CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.DepartmentId, cancellationToken);
        if (department is null) return AcademicProgramSaveResult.Failure("Select a valid department.");
        if (input.IsActive && !department.IsActive)
            return AcademicProgramSaveResult.Failure("An active program must belong to an active department.");

        var normalizedCode = Normalize(input.Code);
        var normalizedName = Normalize(input.Name);
        var duplicate = await FindDuplicateAsync(
            normalizedCode, normalizedName, input.DepartmentId, null, cancellationToken);
        if (duplicate is not null) return AcademicProgramSaveResult.Failure(duplicate);

        var program = new AcademicProgram
        {
            Code = input.Code.Trim().ToUpperInvariant(),
            NormalizedCode = normalizedCode,
            Name = input.Name.Trim(),
            NormalizedName = normalizedName,
            Description = CleanOptional(input.Description),
            DepartmentId = input.DepartmentId,
            TotalCredits = input.TotalCredits,
            DurationYears = input.DurationYears,
            IsActive = input.IsActive
        };

        _dbContext.AcademicPrograms.Add(program);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AcademicProgramSaveResult.Success(program.Id);
        }
        catch (DbUpdateException)
        {
            return AcademicProgramSaveResult.Failure("A program with this code or name already exists.");
        }
    }

    public async Task<AcademicProgramSaveResult> UpdateAsync(
        int id,
        AcademicProgramInput input,
        CancellationToken cancellationToken = default)
    {
        var program = await _dbContext.AcademicPrograms.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (program is null) return AcademicProgramSaveResult.Failure("Program not found.");

        var department = await _dbContext.Departments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.DepartmentId, cancellationToken);
        if (department is null) return AcademicProgramSaveResult.Failure("Select a valid department.");
        if (input.IsActive && !department.IsActive)
            return AcademicProgramSaveResult.Failure("An active program must belong to an active department.");

        var normalizedCode = Normalize(input.Code);
        var normalizedName = Normalize(input.Name);
        var duplicate = await FindDuplicateAsync(
            normalizedCode, normalizedName, input.DepartmentId, id, cancellationToken);
        if (duplicate is not null) return AcademicProgramSaveResult.Failure(duplicate);

        program.Code = input.Code.Trim().ToUpperInvariant();
        program.NormalizedCode = normalizedCode;
        program.Name = input.Name.Trim();
        program.NormalizedName = normalizedName;
        program.Description = CleanOptional(input.Description);
        program.DepartmentId = input.DepartmentId;
        program.TotalCredits = input.TotalCredits;
        program.DurationYears = input.DurationYears;
        program.IsActive = input.IsActive;
        program.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AcademicProgramSaveResult.Success(id);
        }
        catch (DbUpdateException)
        {
            return AcademicProgramSaveResult.Failure("A program with this code or name already exists.");
        }
    }

    public async Task<AcademicProgramSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var program = await _dbContext.AcademicPrograms
            .Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (program is null) return AcademicProgramSaveResult.Failure("Program not found.");
        if (program.IsActive == isActive) return AcademicProgramSaveResult.Success(id);
        if (isActive && !program.Department.IsActive)
            return AcademicProgramSaveResult.Failure("Activate the department before activating this program.");
        if (!isActive && await _dbContext.UniversityMembers
                .AnyAsync(item => item.ProgramId == id && item.IsActive, cancellationToken))
            return AcademicProgramSaveResult.Failure(
                "Deactivate this program's active registration authorizations before deactivating it.");
        if (!isActive && await _dbContext.CatalogCourses
                .AnyAsync(item => item.ProgramId == id && item.IsActive, cancellationToken))
            return AcademicProgramSaveResult.Failure(
                "Archive this program's active catalog courses before deactivating it.");

        program.IsActive = isActive;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return AcademicProgramSaveResult.Success(id);
    }

    private async Task<string?> FindDuplicateAsync(
        string normalizedCode,
        string normalizedName,
        int departmentId,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.AcademicPrograms.AsNoTracking()
            .Where(item => !excludedId.HasValue || item.Id != excludedId.Value)
            .Where(item => item.NormalizedCode == normalizedCode ||
                           (item.DepartmentId == departmentId && item.NormalizedName == normalizedName))
            .Select(item => new { item.NormalizedCode })
            .FirstOrDefaultAsync(cancellationToken);

        return duplicate is null
            ? null
            : duplicate.NormalizedCode == normalizedCode
                ? "A program with this code already exists."
                : "This department already has a program with this name.";
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
