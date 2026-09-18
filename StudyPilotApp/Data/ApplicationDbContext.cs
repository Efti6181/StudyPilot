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
    }
}
