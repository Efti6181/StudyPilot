using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public class GradingScaleEntry
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    public ApplicationUser ApplicationUser { get; set; } = null!;

    [Required, StringLength(10)]
    public string LetterGrade { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "100")]
    public decimal MinimumPercentage { get; set; }

    [Range(typeof(decimal), "0", "10")]
    public decimal GradePoint { get; set; }

    public int SortOrder { get; set; }
}
