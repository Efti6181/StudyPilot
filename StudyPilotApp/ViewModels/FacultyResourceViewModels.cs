using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using StudyPilotApp.Models;
using StudyPilotApp.Services;

namespace StudyPilotApp.ViewModels;

public sealed class FacultyResourceIndexViewModel:FacultyShellViewModel{public IReadOnlyList<FacultyResourceData> Items{get;set;}=[];public IReadOnlyList<FacultyAssessmentCourseOption> Courses{get;set;}=[];public string? Search{get;set;}public int? AssignmentId{get;set;}public ResourceCategory? Category{get;set;}public FacultyResourceStatus? Status{get;set;}}
public sealed class FacultyResourceFormViewModel:FacultyShellViewModel
{
    public int? Id{get;set;}[Required,Display(Name="Course and section")]public int? AssignmentId{get;set;}public IReadOnlyList<FacultyAssessmentCourseOption> Courses{get;set;}=[];
    [Required,StringLength(180,MinimumLength=2)]public string Title{get;set;}=string.Empty;[StringLength(3000)]public string? Description{get;set;}
    [Required]public ResourceKind? Kind{get;set;}[Required]public ResourceCategory? Category{get;set;}[StringLength(500)]public string? Tags{get;set;}
    [StringLength(2048),Display(Name="External URL")]public string? ExternalUrl{get;set;}[Display(Name="Upload file")]public IFormFile? Upload{get;set;}
    public string? ExistingFileName{get;set;}public FacultyResourceStatus CurrentStatus{get;set;}
}
public sealed class FacultyResourceDetailsViewModel:FacultyShellViewModel{public FacultyResource Resource{get;set;}=null!;public int StudentCount{get;set;}}
