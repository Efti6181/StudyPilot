using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921243000_AddFacultyAnnouncements")]
public partial class AddFacultyAnnouncements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FacultyAnnouncements",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FacultyCourseAssignmentId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Summary = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FacultyAnnouncements", x => x.Id);
                table.ForeignKey(name: "FK_FacultyAnnouncements_FacultyCourseAssignments_FacultyCourseAssignmentId", column: x => x.FacultyCourseAssignmentId, principalTable: "FacultyCourseAssignments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "FacultyAnnouncementRecipients",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FacultyAnnouncementId = table.Column<int>(type: "integer", nullable: false),
                CourseId = table.Column<int>(type: "integer", nullable: false),
                ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                ReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FacultyAnnouncementRecipients", x => x.Id);
                table.ForeignKey(name: "FK_FacultyAnnouncementRecipients_AspNetUsers_ApplicationUserId", column: x => x.ApplicationUserId, principalTable: "AspNetUsers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_FacultyAnnouncementRecipients_Courses_CourseId_ApplicationUserId", columns: x => new { x.CourseId, x.ApplicationUserId }, principalTable: "Courses", principalColumns: new[] { "Id", "ApplicationUserId" }, onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_FacultyAnnouncementRecipients_FacultyAnnouncements_FacultyAnnouncementId", column: x => x.FacultyAnnouncementId, principalTable: "FacultyAnnouncements", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_FacultyAnnouncements_FacultyCourseAssignmentId_Status_CreatedAt", table: "FacultyAnnouncements", columns: new[] { "FacultyCourseAssignmentId", "Status", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_FacultyAnnouncementRecipients_ApplicationUserId_IsRead_DeliveredAt", table: "FacultyAnnouncementRecipients", columns: new[] { "ApplicationUserId", "IsRead", "DeliveredAt" });
        migrationBuilder.CreateIndex(name: "IX_FacultyAnnouncementRecipients_CourseId_ApplicationUserId", table: "FacultyAnnouncementRecipients", columns: new[] { "CourseId", "ApplicationUserId" });
        migrationBuilder.CreateIndex(name: "IX_FacultyAnnouncementRecipients_FacultyAnnouncementId_CourseId", table: "FacultyAnnouncementRecipients", columns: new[] { "FacultyAnnouncementId", "CourseId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FacultyAnnouncementRecipients");
        migrationBuilder.DropTable(name: "FacultyAnnouncements");
    }
}
