using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum AssessmentType
{
    Assignment = 1,
    Quiz = 2,
    [Display(Name = "Midterm")]
    Midterm = 3,
    [Display(Name = "Final Exam")]
    FinalExam = 4,
    Lab = 5,
    Project = 6,
    Presentation = 7,
    Viva = 8,
    [Display(Name = "Class Test")]
    ClassTest = 9,
    Other = 10
}

public enum AssessmentStatus
{
    [Display(Name = "Not Started")]
    NotStarted = 1,
    [Display(Name = "In Progress")]
    InProgress = 2,
    Completed = 3
}

public enum AssessmentDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}
