using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using System.Linq.Expressions;

namespace StudyPilotApp.Services;

public sealed class CourseCatalogService : ICourseCatalogService
{
    private static readonly Expression<Func<CatalogCourse, CatalogCourseData>> Projection = item => new(
        item.Id,
        item.Code,
        item.Name,
        item.DepartmentId,
        item.Department.Code,
        item.Department.Name,
        item.ProgramId,
        item.Program == null ? null : item.Program.Code,
        item.Program == null ? null : item.Program.Name,
        item.CreditHours,
        item.CourseType,
        item.RecommendedSemester,
        item.Difficulty,
        item.CareerRelevance,
        item.Description,
        item.Prerequisites,
        item.IsActive,
        item.CreatedAt,
        item.UpdatedAt);

    private readonly ApplicationDbContext _dbContext;

    public CourseCatalogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogCourseListData> SearchAsync(
        string? search,
        int? departmentId,
        int? programId,
        CourseType? courseType,
        CourseDifficultyLevel? difficulty,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var all = _dbContext.CatalogCourses.AsNoTracking();
        var totalCount = await all.CountAsync(cancellationToken);
        var activeCount = await all.CountAsync(item => item.IsActive, cancellationToken);
        var coreCount = await all.CountAsync(item => item.CareerRelevance == CareerRelevanceLevel.High, cancellationToken);
        var hardCount = await all.CountAsync(item => item.Difficulty == CourseDifficultyLevel.Hard, cancellationToken);

        IQueryable<CatalogCourse> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, pattern) ||
                EF.Functions.ILike(item.Name, pattern) ||
                EF.Functions.ILike(item.Department.Code, pattern) ||
                EF.Functions.ILike(item.Department.Name, pattern) ||
                (item.Program != null &&
                 (EF.Functions.ILike(item.Program.Code, pattern) ||
                  EF.Functions.ILike(item.Program.Name, pattern))) ||
                (item.Description != null && EF.Functions.ILike(item.Description, pattern)) ||
                (item.Prerequisites != null && EF.Functions.ILike(item.Prerequisites, pattern)));
        }

        if (departmentId.HasValue) query = query.Where(item => item.DepartmentId == departmentId.Value);
        if (programId.HasValue) query = query.Where(item => item.ProgramId == programId.Value);
        if (courseType.HasValue) query = query.Where(item => item.CourseType == courseType.Value);
        if (difficulty.HasValue) query = query.Where(item => item.Difficulty == difficulty.Value);
        if (isActive.HasValue) query = query.Where(item => item.IsActive == isActive.Value);

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Department.Code)
            .ThenBy(item => item.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new CatalogCourseListData(
            items, totalCount, activeCount, totalCount - activeCount, coreCount, hardCount,
            filteredCount, page, pageSize);
    }

    public Task<CatalogCourseData?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.CatalogCourses.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CatalogDepartmentData>> GetDepartmentsAsync(
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Departments.AsNoTracking();
        if (activeOnly) query = query.Where(item => item.IsActive || item.Id == includeId);
        return await query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name)
            .Select(item => new CatalogDepartmentData(item.Id, item.Code, item.Name, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogProgramData>> GetProgramsAsync(
        int? departmentId,
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AcademicPrograms.AsNoTracking();
        if (departmentId.HasValue) query = query.Where(item => item.DepartmentId == departmentId.Value);
        if (activeOnly) query = query.Where(item => item.IsActive || item.Id == includeId);
        return await query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name)
            .Select(item => new CatalogProgramData(item.Id, item.Code, item.Name, item.DepartmentId, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogCourseSaveResult> CreateAsync(
        CatalogCourseInput input,
        CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(input, null, cancellationToken);
        if (error is not null) return CatalogCourseSaveResult.Failure(error);
        var item = new CatalogCourse();
        Apply(item, input);
        _dbContext.CatalogCourses.Add(item);
        return await SaveAsync(item, cancellationToken);
    }

    public async Task<CatalogCourseSaveResult> UpdateAsync(
        int id,
        CatalogCourseInput input,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.CatalogCourses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return CatalogCourseSaveResult.Failure("Catalog course not found.");
        var error = await ValidateAsync(input, id, cancellationToken);
        if (error is not null) return CatalogCourseSaveResult.Failure(error);
        Apply(item, input);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        return await SaveAsync(item, cancellationToken);
    }

    public async Task<CatalogCourseSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.CatalogCourses
            .Include(x => x.Department)
            .Include(x => x.Program)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return CatalogCourseSaveResult.Failure("Catalog course not found.");
        if (item.IsActive == isActive) return CatalogCourseSaveResult.Success(id);
        if (isActive && !item.Department.IsActive)
            return CatalogCourseSaveResult.Failure("Activate the department before activating this course.");
        if (isActive && item.Program is not null && !item.Program.IsActive)
            return CatalogCourseSaveResult.Failure("Activate the program before activating this course.");

        item.IsActive = isActive;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CatalogCourseSaveResult.Success(id);
    }

    private async Task<string?> ValidateAsync(
        CatalogCourseInput input,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(typeof(CourseType), input.CourseType)) return "Select a valid course type.";
        if (!Enum.IsDefined(typeof(CourseDifficultyLevel), input.Difficulty)) return "Select a valid difficulty.";
        if (!Enum.IsDefined(typeof(CareerRelevanceLevel), input.CareerRelevance)) return "Select valid career relevance.";

        var department = await _dbContext.Departments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.DepartmentId, cancellationToken);
        if (department is null) return "Select a valid department.";
        if (input.IsActive && !department.IsActive) return "An active course must belong to an active department.";

        if (input.ProgramId.HasValue)
        {
            var program = await _dbContext.AcademicPrograms.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == input.ProgramId.Value, cancellationToken);
            if (program is null) return "Select a valid program or leave it empty.";
            if (program.DepartmentId != input.DepartmentId) return "The selected program does not belong to this department.";
            if (input.IsActive && !program.IsActive) return "An active course cannot belong to an inactive program.";
        }

        var normalizedCode = Normalize(input.Code);
        var duplicate = await _dbContext.CatalogCourses.AsNoTracking().AnyAsync(item =>
            (!excludedId.HasValue || item.Id != excludedId.Value) &&
            item.NormalizedCode == normalizedCode,
            cancellationToken);
        return duplicate ? "A catalog course with this code already exists." : null;
    }

    private static void Apply(CatalogCourse item, CatalogCourseInput input)
    {
        item.Code = input.Code.Trim().ToUpperInvariant();
        item.NormalizedCode = Normalize(input.Code);
        item.Name = input.Name.Trim();
        item.DepartmentId = input.DepartmentId;
        item.ProgramId = input.ProgramId;
        item.CreditHours = input.CreditHours;
        item.CourseType = input.CourseType;
        item.RecommendedSemester = input.RecommendedSemester;
        item.Difficulty = input.Difficulty;
        item.CareerRelevance = input.CareerRelevance;
        item.Description = Clean(input.Description);
        item.Prerequisites = Clean(input.Prerequisites);
        item.IsActive = input.IsActive;
    }

    private async Task<CatalogCourseSaveResult> SaveAsync(
        CatalogCourse item,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return CatalogCourseSaveResult.Success(item.Id);
        }
        catch (DbUpdateException)
        {
            return CatalogCourseSaveResult.Failure("A catalog course with this code already exists.");
        }
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
