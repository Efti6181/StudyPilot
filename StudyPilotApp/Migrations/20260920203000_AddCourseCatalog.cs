using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920203000_AddCourseCatalog")]
public partial class AddCourseCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CatalogCourses",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                NormalizedCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                DepartmentId = table.Column<int>(type: "integer", nullable: false),
                ProgramId = table.Column<int>(type: "integer", nullable: true),
                CreditHours = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: false),
                CourseType = table.Column<int>(type: "integer", nullable: false),
                RecommendedSemester = table.Column<int>(type: "integer", nullable: true),
                Difficulty = table.Column<int>(type: "integer", nullable: false),
                CareerRelevance = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                Prerequisites = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CatalogCourses", x => x.Id);
                table.ForeignKey(
                    name: "FK_CatalogCourses_AcademicPrograms_ProgramId",
                    column: x => x.ProgramId,
                    principalTable: "AcademicPrograms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CatalogCourses_Departments_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "Departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CatalogCourses_DepartmentId_IsActive_Code",
            table: "CatalogCourses",
            columns: new[] { "DepartmentId", "IsActive", "Code" });
        migrationBuilder.CreateIndex(
            name: "IX_CatalogCourses_Difficulty_CareerRelevance",
            table: "CatalogCourses",
            columns: new[] { "Difficulty", "CareerRelevance" });
        migrationBuilder.CreateIndex(
            name: "IX_CatalogCourses_NormalizedCode",
            table: "CatalogCourses",
            column: "NormalizedCode",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_CatalogCourses_ProgramId",
            table: "CatalogCourses",
            column: "ProgramId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CatalogCourses");
    }
}
