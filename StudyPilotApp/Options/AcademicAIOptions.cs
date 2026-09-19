using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Options;

public sealed class AcademicAIOptions
{
    public const string SectionName = "AcademicAI";

    public bool Enabled { get; set; }

    [StringLength(500)]
    public string ApiKey { get; set; } = string.Empty;

    [StringLength(100)]
    public string Model { get; set; } = "gemini-3.8-flash";

    [Range(5, 60)]
    public int TimeoutSeconds { get; set; } = 25;

    [Range(256, 4096)]
    public int MaxOutputTokens { get; set; } = 1400;
}
