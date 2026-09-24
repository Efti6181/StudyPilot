using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Data;
using StudyPilotApp.Models;

namespace StudyPilotApp.Services;

public sealed class FacultyResourceService : IFacultyResourceService
{
    private readonly ApplicationDbContext _db;
    public FacultyResourceService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FacultyResourceData>> SearchAsync(string userId, string? search, int? assignmentId, ResourceCategory? category, FacultyResourceStatus? status, CancellationToken token = default)
    {
        var query = Owned(userId).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { var pattern=$"%{search.Trim()}%"; query=query.Where(x=>EF.Functions.ILike(x.Title,pattern)||(x.Tags!=null&&EF.Functions.ILike(x.Tags,pattern))||EF.Functions.ILike(x.FacultyCourseAssignment.CatalogCourse.Code,pattern)); }
        if (assignmentId.HasValue) query=query.Where(x=>x.FacultyCourseAssignmentId==assignmentId);
        if (category.HasValue) query=query.Where(x=>x.Category==category);
        if (status.HasValue) query=query.Where(x=>x.Status==status);
        return await query.OrderBy(x=>x.Status).ThenByDescending(x=>x.CreatedAt).Select(x=>new FacultyResourceData(x.Id,x.FacultyCourseAssignmentId,x.FacultyCourseAssignment.CatalogCourse.Code,x.FacultyCourseAssignment.CatalogCourse.Name,x.FacultyCourseAssignment.AcademicPeriod.Term,x.FacultyCourseAssignment.AcademicPeriod.AcademicYear,x.FacultyCourseAssignment.Section,x.Title,x.Description,x.Kind,x.Category,x.Status,x.StudentResources.Count,x.CreatedAt)).ToListAsync(token);
    }

    public Task<FacultyResource?> GetOwnedAsync(string userId,int id,bool track=false,CancellationToken token=default)
    {
        var query=Owned(userId).Include(x=>x.FacultyCourseAssignment).ThenInclude(x=>x.CatalogCourse).Include(x=>x.FacultyCourseAssignment).ThenInclude(x=>x.AcademicPeriod);
        return (track?query:query.AsNoTracking()).SingleOrDefaultAsync(x=>x.Id==id,token);
    }

    public async Task<int> CreateAsync(string userId,FacultyResourceInput input,StoredResourceFile? file,CancellationToken token=default)
    {
        if(!await OwnsAssignment(userId,input.AssignmentId,token))return 0;var item=new FacultyResource();Apply(item,input,file);_db.Add(item);await _db.SaveChangesAsync(token);return item.Id;
    }

    public async Task<bool> UpdateAsync(string userId,int id,FacultyResourceInput input,StoredResourceFile? file,CancellationToken token=default)
    {
        var item=await GetOwnedAsync(userId,id,true,token);if(item is null||item.Status==FacultyResourceStatus.Archived||(item.Status==FacultyResourceStatus.Published&&item.FacultyCourseAssignmentId!=input.AssignmentId)||!await OwnsAssignment(userId,input.AssignmentId,token))return false;
        Apply(item,input,file);item.UpdatedAt=DateTimeOffset.UtcNow;
        if(item.Status==FacultyResourceStatus.Published){var copies=await _db.StudyResources.Where(x=>x.FacultyResourceId==id).ToListAsync(token);foreach(var copy in copies)ApplyShared(copy,item);}
        await _db.SaveChangesAsync(token);return true;
    }

    public async Task<FacultyResourcePublishResult> PublishAsync(string userId,int id,CancellationToken token=default)
    {
        var item=await GetOwnedAsync(userId,id,true,token);if(item is null)return new(false,0,"Resource not found.");if(item.Status!=FacultyResourceStatus.Draft)return new(false,0,"Only drafts can be published.");
        var a=item.FacultyCourseAssignment;var courses=await _db.Courses.Where(x=>x.Status==CourseStatus.Active&&x.FacultyCourseAssignmentId==a.Id).ToListAsync(token);
        var copies=courses.Select(c=>{var copy=new StudyResource{ApplicationUserId=c.ApplicationUserId,CourseId=c.Id,FacultyResourceId=item.Id};ApplyShared(copy,item);return copy;}).ToList();_db.StudyResources.AddRange(copies);item.Status=FacultyResourceStatus.Published;item.PublishedAt=DateTimeOffset.UtcNow;item.UpdatedAt=DateTimeOffset.UtcNow;await _db.SaveChangesAsync(token);
        foreach(var copy in copies)_db.AppNotifications.Add(new AppNotification{ApplicationUserId=copy.ApplicationUserId,Title="New learning resource",Message=$"{a.CatalogCourse.Code}: {item.Title} is now available.",Type=NotificationType.Academic,RelatedUrl=$"/Resources/Details/{copy.Id}"});await _db.SaveChangesAsync(token);return new(true,copies.Count);
    }

    public async Task<bool> ArchiveAsync(string userId,int id,CancellationToken token=default){var item=await GetOwnedAsync(userId,id,true,token);if(item is null||item.Status!=FacultyResourceStatus.Published)return false;item.Status=FacultyResourceStatus.Archived;item.ArchivedAt=DateTimeOffset.UtcNow;item.UpdatedAt=DateTimeOffset.UtcNow;await _db.SaveChangesAsync(token);return true;}
    public async Task<FacultyResource?> DeleteDraftAsync(string userId,int id,CancellationToken token=default){var item=await GetOwnedAsync(userId,id,true,token);if(item is null||item.Status!=FacultyResourceStatus.Draft)return null;_db.Remove(item);await _db.SaveChangesAsync(token);return item;}
    private IQueryable<FacultyResource> Owned(string userId)=>_db.FacultyResources.Where(x=>x.FacultyCourseAssignment.FacultyProfile.ApplicationUserId==userId);
    private Task<bool> OwnsAssignment(string userId,int id,CancellationToken token)=>_db.FacultyCourseAssignments.AnyAsync(x=>x.Id==id&&x.IsActive&&x.FacultyProfile.ApplicationUserId==userId,token);
    private static void Apply(FacultyResource x,FacultyResourceInput i,StoredResourceFile? f){x.FacultyCourseAssignmentId=i.AssignmentId;x.Title=i.Title.Trim();x.Description=Clean(i.Description);x.Kind=i.Kind;x.Category=i.Category;x.Tags=Clean(i.Tags);x.ExternalUrl=i.Kind==ResourceKind.Link?i.ExternalUrl:null;if(f is not null){x.OriginalFileName=f.OriginalFileName;x.StoredFileName=f.StoredFileName;x.ContentType=f.ContentType;x.FileSizeBytes=f.SizeBytes;}else if(i.Kind==ResourceKind.Link){x.OriginalFileName=null;x.StoredFileName=null;x.ContentType=null;x.FileSizeBytes=null;}}
    private static void ApplyShared(StudyResource x,FacultyResource s){x.Title=s.Title;x.Description=s.Description;x.Kind=s.Kind;x.Category=s.Category;x.Tags=s.Tags;x.ExternalUrl=s.ExternalUrl;x.OriginalFileName=s.OriginalFileName;x.StoredFileName=s.StoredFileName;x.ContentType=s.ContentType;x.FileSizeBytes=s.FileSizeBytes;x.UpdatedAt=DateTimeOffset.UtcNow;}
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
