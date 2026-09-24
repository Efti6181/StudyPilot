using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921233000_AddFacultyAssessments")]
public partial class AddFacultyAssessments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FacultyAssessments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FacultyCourseAssignmentId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                Instructions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                TotalMarks = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                WeightPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                Difficulty = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                AttachmentFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                AttachmentContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AttachmentData = table.Column<byte[]>(type: "bytea", maxLength: 5242880, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FacultyAssessments", x => x.Id);
                table.ForeignKey(name: "FK_FacultyAssessments_FacultyCourseAssignments_FacultyCourseAssignmentId", column: x => x.FacultyCourseAssignmentId, principalTable: "FacultyCourseAssignments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<int>(name: "FacultyAssessmentId", table: "Assessments", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Instructions", table: "Assessments", type: "character varying(4000)", maxLength: 4000, nullable: true);
        migrationBuilder.AlterColumn<string>(name: "Description", table: "Assessments", type: "character varying(4000)", maxLength: 4000, nullable: true, oldClrType: typeof(string), oldType: "character varying(2000)", oldMaxLength: 2000, oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_FacultyAssessments_FacultyCourseAssignmentId_Status_DueDate", table: "FacultyAssessments", columns: new[] { "FacultyCourseAssignmentId", "Status", "DueDate" });
        migrationBuilder.CreateIndex(name: "IX_Assessments_FacultyAssessmentId", table: "Assessments", column: "FacultyAssessmentId");
        migrationBuilder.CreateIndex(name: "IX_Assessments_FacultyAssessmentId_CourseId", table: "Assessments", columns: new[] { "FacultyAssessmentId", "CourseId" }, unique: true, filter: "\"FacultyAssessmentId\" IS NOT NULL");
        migrationBuilder.AddForeignKey(name: "FK_Assessments_FacultyAssessments_FacultyAssessmentId", table: "Assessments", column: "FacultyAssessmentId", principalTable: "FacultyAssessments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Assessments_FacultyAssessments_FacultyAssessmentId", table: "Assessments");
        migrationBuilder.DropIndex(name: "IX_Assessments_FacultyAssessmentId", table: "Assessments");
        migrationBuilder.DropIndex(name: "IX_Assessments_FacultyAssessmentId_CourseId", table: "Assessments");
        migrationBuilder.DropColumn(name: "FacultyAssessmentId", table: "Assessments");
        migrationBuilder.DropColumn(name: "Instructions", table: "Assessments");
        migrationBuilder.AlterColumn<string>(name: "Description", table: "Assessments", type: "character varying(2000)", maxLength: 2000, nullable: true, oldClrType: typeof(string), oldType: "character varying(4000)", oldMaxLength: 4000, oldNullable: true);
        migrationBuilder.DropTable(name: "FacultyAssessments");
    }
}
