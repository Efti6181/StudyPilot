using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;
using System.Linq.Expressions;

namespace StudyPilotApp.Services;

public sealed class RegistrationAuthorizationService : IRegistrationAuthorizationService
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "Student", "Faculty"
    };

    private static readonly Expression<Func<UniversityMember, RegistrationAuthorizationData>> Projection = item => new(
        item.Id,
        item.UniversityId,
        item.Email,
        item.Role,
        item.FullName,
        item.DepartmentId,
        item.Department == null ? null : item.Department.Code,
        item.Department == null ? null : item.Department.Name,
        item.ProgramId,
        item.Program == null ? null : item.Program.Code,
        item.Program == null ? null : item.Program.Name,
        item.Batch,
        item.CurrentSemester,
        item.IsActive,
        item.IsClaimed,
        item.ApplicationUserId,
        item.RegisteredUser == null ? null : item.RegisteredUser.FullName,
        item.CreatedAt,
        item.RegisteredAt,
        item.UpdatedAt);

    private readonly ApplicationDbContext _dbContext;
    private readonly ILookupNormalizer _normalizer;

    public RegistrationAuthorizationService(
        ApplicationDbContext dbContext,
        ILookupNormalizer normalizer)
    {
        _dbContext = dbContext;
        _normalizer = normalizer;
    }

    public async Task<RegistrationAuthorizationListData> SearchAsync(
        string? search,
        string? role,
        string status,
        int? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var all = _dbContext.UniversityMembers.AsNoTracking()
            .Where(item => item.Role == "Student" || item.Role == "Faculty");

        var totalCount = await all.CountAsync(cancellationToken);
        var studentCount = await all.CountAsync(item => item.Role == "Student", cancellationToken);
        var registeredCount = await all.CountAsync(item => item.IsClaimed, cancellationToken);
        var unregisteredCount = await all.CountAsync(
            item => !item.IsClaimed && item.IsActive, cancellationToken);
        var disabledCount = await all.CountAsync(item => !item.IsActive, cancellationToken);

        IQueryable<UniversityMember> query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.UniversityId, pattern) ||
                EF.Functions.ILike(item.Email, pattern) ||
                (item.FullName != null && EF.Functions.ILike(item.FullName, pattern)) ||
                (item.Department != null &&
                    (EF.Functions.ILike(item.Department.Code, pattern) ||
                     EF.Functions.ILike(item.Department.Name, pattern))) ||
                (item.Program != null &&
                    (EF.Functions.ILike(item.Program.Code, pattern) ||
                     EF.Functions.ILike(item.Program.Name, pattern))));
        }

        if (AllowedRoles.Contains(role ?? string.Empty))
            query = query.Where(item => item.Role == role);
        if (departmentId.HasValue)
            query = query.Where(item => item.DepartmentId == departmentId.Value);

        query = status switch
        {
            "registered" => query.Where(item => item.IsClaimed),
            "unregistered" => query.Where(item => !item.IsClaimed && item.IsActive),
            "disabled" => query.Where(item => !item.IsActive),
            _ => query
        };

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new RegistrationAuthorizationListData(
            items,
            totalCount,
            studentCount,
            totalCount - studentCount,
            registeredCount,
            unregisteredCount,
            disabledCount,
            filteredCount,
            page,
            pageSize);
    }

    public Task<RegistrationAuthorizationData?> GetAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        _dbContext.UniversityMembers.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AuthorizationDepartmentData>> GetDepartmentsAsync(
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Departments.AsNoTracking();
        if (activeOnly) query = query.Where(item => item.IsActive || item.Id == includeId);
        return await query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name)
            .Select(item => new AuthorizationDepartmentData(item.Id, item.Code, item.Name, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorizationProgramData>> GetProgramsAsync(
        int departmentId,
        bool activeOnly,
        int? includeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AcademicPrograms.AsNoTracking()
            .Where(item => item.DepartmentId == departmentId);
        if (activeOnly) query = query.Where(item => item.IsActive || item.Id == includeId);
        return await query.OrderByDescending(item => item.IsActive).ThenBy(item => item.Name)
            .Select(item => new AuthorizationProgramData(
                item.Id, item.Code, item.Name, item.DepartmentId, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<RegistrationAuthorizationSaveResult> CreateAsync(
        RegistrationAuthorizationInput input,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAcademicSelectionAsync(input, cancellationToken);
        if (validationError is not null) return RegistrationAuthorizationSaveResult.Failure(validationError);

        var universityId = NormalizeUniversityId(input.UniversityId);
        var email = input.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        var duplicate = await FindDuplicateAsync(universityId, normalizedEmail, null, cancellationToken);
        if (duplicate is not null) return RegistrationAuthorizationSaveResult.Failure(duplicate);
        if (await _dbContext.Users.AsNoTracking()
                .AnyAsync(item => item.NormalizedEmail == normalizedEmail, cancellationToken))
            return RegistrationAuthorizationSaveResult.Failure("An Identity account with this email already exists.");

        var member = new UniversityMember
        {
            UniversityId = universityId,
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = input.Role,
            FullName = input.FullName.Trim(),
            DepartmentId = input.DepartmentId,
            ProgramId = input.Role == "Student" ? input.ProgramId : null,
            Batch = input.Role == "Student" ? CleanOptional(input.Batch) : null,
            CurrentSemester = input.Role == "Student" ? input.CurrentSemester : null,
            IsActive = input.IsActive,
            CreatedByAdminId = adminUserId
        };

        _dbContext.UniversityMembers.Add(member);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RegistrationAuthorizationSaveResult.Success(member.Id);
        }
        catch (DbUpdateException)
        {
            return RegistrationAuthorizationSaveResult.Failure(
                "This university ID or email already has an authorization record.");
        }
    }

    public async Task<RegistrationAuthorizationSaveResult> UpdateAsync(
        int id,
        RegistrationAuthorizationInput input,
        CancellationToken cancellationToken = default)
    {
        var member = await _dbContext.UniversityMembers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (member is null) return RegistrationAuthorizationSaveResult.Failure("Authorization record not found.");

        var universityId = NormalizeUniversityId(input.UniversityId);
        var email = input.Email.Trim();
        var normalizedEmail = NormalizeEmail(email);
        if (member.IsClaimed &&
            (member.UniversityId != universityId || member.NormalizedEmail != normalizedEmail || member.Role != input.Role))
        {
            return RegistrationAuthorizationSaveResult.Failure(
                "University ID, email, and user type cannot be changed after registration.");
        }

        var validationError = await ValidateAcademicSelectionAsync(input, cancellationToken);
        if (validationError is not null) return RegistrationAuthorizationSaveResult.Failure(validationError);
        var duplicate = await FindDuplicateAsync(universityId, normalizedEmail, id, cancellationToken);
        if (duplicate is not null) return RegistrationAuthorizationSaveResult.Failure(duplicate);

        if (!member.IsClaimed && member.NormalizedEmail != normalizedEmail &&
            await _dbContext.Users.AsNoTracking().AnyAsync(
                item => item.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return RegistrationAuthorizationSaveResult.Failure("An Identity account with this email already exists.");
        }

        member.UniversityId = universityId;
        member.Email = email;
        member.NormalizedEmail = normalizedEmail;
        member.Role = input.Role;
        member.FullName = input.FullName.Trim();
        member.DepartmentId = input.DepartmentId;
        member.ProgramId = input.Role == "Student" ? input.ProgramId : null;
        member.Batch = input.Role == "Student" ? CleanOptional(input.Batch) : null;
        member.CurrentSemester = input.Role == "Student" ? input.CurrentSemester : null;
        member.IsActive = input.IsActive;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RegistrationAuthorizationSaveResult.Success(id);
        }
        catch (DbUpdateException)
        {
            return RegistrationAuthorizationSaveResult.Failure(
                "This university ID or email already has an authorization record.");
        }
    }

    public async Task<RegistrationAuthorizationSaveResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var member = await _dbContext.UniversityMembers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (member is null) return RegistrationAuthorizationSaveResult.Failure("Authorization record not found.");
        if (member.IsActive == isActive) return RegistrationAuthorizationSaveResult.Success(id);

        if (isActive)
        {
            var input = new RegistrationAuthorizationInput(
                member.UniversityId,
                member.Email,
                member.Role,
                member.FullName ?? "Authorized user",
                member.DepartmentId ?? 0,
                member.ProgramId,
                member.Batch,
                member.CurrentSemester,
                true);
            var error = await ValidateAcademicSelectionAsync(input, cancellationToken);
            if (error is not null) return RegistrationAuthorizationSaveResult.Failure(error);
        }

        member.IsActive = isActive;
        member.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return RegistrationAuthorizationSaveResult.Success(id);
    }

    private async Task<string?> ValidateAcademicSelectionAsync(
        RegistrationAuthorizationInput input,
        CancellationToken cancellationToken)
    {
        if (!AllowedRoles.Contains(input.Role)) return "Select Student or Faculty.";
        var department = await _dbContext.Departments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.DepartmentId, cancellationToken);
        if (department is null) return "Select a valid department.";
        if (input.IsActive && !department.IsActive) return "Active authorization requires an active department.";

        if (input.Role == "Student")
        {
            if (!input.ProgramId.HasValue) return "Select a program for the Student.";
            var program = await _dbContext.AcademicPrograms.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == input.ProgramId.Value, cancellationToken);
            if (program is null || program.DepartmentId != input.DepartmentId)
                return "The selected program does not belong to this department.";
            if (input.IsActive && !program.IsActive) return "Active authorization requires an active program.";
            if (!input.CurrentSemester.HasValue) return "Enter the Student's current semester.";
        }

        return null;
    }

    private async Task<string?> FindDuplicateAsync(
        string universityId,
        string normalizedEmail,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var duplicate = await _dbContext.UniversityMembers.AsNoTracking()
            .Where(item => !excludedId.HasValue || item.Id != excludedId.Value)
            .Where(item => item.UniversityId == universityId || item.NormalizedEmail == normalizedEmail)
            .Select(item => new { item.UniversityId })
            .FirstOrDefaultAsync(cancellationToken);
        return duplicate is null
            ? null
            : duplicate.UniversityId == universityId
                ? "This university ID already has an authorization record."
                : "This email already has an authorization record.";
    }

    private string NormalizeEmail(string email) =>
        _normalizer.NormalizeEmail(email) ?? email.ToUpperInvariant();
    private static string NormalizeUniversityId(string value) => value.Trim().ToUpperInvariant();
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
