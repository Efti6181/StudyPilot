using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    CourseCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreditHours = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                    Instructor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    AcademicTerm = table.Column<int>(type: "integer", nullable: false),
                    AcademicYear = table.Column<int>(type: "integer", nullable: false),
                    CourseType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetGrade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<int>(type: "integer", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ApplicationUserId",
                table: "Courses",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ApplicationUserId_CourseCode_Semester_AcademicTerm_~",
                table: "Courses",
                columns: new[] { "ApplicationUserId", "CourseCode", "Semester", "AcademicTerm", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ApplicationUserId_Status",
                table: "Courses",
                columns: new[] { "ApplicationUserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Courses");
        }
    }
}
