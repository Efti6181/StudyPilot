using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudyPilotApp.Models;

namespace StudyPilotApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UniversityMember> UniversityMembers => Set<UniversityMember>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<FacultyProfile> FacultyProfiles => Set<FacultyProfile>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<SemesterResult> SemesterResults => Set<SemesterResult>();
    public DbSet<CourseGrade> CourseGrades => Set<CourseGrade>();
    public DbSet<GradingScaleEntry> GradingScaleEntries => Set<GradingScaleEntry>();
    public DbSet<CoursePriorityPreference> CoursePriorityPreferences => Set<CoursePriorityPreference>();
    public DbSet<PriorityWeightSettings> PriorityWeightSettings => Set<PriorityWeightSettings>();
    public DbSet<AcademicProgressSnapshot> AcademicProgressSnapshots => Set<AcademicProgressSnapshot>();
    public DbSet<StudyResource> StudyResources => Set<StudyResource>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<CommunityComment> CommunityComments => Set<CommunityComment>();
    public DbSet<CommunityPostLike> CommunityPostLikes => Set<CommunityPostLike>();
    public DbSet<CommunityCommentLike> CommunityCommentLikes => Set<CommunityCommentLike>();
    public DbSet<CampusEvent> CampusEvents => Set<CampusEvent>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<SavedEvent> SavedEvents => Set<SavedEvent>();
    public DbSet<AppNotification> AppNotifications => Set<AppNotification>();
    public DbSet<AcademicAIConversation> AcademicAIConversations => Set<AcademicAIConversation>();
    public DbSet<AcademicAIMessage> AcademicAIMessages => Set<AcademicAIMessage>();
    public DbSet<SmartStudyPlan> SmartStudyPlans => Set<SmartStudyPlan>();
    public DbSet<SmartStudyPlanCourse> SmartStudyPlanCourses => Set<SmartStudyPlanCourse>();
    public DbSet<SmartStudySession> SmartStudySessions => Set<SmartStudySession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UniversityMember>(entity =>
        {
            entity.HasIndex(x => x.UniversityId).IsUnique();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.IsClaimed).HasDefaultValue(false);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentProfile>(entity =>
        {
            entity.HasIndex(x => x.StudentId).IsUnique();
            entity.HasIndex(x => x.ApplicationUserId).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithOne()
                .HasForeignKey<StudentProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<FacultyProfile>(entity =>
        {
            entity.HasIndex(x => x.FacultyId).IsUnique();
            entity.HasIndex(x => x.ApplicationUserId).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithOne()
                .HasForeignKey<FacultyProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Course>(entity =>
        {
            entity.HasAlternateKey(x => new { x.Id, x.ApplicationUserId });
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.Status });
            entity.HasIndex(x => new
            {
                x.ApplicationUserId,
                x.CourseCode,
                x.Semester,
                x.AcademicTerm,
                x.AcademicYear
            }).IsUnique();

            entity.Property(x => x.CreditHours).HasPrecision(4, 1);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Assessment>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.DueDate });
            entity.HasIndex(x => new { x.ApplicationUserId, x.Status });

            entity.Property(x => x.DueDate).HasColumnType("timestamp without time zone");
            entity.Property(x => x.TotalMarks).HasPrecision(10, 2);
            entity.Property(x => x.ObtainedMarks).HasPrecision(10, 2);
            entity.Property(x => x.WeightPercentage).HasPrecision(5, 2);
            entity.Property(x => x.EstimatedStudyHours).HasPrecision(7, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Assessments)
                .HasForeignKey(x => new { x.CourseId, x.ApplicationUserId })
                .HasPrincipalKey(x => new { x.Id, x.ApplicationUserId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SemesterResult>(entity =>
        {
            entity.HasAlternateKey(x => new { x.Id, x.ApplicationUserId });
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new
            {
                x.ApplicationUserId,
                x.SemesterNumber,
                x.AcademicTerm,
                x.AcademicYear
            }).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CourseGrade>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.SemesterResultId, x.CourseId }).IsUnique();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.SemesterResult)
                .WithMany(x => x.CourseGrades)
                .HasForeignKey(x => new { x.SemesterResultId, x.ApplicationUserId })
                .HasPrincipalKey(x => new { x.Id, x.ApplicationUserId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany(x => x.CourseGrades)
                .HasForeignKey(x => new { x.CourseId, x.ApplicationUserId })
                .HasPrincipalKey(x => new { x.Id, x.ApplicationUserId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GradingScaleEntry>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.LetterGrade }).IsUnique();
            entity.Property(x => x.MinimumPercentage).HasPrecision(5, 2);
            entity.Property(x => x.GradePoint).HasPrecision(4, 2);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CoursePriorityPreference>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.CourseId }).IsUnique();
            entity.Property(x => x.AvailableStudyHoursPerWeek).HasPrecision(5, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.Course)
                .WithOne(x => x.PriorityPreference)
                .HasForeignKey<CoursePriorityPreference>(x => new { x.CourseId, x.ApplicationUserId })
                .HasPrincipalKey<Course>(x => new { x.Id, x.ApplicationUserId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PriorityWeightSettings>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId).IsUnique();
            entity.Property(x => x.TargetGradeGapWeight).HasPrecision(5, 2);
            entity.Property(x => x.AssessmentUrgencyWeight).HasPrecision(5, 2);
            entity.Property(x => x.CourseCreditWeight).HasPrecision(5, 2);
            entity.Property(x => x.WeaknessWeight).HasPrecision(5, 2);
            entity.Property(x => x.IncompleteTopicsWeight).HasPrecision(5, 2);
            entity.Property(x => x.WorkloadRiskWeight).HasPrecision(5, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithOne()
                .HasForeignKey<PriorityWeightSettings>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AcademicProgressSnapshot>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.SnapshotDate }).IsUnique();
            entity.Property(x => x.AverageCourseProgress).HasPrecision(5, 2);
            entity.Property(x => x.AssessmentCompletionRate).HasPrecision(5, 2);
            entity.Property(x => x.AverageAssessmentScore).HasPrecision(5, 2);
            entity.Property(x => x.CurrentCgpa).HasPrecision(4, 2);
            entity.Property(x => x.CompletedCredits).HasPrecision(8, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StudyResource>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.Category });
            entity.HasIndex(x => new { x.ApplicationUserId, x.IsFavorite });
            entity.HasIndex(x => new { x.ApplicationUserId, x.CreatedAt });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Resources)
                .HasForeignKey(x => new { x.CourseId, x.ApplicationUserId })
                .HasPrincipalKey(x => new { x.Id, x.ApplicationUserId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CommunityPost>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.Category, x.CreatedAt });
            entity.HasIndex(x => x.CreatedAt);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommunityComment>(entity =>
        {
            entity.HasIndex(x => x.CommunityPostId);
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.CommunityPostId, x.CreatedAt });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.CommunityPost)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.CommunityPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommunityPostLike>(entity =>
        {
            entity.HasKey(x => new { x.CommunityPostId, x.ApplicationUserId });
            entity.HasIndex(x => x.ApplicationUserId);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.CommunityPost)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.CommunityPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommunityCommentLike>(entity =>
        {
            entity.HasKey(x => new { x.CommunityCommentId, x.ApplicationUserId });
            entity.HasIndex(x => x.ApplicationUserId);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.CommunityComment)
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.CommunityCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CampusEvent>(entity =>
        {
            entity.HasIndex(x => x.CreatedByUserId);
            entity.HasIndex(x => new { x.IsPublished, x.StartAt });
            entity.HasIndex(x => new { x.Type, x.StartAt });
            entity.HasIndex(x => new { x.LocationType, x.StartAt });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<EventRegistration>(entity =>
        {
            entity.HasKey(x => new { x.CampusEventId, x.ApplicationUserId });
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.CancelledAt, x.RegisteredAt });
            entity.Property(x => x.RegisteredAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.CampusEvent)
                .WithMany(x => x.Registrations)
                .HasForeignKey(x => x.CampusEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SavedEvent>(entity =>
        {
            entity.HasKey(x => new { x.CampusEventId, x.ApplicationUserId });
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.SavedAt });
            entity.Property(x => x.SavedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.CampusEvent)
                .WithMany(x => x.SavedByStudents)
                .HasForeignKey(x => x.CampusEventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AppNotification>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.IsRead, x.CreatedAt });
            entity.HasIndex(x => new { x.ApplicationUserId, x.Type, x.CreatedAt });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AcademicAIConversation>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.UpdatedAt });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AcademicAIMessage>(entity =>
        {
            entity.HasIndex(x => x.ConversationId);
            entity.HasIndex(x => new { x.ConversationId, x.CreatedAt });
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SmartStudyPlan>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId);
            entity.HasIndex(x => new { x.ApplicationUserId, x.IsActive, x.CreatedAt });
            entity.Property(x => x.WeeklyStudyHours).HasPrecision(5, 2);
            entity.Property(x => x.PreferredStartTime).HasColumnType("time without time zone");
            entity.Property(x => x.PreferredEndTime).HasColumnType("time without time zone");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(x => x.ApplicationUser)
                .WithMany()
                .HasForeignKey(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SmartStudyPlanCourse>(entity =>
        {
            entity.HasIndex(x => x.SmartStudyPlanId);
            entity.HasIndex(x => new { x.SmartStudyPlanId, x.CourseId }).IsUnique();
            entity.Property(x => x.AllocationScore).HasPrecision(8, 3);

            entity.HasOne(x => x.SmartStudyPlan)
                .WithMany(x => x.Courses)
                .HasForeignKey(x => x.SmartStudyPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SmartStudySession>(entity =>
        {
            entity.HasIndex(x => x.SmartStudyPlanCourseId);
            entity.HasIndex(x => new { x.Day, x.StartTime });
            entity.Property(x => x.StartTime).HasColumnType("time without time zone");
            entity.Property(x => x.EndTime).HasColumnType("time without time zone");

            entity.HasOne(x => x.SmartStudyPlanCourse)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.SmartStudyPlanCourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
