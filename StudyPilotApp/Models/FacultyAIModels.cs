using System.ComponentModel.DataAnnotations;

namespace StudyPilotApp.Models;

public enum FacultyAIMode
{
    [Display(Name = "Ask Faculty AI")]
    Ask = 0,

    [Display(Name = "Lesson Planner")]
    LessonPlan = 1,

    [Display(Name = "Assessment Designer")]
    AssessmentDesigner = 2,

    [Display(Name = "Student Support")]
    StudentSupport = 3,

    [Display(Name = "Course Review")]
    CourseReview = 4
}
