using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.ViewModels;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please create a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must contain at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
