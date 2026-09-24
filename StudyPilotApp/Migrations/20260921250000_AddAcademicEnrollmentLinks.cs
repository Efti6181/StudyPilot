using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921250000_AddAcademicEnrollmentLinks")]
public partial class AddAcademicEnrollmentLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "CatalogCourseId", table: "Courses", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "FacultyCourseAssignmentId", table: "Courses", type: "integer", nullable: true);

        migrationBuilder.AddColumn<int>(name: "AcademicProgramId", table: "StudentProfiles", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DepartmentId", table: "StudentProfiles", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Program", table: "StudentProfiles", type: "character varying(180)", maxLength: 180, nullable: true);

        migrationBuilder.Sql("""
            WITH department_matches AS (
                SELECT profile."Id" AS "ProfileId", MIN(department."Id") AS "DepartmentId"
                FROM "StudentProfiles" AS profile
                JOIN "Departments" AS department
                  ON UPPER(TRIM(profile."Department")) = department."NormalizedName"
                  OR UPPER(TRIM(profile."Department")) = department."NormalizedCode"
                WHERE profile."DepartmentId" IS NULL AND profile."Department" IS NOT NULL
                GROUP BY profile."Id"
                HAVING COUNT(DISTINCT department."Id") = 1
            )
            UPDATE "StudentProfiles" AS profile
            SET "DepartmentId" = department_matches."DepartmentId"
            FROM department_matches
            WHERE profile."Id" = department_matches."ProfileId";

            UPDATE "Courses" AS course
            SET "CatalogCourseId" = catalog."Id"
            FROM "CatalogCourses" AS catalog
            WHERE course."CatalogCourseId" IS NULL
              AND UPPER(TRIM(course."CourseCode")) = catalog."NormalizedCode";

            WITH unique_matches AS (
                SELECT course."Id" AS "CourseId", MIN(assignment."Id") AS "AssignmentId"
                FROM "Courses" AS course
                JOIN "FacultyCourseAssignments" AS assignment
                  ON assignment."CatalogCourseId" = course."CatalogCourseId"
                 AND assignment."IsActive" = TRUE
                JOIN "AcademicPeriods" AS period
                  ON period."Id" = assignment."AcademicPeriodId"
                 AND period."Term" = course."AcademicTerm"
                 AND period."AcademicYear" = course."AcademicYear"
                WHERE course."CatalogCourseId" IS NOT NULL
                GROUP BY course."Id"
                HAVING COUNT(*) = 1
            )
            UPDATE "Courses" AS course
            SET "FacultyCourseAssignmentId" = unique_matches."AssignmentId"
            FROM unique_matches
            WHERE course."Id" = unique_matches."CourseId";
            """);

        migrationBuilder.CreateIndex(name: "IX_Courses_CatalogCourseId", table: "Courses", column: "CatalogCourseId");
        migrationBuilder.CreateIndex(name: "IX_Courses_FacultyCourseAssignmentId", table: "Courses", column: "FacultyCourseAssignmentId");
        migrationBuilder.CreateIndex(
            name: "IX_Courses_ApplicationUserId_FacultyCourseAssignmentId",
            table: "Courses",
            columns: new[] { "ApplicationUserId", "FacultyCourseAssignmentId" },
            unique: true,
            filter: "\"FacultyCourseAssignmentId\" IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_StudentProfiles_AcademicProgramId", table: "StudentProfiles", column: "AcademicProgramId");
        migrationBuilder.CreateIndex(name: "IX_StudentProfiles_DepartmentId", table: "StudentProfiles", column: "DepartmentId");

        migrationBuilder.AddForeignKey(
            name: "FK_Courses_CatalogCourses_CatalogCourseId",
            table: "Courses",
            column: "CatalogCourseId",
            principalTable: "CatalogCourses",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_Courses_FacultyCourseAssignments_FacultyCourseAssignmentId",
            table: "Courses",
            column: "FacultyCourseAssignmentId",
            principalTable: "FacultyCourseAssignments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_StudentProfiles_AcademicPrograms_AcademicProgramId",
            table: "StudentProfiles",
            column: "AcademicProgramId",
            principalTable: "AcademicPrograms",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_StudentProfiles_Departments_DepartmentId",
            table: "StudentProfiles",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Courses_CatalogCourses_CatalogCourseId", table: "Courses");
        migrationBuilder.DropForeignKey(name: "FK_Courses_FacultyCourseAssignments_FacultyCourseAssignmentId", table: "Courses");
        migrationBuilder.DropForeignKey(name: "FK_StudentProfiles_AcademicPrograms_AcademicProgramId", table: "StudentProfiles");
        migrationBuilder.DropForeignKey(name: "FK_StudentProfiles_Departments_DepartmentId", table: "StudentProfiles");

        migrationBuilder.DropIndex(name: "IX_Courses_ApplicationUserId_FacultyCourseAssignmentId", table: "Courses");
        migrationBuilder.DropIndex(name: "IX_Courses_CatalogCourseId", table: "Courses");
        migrationBuilder.DropIndex(name: "IX_Courses_FacultyCourseAssignmentId", table: "Courses");
        migrationBuilder.DropIndex(name: "IX_StudentProfiles_AcademicProgramId", table: "StudentProfiles");
        migrationBuilder.DropIndex(name: "IX_StudentProfiles_DepartmentId", table: "StudentProfiles");

        migrationBuilder.DropColumn(name: "CatalogCourseId", table: "Courses");
        migrationBuilder.DropColumn(name: "FacultyCourseAssignmentId", table: "Courses");
        migrationBuilder.DropColumn(name: "AcademicProgramId", table: "StudentProfiles");
        migrationBuilder.DropColumn(name: "DepartmentId", table: "StudentProfiles");
        migrationBuilder.DropColumn(name: "Program", table: "StudentProfiles");
    }
}
