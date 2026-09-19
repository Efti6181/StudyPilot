namespace StudyPilotApp.ViewModels;

public sealed class StudentSearchViewModel : StudentShellViewModel
{
    public string Query { get; set; } = string.Empty;
    public IReadOnlyList<StudentSearchResultViewModel> Results { get; set; } = [];
}

public sealed class StudentSearchResultViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-search";
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = "Index";
    public int? RouteId { get; set; }
}
