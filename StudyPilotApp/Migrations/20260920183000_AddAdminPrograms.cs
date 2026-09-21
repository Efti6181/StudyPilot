using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920183000_AddAdminPrograms")]
public partial class AddAdminPrograms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AcademicPrograms",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                NormalizedCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                DepartmentId = table.Column<int>(type: "integer", nullable: false),
                TotalCredits = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                DurationYears = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AcademicPrograms", x => x.Id);
                table.ForeignKey(
                    name: "FK_AcademicPrograms_Departments_DepartmentId",
                    column: x => x.DepartmentId,
                    principalTable: "Departments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AcademicPrograms_DepartmentId_NormalizedName",
            table: "AcademicPrograms",
            columns: new[] { "DepartmentId", "NormalizedName" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AcademicPrograms_IsActive_Name",
            table: "AcademicPrograms",
            columns: new[] { "IsActive", "Name" });

        migrationBuilder.CreateIndex(
            name: "IX_AcademicPrograms_NormalizedCode",
            table: "AcademicPrograms",
            column: "NormalizedCode",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AcademicPrograms");
    }
}
