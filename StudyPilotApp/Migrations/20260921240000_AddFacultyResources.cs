using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921240000_AddFacultyResources")]
public partial class AddFacultyResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The earlier faculty-assessment hotfix may already have removed this redundant index.
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Assessments_FacultyAssessmentId\";");

        migrationBuilder.CreateTable(
            name: "FacultyResources",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FacultyCourseAssignmentId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                Kind = table.Column<int>(type: "integer", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ExternalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                StoredFileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FacultyResources", x => x.Id);
                table.ForeignKey(
                    name: "FK_FacultyResources_FacultyCourseAssignments_FacultyCourseAssignmentId",
                    column: x => x.FacultyCourseAssignmentId,
                    principalTable: "FacultyCourseAssignments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AlterColumn<string>(
            name: "Description", table: "StudyResources", type: "character varying(3000)",
            maxLength: 3000, nullable: true, oldClrType: typeof(string),
            oldType: "character varying(2000)", oldMaxLength: 2000, oldNullable: true);
        migrationBuilder.AddColumn<int>(
            name: "FacultyResourceId", table: "StudyResources", type: "integer", nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_FacultyResources_FacultyCourseAssignmentId_Status_CreatedAt",
            table: "FacultyResources",
            columns: new[] { "FacultyCourseAssignmentId", "Status", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_StudyResources_FacultyResourceId_CourseId",
            table: "StudyResources",
            columns: new[] { "FacultyResourceId", "CourseId" },
            unique: true,
            filter: "\"FacultyResourceId\" IS NOT NULL");
        migrationBuilder.AddForeignKey(
            name: "FK_StudyResources_FacultyResources_FacultyResourceId",
            table: "StudyResources", column: "FacultyResourceId",
            principalTable: "FacultyResources", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_StudyResources_FacultyResources_FacultyResourceId",
            table: "StudyResources");
        migrationBuilder.DropIndex(
            name: "IX_StudyResources_FacultyResourceId_CourseId",
            table: "StudyResources");
        migrationBuilder.DropColumn(name: "FacultyResourceId", table: "StudyResources");
        migrationBuilder.AlterColumn<string>(
            name: "Description", table: "StudyResources", type: "character varying(2000)",
            maxLength: 2000, nullable: true, oldClrType: typeof(string),
            oldType: "character varying(3000)", oldMaxLength: 3000, oldNullable: true);
        migrationBuilder.DropTable(name: "FacultyResources");
    }
}
