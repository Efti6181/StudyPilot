using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum CourseStatus
{
    Active = 1,
    Completed = 2,
    Dropped = 3
}

public enum CourseType
{
    Theory = 1,
    Lab = 2,
    Project = 3,
    Thesis = 4,
    Other = 5
}

public enum AcademicTerm
{
    Spring = 1,
    Summer = 2,
    Fall = 3
}

public enum CourseColor
{
    [Display(Name = "StudyPilot Blue")]
    Blue = 1,
    Teal = 2,
    Purple = 3,
    Orange = 4,
    Red = 5
}
