using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.ViewModels;

public class StudentProfileViewModel : StudentShellViewModel
{
    [Required(ErrorMessage = "Please select your department.")]
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Please select your academic program.")]
    [Display(Name = "Program")]
    public int? AcademicProgramId { get; set; }

    public IReadOnlyList<SelectListItem> DepartmentOptions { get; set; } = [];
    public IReadOnlyList<SelectListItem> ProgramOptions { get; set; } = [];

    public string Department { get; set; } = string.Empty;

    public string Program { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select your semester.")]
    [Range(1, 12, ErrorMessage = "Please select a valid semester.")]
    public int? Semester { get; set; }

    [Required(ErrorMessage = "Please enter your batch.")]
    [StringLength(30)]
    public string Batch { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    [RegularExpression(@"^\+?[0-9\s()\-]{7,20}$", ErrorMessage = "Please enter a valid phone number.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Guardian Phone Number")]
    [RegularExpression(@"^\+?[0-9\s()\-]{7,20}$", ErrorMessage = "Please enter a valid guardian phone number.")]
    public string? GuardianPhoneNumber { get; set; }

    [Display(Name = "Present Address")]
    [StringLength(250)]
    public string? PresentAddress { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    [Display(Name = "Blood Group")]
    [StringLength(5)]
    public string? BloodGroup { get; set; }

    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
    public string? Bio { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? ProfileImage { get; set; }

    [Display(Name = "Remove current photo")]
    public bool RemoveProfileImage { get; set; }

    public int CompletionPercentage { get; set; }
}
