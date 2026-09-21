using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class AdminUserService : IAdminUserService
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.Ordinal)
    {
        "Student", "Faculty", "Admin"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<AdminUserListData> SearchAsync(
        string? search,
        string? role,
        bool? isActive,
        int? departmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        // Keep counting and filtering on mapped Identity columns. EF Core cannot
        // translate predicates applied after projection into AdminUserData.
        var all =
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join identityRole in _dbContext.Roles.AsNoTracking()
                on userRole.RoleId equals identityRole.Id
            join user in _dbContext.Users.AsNoTracking()
                on userRole.UserId equals user.Id
            where identityRole.Name == "Student" ||
                  identityRole.Name == "Faculty" ||
                  identityRole.Name == "Admin"
            select new { User = user, Role = identityRole.Name! };

        var totalCount = await all.CountAsync(cancellationToken);
        var roleCounts = await all
            .GroupBy(item => item.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);
        var activeCount = await all.CountAsync(item => item.User.IsActive, cancellationToken);

        var query = all;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.User.FullName, pattern) ||
                (item.User.Email != null && EF.Functions.ILike(item.User.Email, pattern)) ||
                _dbContext.UniversityMembers.Any(member =>
                    member.IsClaimed &&
                    member.ApplicationUserId == item.User.Id &&
                    (EF.Functions.ILike(member.UniversityId, pattern) ||
                     (member.Department != null &&
                      (EF.Functions.ILike(member.Department.Code, pattern) ||
                       EF.Functions.ILike(member.Department.Name, pattern))) ||
                     (member.Program != null &&
                      (EF.Functions.ILike(member.Program.Code, pattern) ||
                       EF.Functions.ILike(member.Program.Name, pattern))))) ||
                _dbContext.StudentProfiles.Any(profile =>
                    profile.ApplicationUserId == item.User.Id &&
                    (EF.Functions.ILike(profile.StudentId, pattern) ||
                     (profile.Department != null && EF.Functions.ILike(profile.Department, pattern)))) ||
                _dbContext.FacultyProfiles.Any(profile =>
                    profile.ApplicationUserId == item.User.Id &&
                    EF.Functions.ILike(profile.FacultyId, pattern)));
        }

        if (AllowedRoles.Contains(role ?? string.Empty))
            query = query.Where(item => item.Role == role);
        if (isActive.HasValue)
            query = query.Where(item => item.User.IsActive == isActive.Value);
        if (departmentId.HasValue)
        {
            var selectedDepartmentId = departmentId.Value;
            query = query.Where(item => _dbContext.UniversityMembers.Any(member =>
                member.IsClaimed &&
                member.ApplicationUserId == item.User.Id &&
                member.DepartmentId == selectedDepartmentId));
        }

        var filteredCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var identityRows = await query
            .OrderByDescending(item => item.User.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AdminUserIdentityRow(
                item.User.Id,
                item.User.FullName,
                item.User.Email ?? string.Empty,
                item.Role,
                item.User.IsActive,
                item.User.CreatedAt,
                item.User.DeactivatedAt,
                item.User.AccountStatusChangedAt,
                item.User.EmailConfirmed,
                item.User.PhoneNumber,
                item.User.AccessFailedCount,
                item.User.LockoutEnd))
            .ToListAsync(cancellationToken);

        var items = await AddAcademicDataAsync(identityRows, cancellationToken);
        var studentCount = roleCounts.GetValueOrDefault("Student");
        var facultyCount = roleCounts.GetValueOrDefault("Faculty");
        var adminCount = roleCounts.GetValueOrDefault("Admin");

        return new AdminUserListData(
            items, totalCount, studentCount, facultyCount, adminCount, activeCount,
            totalCount - activeCount, filteredCount, page, pageSize);
    }

    public async Task<AdminUserData?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var identityRow = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join identityRole in _dbContext.Roles.AsNoTracking()
                on userRole.RoleId equals identityRole.Id
            join user in _dbContext.Users.AsNoTracking()
                on userRole.UserId equals user.Id
            where user.Id == id &&
                  (identityRole.Name == "Student" ||
                   identityRole.Name == "Faculty" ||
                   identityRole.Name == "Admin")
            select new AdminUserIdentityRow(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                identityRole.Name!,
                user.IsActive,
                user.CreatedAt,
                user.DeactivatedAt,
                user.AccountStatusChangedAt,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.AccessFailedCount,
                user.LockoutEnd))
            .FirstOrDefaultAsync(cancellationToken);

        if (identityRow is null) return null;
        var items = await AddAcademicDataAsync([identityRow], cancellationToken);
        return items[0];
    }

    public async Task<AdminUserStatusResult> SetActiveAsync(
        string id,
        bool isActive,
        string actingAdminId,
        CancellationToken cancellationToken = default)
    {
        if (id == actingAdminId)
            return AdminUserStatusResult.Failure("You cannot change the status of your own Admin account.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return AdminUserStatusResult.Failure("User account not found.");
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return AdminUserStatusResult.Failure("Admin accounts are protected from this user-management action.");
        if (user.IsActive == isActive) return AdminUserStatusResult.Success();

        user.IsActive = isActive;
        user.DeactivatedAt = isActive ? null : DateTimeOffset.UtcNow;
        user.AccountStatusChangedAt = DateTimeOffset.UtcNow;
        var result = await _userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(item => item.Description));
            return AdminUserStatusResult.Failure($"Account status could not be updated. {error}");
        }

        return AdminUserStatusResult.Success();
    }

    private async Task<IReadOnlyList<AdminUserData>> AddAcademicDataAsync(
        IReadOnlyList<AdminUserIdentityRow> identityRows,
        CancellationToken cancellationToken)
    {
        if (identityRows.Count == 0) return [];
        var userIds = identityRows.Select(item => item.Id).ToArray();

        var memberRows = await _dbContext.UniversityMembers.AsNoTracking()
            .Where(item => item.IsClaimed &&
                           item.ApplicationUserId != null &&
                           userIds.Contains(item.ApplicationUserId))
            .Select(item => new AdminUserAcademicRow(
                item.ApplicationUserId!,
                item.UniversityId,
                item.DepartmentId,
                item.Department == null ? null : item.Department.Code,
                item.Department == null ? null : item.Department.Name,
                item.ProgramId,
                item.Program == null ? null : item.Program.Code,
                item.Program == null ? null : item.Program.Name,
                item.Batch,
                item.CurrentSemester))
            .ToListAsync(cancellationToken);

        var studentRows = await _dbContext.StudentProfiles.AsNoTracking()
            .Where(item => userIds.Contains(item.ApplicationUserId))
            .Select(item => new AdminUserLegacyStudentRow(
                item.ApplicationUserId,
                item.StudentId,
                item.Department,
                item.Batch,
                item.Semester))
            .ToListAsync(cancellationToken);

        var facultyRows = await _dbContext.FacultyProfiles.AsNoTracking()
            .Where(item => userIds.Contains(item.ApplicationUserId))
            .Select(item => new AdminUserLegacyFacultyRow(
                item.ApplicationUserId,
                item.FacultyId))
            .ToListAsync(cancellationToken);

        var members = memberRows.GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.First());
        var students = studentRows.ToDictionary(item => item.UserId);
        var faculty = facultyRows.ToDictionary(item => item.UserId);

        return identityRows.Select(identity =>
        {
            members.TryGetValue(identity.Id, out var member);
            students.TryGetValue(identity.Id, out var student);
            faculty.TryGetValue(identity.Id, out var facultyMember);

            return new AdminUserData(
                identity.Id,
                identity.FullName,
                identity.Email,
                identity.Role,
                identity.IsActive,
                identity.CreatedAt,
                identity.DeactivatedAt,
                identity.AccountStatusChangedAt,
                identity.EmailConfirmed,
                identity.PhoneNumber,
                identity.AccessFailedCount,
                identity.LockoutEnd,
                member?.UniversityId ?? student?.StudentId ?? facultyMember?.FacultyId,
                member?.DepartmentId,
                member?.DepartmentCode,
                member?.DepartmentName ?? student?.Department,
                member?.ProgramId,
                member?.ProgramCode,
                member?.ProgramName,
                member?.Batch ?? student?.Batch,
                member?.CurrentSemester ?? student?.Semester);
        }).ToList();
    }

    private sealed record AdminUserIdentityRow(
        string Id,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? DeactivatedAt,
        DateTimeOffset? AccountStatusChangedAt,
        bool EmailConfirmed,
        string? PhoneNumber,
        int AccessFailedCount,
        DateTimeOffset? LockoutEnd);

    private sealed record AdminUserAcademicRow(
        string UserId,
        string UniversityId,
        int? DepartmentId,
        string? DepartmentCode,
        string? DepartmentName,
        int? ProgramId,
        string? ProgramCode,
        string? ProgramName,
        string? Batch,
        int? CurrentSemester);

    private sealed record AdminUserLegacyStudentRow(
        string UserId,
        string StudentId,
        string? Department,
        string? Batch,
        int? Semester);

    private sealed record AdminUserLegacyFacultyRow(string UserId, string FacultyId);
}
