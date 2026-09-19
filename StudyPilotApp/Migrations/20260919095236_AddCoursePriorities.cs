using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursePriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoursePriorityPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceRating = table.Column<int>(type: "integer", nullable: false),
                    TopicCompletionPercentage = table.Column<int>(type: "integer", nullable: false),
                    WorkloadRisk = table.Column<int>(type: "integer", nullable: false),
                    AvailableStudyHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ManualPriorityLevel = table.Column<int>(type: "integer", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePriorityPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoursePriorityPreferences_Courses_CourseId_ApplicationUserId",
                        columns: x => new { x.CourseId, x.ApplicationUserId },
                        principalTable: "Courses",
                        principalColumns: new[] { "Id", "ApplicationUserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriorityWeightSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    TargetGradeGapWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AssessmentUrgencyWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CourseCreditWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    WeaknessWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IncompleteTopicsWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    WorkloadRiskWeight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriorityWeightSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriorityWeightSettings_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoursePriorityPreferences_ApplicationUserId",
                table: "CoursePriorityPreferences",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePriorityPreferences_ApplicationUserId_CourseId",
                table: "CoursePriorityPreferences",
                columns: new[] { "ApplicationUserId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePriorityPreferences_CourseId_ApplicationUserId",
                table: "CoursePriorityPreferences",
                columns: new[] { "CourseId", "ApplicationUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriorityWeightSettings_ApplicationUserId",
                table: "PriorityWeightSettings",
                column: "ApplicationUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoursePriorityPreferences");

            migrationBuilder.DropTable(
                name: "PriorityWeightSettings");
        }
    }
}
