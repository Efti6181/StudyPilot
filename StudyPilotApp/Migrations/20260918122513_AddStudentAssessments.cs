using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Courses_Id_ApplicationUserId",
                table: "Courses",
                columns: new[] { "Id", "ApplicationUserId" });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ObtainedMarks = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    WeightPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    EstimatedStudyHours = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessments_Courses_CourseId_ApplicationUserId",
                        columns: x => new { x.CourseId, x.ApplicationUserId },
                        principalTable: "Courses",
                        principalColumns: new[] { "Id", "ApplicationUserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ApplicationUserId",
                table: "Assessments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ApplicationUserId_DueDate",
                table: "Assessments",
                columns: new[] { "ApplicationUserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_ApplicationUserId_Status",
                table: "Assessments",
                columns: new[] { "ApplicationUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CourseId_ApplicationUserId",
                table: "Assessments",
                columns: new[] { "CourseId", "ApplicationUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Courses_Id_ApplicationUserId",
                table: "Courses");
        }
    }
}
