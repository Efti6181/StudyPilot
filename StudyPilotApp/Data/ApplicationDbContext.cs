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
    }
}
