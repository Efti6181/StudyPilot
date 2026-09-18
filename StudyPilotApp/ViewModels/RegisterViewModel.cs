using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must contain between 2 and 150 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your registered email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an account type.")]
    [Display(Name = "Account Type")]
    public string AccountType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your university ID.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Please enter a valid university ID.")]
    [Display(Name = "University ID")]
    public string UniversityId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please create a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
