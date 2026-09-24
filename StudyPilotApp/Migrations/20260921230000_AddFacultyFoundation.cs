using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921230000_AddFacultyFoundation")]
public partial class AddFacultyFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Bio", table: "FacultyProfiles", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<int>(name: "DepartmentId", table: "FacultyProfiles", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Designation", table: "FacultyProfiles", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "OfficeHours", table: "FacultyProfiles", type: "character varying(250)", maxLength: 250, nullable: true);
        migrationBuilder.AddColumn<string>(name: "OfficeLocation", table: "FacultyProfiles", type: "character varying(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<string>(name: "PhoneNumber", table: "FacultyProfiles", type: "character varying(20)", maxLength: 20, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProfileImageContentType", table: "FacultyProfiles", type: "character varying(50)", maxLength: 50, nullable: true);
        migrationBuilder.AddColumn<byte[]>(name: "ProfileImageData", table: "FacultyProfiles", type: "bytea", maxLength: 2097152, nullable: true);
        migrationBuilder.AddColumn<string>(name: "TeachingInterests", table: "FacultyProfiles", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "UpdatedAt", table: "FacultyProfiles", type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            UPDATE "FacultyProfiles" AS fp
            SET "DepartmentId" = um."DepartmentId"
            FROM "UniversityMembers" AS um
            WHERE um."ApplicationUserId" = fp."ApplicationUserId"
              AND um."Role" = 'Faculty'
              AND fp."DepartmentId" IS NULL;
            """);

        migrationBuilder.CreateIndex(name: "IX_FacultyProfiles_DepartmentId", table: "FacultyProfiles", column: "DepartmentId");
        migrationBuilder.AddForeignKey(
            name: "FK_FacultyProfiles_Departments_DepartmentId", table: "FacultyProfiles",
            column: "DepartmentId", principalTable: "Departments", principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.CreateTable(
            name: "FacultyCourseAssignments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FacultyProfileId = table.Column<int>(type: "integer", nullable: false),
                CatalogCourseId = table.Column<int>(type: "integer", nullable: false),
                AcademicPeriodId = table.Column<int>(type: "integer", nullable: false),
                Section = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                CourseOverview = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FacultyCourseAssignments", x => x.Id);
                table.ForeignKey(name: "FK_FacultyCourseAssignments_AcademicPeriods_AcademicPeriodId", column: x => x.AcademicPeriodId, principalTable: "AcademicPeriods", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_FacultyCourseAssignments_CatalogCourses_CatalogCourseId", column: x => x.CatalogCourseId, principalTable: "CatalogCourses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_FacultyCourseAssignments_FacultyProfiles_FacultyProfileId", column: x => x.FacultyProfileId, principalTable: "FacultyProfiles", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_FacultyCourseAssignments_AcademicPeriodId", table: "FacultyCourseAssignments", column: "AcademicPeriodId");
        migrationBuilder.CreateIndex(name: "IX_FacultyCourseAssignments_CatalogCourseId_AcademicPeriodId", table: "FacultyCourseAssignments", columns: new[] { "CatalogCourseId", "AcademicPeriodId" });
        migrationBuilder.CreateIndex(name: "IX_FacultyCourseAssignments_FacultyProfileId_IsActive", table: "FacultyCourseAssignments", columns: new[] { "FacultyProfileId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_FacultyCourseAssignments_FacultyProfileId_CatalogCourseId_AcademicPeriodId_Section", table: "FacultyCourseAssignments", columns: new[] { "FacultyProfileId", "CatalogCourseId", "AcademicPeriodId", "Section" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FacultyCourseAssignments");
        migrationBuilder.DropForeignKey(name: "FK_FacultyProfiles_Departments_DepartmentId", table: "FacultyProfiles");
        migrationBuilder.DropIndex(name: "IX_FacultyProfiles_DepartmentId", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "Bio", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "DepartmentId", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "Designation", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "OfficeHours", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "OfficeLocation", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "PhoneNumber", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "ProfileImageContentType", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "ProfileImageData", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "TeachingInterests", table: "FacultyProfiles");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "FacultyProfiles");
    }
}
