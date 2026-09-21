using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920200000_AddAcademicPeriods")]
public partial class AddAcademicPeriods : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AcademicPeriods",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Term = table.Column<int>(type: "integer", nullable: false),
                AcademicYear = table.Column<int>(type: "integer", nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                RegistrationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                RegistrationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsCurrent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AcademicPeriods", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AcademicPeriods_IsActive_StartDate_EndDate",
            table: "AcademicPeriods",
            columns: new[] { "IsActive", "StartDate", "EndDate" });

        migrationBuilder.CreateIndex(
            name: "IX_AcademicPeriods_IsCurrent",
            table: "AcademicPeriods",
            column: "IsCurrent",
            unique: true,
            filter: "\"IsCurrent\" = TRUE");

        migrationBuilder.CreateIndex(
            name: "IX_AcademicPeriods_Term_AcademicYear",
            table: "AcademicPeriods",
            columns: new[] { "Term", "AcademicYear" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AcademicPeriods");
    }
}
