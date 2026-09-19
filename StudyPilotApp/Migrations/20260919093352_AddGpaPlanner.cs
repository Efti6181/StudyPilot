using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddGpaPlanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradingScaleEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    LetterGrade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MinimumPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    GradePoint = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingScaleEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradingScaleEntries_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SemesterResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    SemesterNumber = table.Column<int>(type: "integer", nullable: false),
                    AcademicTerm = table.Column<int>(type: "integer", nullable: false),
                    AcademicYear = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterResults", x => x.Id);
                    table.UniqueConstraint("AK_SemesterResults_Id_ApplicationUserId", x => new { x.Id, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_SemesterResults_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    SemesterResultId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    ExpectedLetterGrade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ActualLetterGrade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseGrades_Courses_CourseId_ApplicationUserId",
                        columns: x => new { x.CourseId, x.ApplicationUserId },
                        principalTable: "Courses",
                        principalColumns: new[] { "Id", "ApplicationUserId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseGrades_SemesterResults_SemesterResultId_ApplicationUs~",
                        columns: x => new { x.SemesterResultId, x.ApplicationUserId },
                        principalTable: "SemesterResults",
                        principalColumns: new[] { "Id", "ApplicationUserId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_ApplicationUserId",
                table: "CourseGrades",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_CourseId_ApplicationUserId",
                table: "CourseGrades",
                columns: new[] { "CourseId", "ApplicationUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_SemesterResultId_ApplicationUserId",
                table: "CourseGrades",
                columns: new[] { "SemesterResultId", "ApplicationUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_SemesterResultId_CourseId",
                table: "CourseGrades",
                columns: new[] { "SemesterResultId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingScaleEntries_ApplicationUserId",
                table: "GradingScaleEntries",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingScaleEntries_ApplicationUserId_LetterGrade",
                table: "GradingScaleEntries",
                columns: new[] { "ApplicationUserId", "LetterGrade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResults_ApplicationUserId",
                table: "SemesterResults",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SemesterResults_ApplicationUserId_SemesterNumber_AcademicTe~",
                table: "SemesterResults",
                columns: new[] { "ApplicationUserId", "SemesterNumber", "AcademicTerm", "AcademicYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseGrades");

            migrationBuilder.DropTable(
                name: "GradingScaleEntries");

            migrationBuilder.DropTable(
                name: "SemesterResults");
        }
    }
}
