using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920193000_AddUserAccountStatus")]
public partial class AddUserAccountStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "AccountStatusChangedAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeactivatedAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_IsActive_CreatedAt",
            table: "AspNetUsers",
            columns: new[] { "IsActive", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_IsActive_CreatedAt",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(name: "AccountStatusChangedAt", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "DeactivatedAt", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "IsActive", table: "AspNetUsers");
    }
}
