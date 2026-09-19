using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260919173000_AddSmartStudyPlans")]
public partial class AddSmartStudyPlans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SmartStudyPlans",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Semester = table.Column<int>(type: "integer", nullable: false),
                AcademicTerm = table.Column<int>(type: "integer", nullable: false),
                AcademicYear = table.Column<int>(type: "integer", nullable: false),
                CareerGoal = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                StudyDays = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PreferredStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                PreferredEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                WeeklyStudyHours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                SessionMinutes = table.Column<int>(type: "integer", nullable: false),
                BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                UsedAiAnalysis = table.Column<bool>(type: "boolean", nullable: false),
                AnalysisProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SmartStudyPlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_SmartStudyPlans_AspNetUsers_ApplicationUserId",
                    column: x => x.ApplicationUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SmartStudyPlanCourses",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SmartStudyPlanId = table.Column<int>(type: "integer", nullable: false),
                CourseId = table.Column<int>(type: "integer", nullable: false),
                CourseCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CourseName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Difficulty = table.Column<int>(type: "integer", nullable: false),
                CareerRelevance = table.Column<int>(type: "integer", nullable: false),
                AnalysisReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                WeeklyMinutes = table.Column<int>(type: "integer", nullable: false),
                AllocationScore = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SmartStudyPlanCourses", x => x.Id);
                table.ForeignKey(
                    name: "FK_SmartStudyPlanCourses_SmartStudyPlans_SmartStudyPlanId",
                    column: x => x.SmartStudyPlanId,
                    principalTable: "SmartStudyPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SmartStudySessions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SmartStudyPlanCourseId = table.Column<int>(type: "integer", nullable: false),
                Day = table.Column<int>(type: "integer", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                Sequence = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SmartStudySessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SmartStudySessions_SmartStudyPlanCourses_SmartStudyPlanCourseId",
                    column: x => x.SmartStudyPlanCourseId,
                    principalTable: "SmartStudyPlanCourses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SmartStudyPlans_ApplicationUserId",
            table: "SmartStudyPlans",
            column: "ApplicationUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SmartStudyPlans_ApplicationUserId_IsActive_CreatedAt",
            table: "SmartStudyPlans",
            columns: new[] { "ApplicationUserId", "IsActive", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_SmartStudyPlanCourses_SmartStudyPlanId",
            table: "SmartStudyPlanCourses",
            column: "SmartStudyPlanId");

        migrationBuilder.CreateIndex(
            name: "IX_SmartStudyPlanCourses_SmartStudyPlanId_CourseId",
            table: "SmartStudyPlanCourses",
            columns: new[] { "SmartStudyPlanId", "CourseId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SmartStudySessions_Day_StartTime",
            table: "SmartStudySessions",
            columns: new[] { "Day", "StartTime" });

        migrationBuilder.CreateIndex(
            name: "IX_SmartStudySessions_SmartStudyPlanCourseId",
            table: "SmartStudySessions",
            column: "SmartStudyPlanCourseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SmartStudySessions");
        migrationBuilder.DropTable(name: "SmartStudyPlanCourses");
        migrationBuilder.DropTable(name: "SmartStudyPlans");
    }
}
