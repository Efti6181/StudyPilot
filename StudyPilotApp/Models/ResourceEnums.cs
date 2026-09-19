using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum ResourceKind
{
    File = 1,
    Link = 2
}

public enum ResourceCategory
{
    Notes = 1,
    [Display(Name = "PDF / Document")]
    Document = 2,
    Slides = 3,
    Book = 4,
    Website = 5,
    Video = 6,
    [Display(Name = "Past Question")]
    PastQuestion = 7,
    Tutorial = 8,
    Code = 9,
    Other = 10
}
