using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920190000_ExtendRegistrationAuthorizations")]
public partial class ExtendRegistrationAuthorizations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Batch", table: "UniversityMembers", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<string>(name: "CreatedByAdminId", table: "UniversityMembers", type: "text", nullable: true);
        migrationBuilder.AddColumn<int>(name: "CurrentSemester", table: "UniversityMembers", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "DepartmentId", table: "UniversityMembers", type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(name: "FullName", table: "UniversityMembers", type: "character varying(150)", maxLength: 150, nullable: true);
        migrationBuilder.AddColumn<int>(name: "ProgramId", table: "UniversityMembers", type: "integer", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "RegisteredAt", table: "UniversityMembers", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "UpdatedAt", table: "UniversityMembers", type: "timestamp with time zone", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_UniversityMembers_CreatedByAdminId", table: "UniversityMembers", column: "CreatedByAdminId");
        migrationBuilder.CreateIndex(name: "IX_UniversityMembers_DepartmentId", table: "UniversityMembers", column: "DepartmentId");
        migrationBuilder.CreateIndex(name: "IX_UniversityMembers_ProgramId", table: "UniversityMembers", column: "ProgramId");
        migrationBuilder.CreateIndex(
            name: "IX_UniversityMembers_Role_IsClaimed_IsActive",
            table: "UniversityMembers",
            columns: new[] { "Role", "IsClaimed", "IsActive" });

        migrationBuilder.AddForeignKey(
            name: "FK_UniversityMembers_AcademicPrograms_ProgramId",
            table: "UniversityMembers",
            column: "ProgramId",
            principalTable: "AcademicPrograms",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_UniversityMembers_AspNetUsers_CreatedByAdminId",
            table: "UniversityMembers",
            column: "CreatedByAdminId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_UniversityMembers_Departments_DepartmentId",
            table: "UniversityMembers",
            column: "DepartmentId",
            principalTable: "Departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_UniversityMembers_AcademicPrograms_ProgramId", table: "UniversityMembers");
        migrationBuilder.DropForeignKey(name: "FK_UniversityMembers_AspNetUsers_CreatedByAdminId", table: "UniversityMembers");
        migrationBuilder.DropForeignKey(name: "FK_UniversityMembers_Departments_DepartmentId", table: "UniversityMembers");
        migrationBuilder.DropIndex(name: "IX_UniversityMembers_CreatedByAdminId", table: "UniversityMembers");
        migrationBuilder.DropIndex(name: "IX_UniversityMembers_DepartmentId", table: "UniversityMembers");
        migrationBuilder.DropIndex(name: "IX_UniversityMembers_ProgramId", table: "UniversityMembers");
        migrationBuilder.DropIndex(name: "IX_UniversityMembers_Role_IsClaimed_IsActive", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "Batch", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "CreatedByAdminId", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "CurrentSemester", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "DepartmentId", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "FullName", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "ProgramId", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "RegisteredAt", table: "UniversityMembers");
        migrationBuilder.DropColumn(name: "UpdatedAt", table: "UniversityMembers");
    }
}
