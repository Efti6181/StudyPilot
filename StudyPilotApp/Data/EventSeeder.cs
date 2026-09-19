using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Models;

namespace StudyPilotApp.Data;

public static class EventSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.CampusEvents.AnyAsync()) return;

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var admin = admins.FirstOrDefault();
        if (admin is null) return;

        var now = DateTimeOffset.UtcNow;

        DateTimeOffset AtUtc(int daysFromNow, int hour)
        {
            var dateTime = now.UtcDateTime.Date
                .AddDays(daysFromNow)
                .AddHours(hour);

            return new DateTimeOffset(dateTime, TimeSpan.Zero);
        }

        var events = new[]
        {
            Create(admin.Id, "Future Tech & AI Workshop", "Build practical AI skills with guided hands-on exercises.",
                "A practical workshop covering responsible AI tools, prompt design, workflow automation and a guided mini project. Bring your laptop and university ID.",
                CampusEventType.Workshop, EventLocationType.OnCampus, "Innovation Lab, Building B", null,
                "CSE Department", AtUtc(7, 4), AtUtc(7, 7), now.AddDays(5), 80),
            Create(admin.Id, "Career Launch Seminar", "Prepare your CV, portfolio and interview strategy for software roles.",
                "Industry speakers will explain internship preparation, technical interviews, portfolio presentation and professional networking for CSE students.",
                CampusEventType.Career, EventLocationType.Hybrid, "University Auditorium", "https://meet.google.com/",
                "Career Development Center", AtUtc(12, 5), AtUtc(12, 7), now.AddDays(10), 250),
            Create(admin.Id, "Inter-University Programming Contest", "Compete in teams and solve algorithmic problems under contest conditions.",
                "A three-hour programming contest for undergraduate teams. Registration is individual for this StudyPilot demo; official team confirmation will be handled by the organizer.",
                CampusEventType.Competition, EventLocationType.OnCampus, "Computer Labs 301–304", null,
                "PUC Programming Club", AtUtc(18, 3), AtUtc(18, 7), now.AddDays(14), 120),
            Create(admin.Id, "Cybersecurity Awareness Webinar", "Learn practical defenses against phishing, account theft and unsafe browsing.",
                "This online session covers password hygiene, multi-factor authentication, social engineering, malicious extensions and safe incident reporting.",
                CampusEventType.Seminar, EventLocationType.Online, null, "https://meet.google.com/",
                "Cybersecurity Society", AtUtc(4, 8), AtUtc(4, 10), now.AddDays(3), null),
            Create(admin.Id, "University Cultural Evening", "Celebrate student music, theatre, art and cultural performances.",
                "An evening of student performances and exhibitions. Registered students receive priority entry while seats remain available.",
                CampusEventType.Cultural, EventLocationType.OnCampus, "Central Campus Stage", null,
                "Student Affairs Office", AtUtc(25, 11), AtUtc(25, 15), now.AddDays(21), 500),
            Create(admin.Id, "Research Paper Reading Club", "Discuss one accessible computing research paper with peers each month.",
                "The first session focuses on how to read a research paper, identify its contribution and evaluate its evidence. The selected paper will be emailed by the organizer.",
                CampusEventType.Club, EventLocationType.Hybrid, "Library Seminar Room", "https://meet.google.com/",
                "Research & Innovation Club", AtUtc(9, 6), AtUtc(9, 8), now.AddDays(8), 40)
        };

        await dbContext.CampusEvents.AddRangeAsync(events);
        await dbContext.SaveChangesAsync();
    }

    private static CampusEvent Create(
        string adminId,
        string title,
        string summary,
        string description,
        CampusEventType type,
        EventLocationType locationType,
        string? venue,
        string? onlineUrl,
        string organizer,
        DateTimeOffset start,
        DateTimeOffset end,
        DateTimeOffset deadline,
        int? capacity) => new()
    {
        CreatedByUserId = adminId,
        Title = title,
        ShortDescription = summary,
        Description = description,
        Type = type,
        LocationType = locationType,
        Venue = venue,
        OnlineUrl = onlineUrl,
        OrganizerName = organizer,
        StartAt = start,
        EndAt = end,
        RegistrationDeadline = deadline,
        Capacity = capacity,
        IsPublished = true
    };
}
