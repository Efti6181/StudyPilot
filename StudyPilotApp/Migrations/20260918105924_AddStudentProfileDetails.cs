using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentProfileDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Batch",
                table: "StudentProfiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "StudentProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "StudentProfiles",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "StudentProfiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "StudentProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "StudentProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "StudentProfiles",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianPhoneNumber",
                table: "StudentProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "StudentProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentAddress",
                table: "StudentProfiles",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageContentType",
                table: "StudentProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfileImageData",
                table: "StudentProfiles",
                type: "bytea",
                maxLength: 2097152,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Semester",
                table: "StudentProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "StudentProfiles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Batch",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "GuardianPhoneNumber",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "PresentAddress",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileImageContentType",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileImageData",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudentProfiles");
        }
    }
}
