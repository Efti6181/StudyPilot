using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260921220000_AddAdminOperations")]
public partial class AddAdminOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AdminAuditLogs",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AdminUserId = table.Column<string>(type: "text", nullable: false),
                AdminName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Controller = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                RequestPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Succeeded = table.Column<bool>(type: "boolean", nullable: false),
                StatusCode = table.Column<int>(type: "integer", nullable: false),
                Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdminAuditLogs_AspNetUsers_AdminUserId",
                    column: x => x.AdminUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PlatformSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                InstitutionName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                SupportEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                DateFormat = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                DefaultPageSize = table.Column<int>(type: "integer", nullable: false),
                AuditRetentionDays = table.Column<int>(type: "integer", nullable: false),
                StudentRegistrationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                FacultyRegistrationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AcademicAiEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CommunityEnabled = table.Column<bool>(type: "boolean", nullable: false),
                EventsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                MaintenanceNoticeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                MaintenanceNotice = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                table.ForeignKey(
                    name: "FK_PlatformSettings_AspNetUsers_UpdatedByUserId",
                    column: x => x.UpdatedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_AdminAuditLogs_AdminUserId_CreatedAt", table: "AdminAuditLogs", columns: new[] { "AdminUserId", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_AdminAuditLogs_Controller_CreatedAt", table: "AdminAuditLogs", columns: new[] { "Controller", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_AdminAuditLogs_CreatedAt", table: "AdminAuditLogs", column: "CreatedAt");
        migrationBuilder.CreateIndex(name: "IX_AdminAuditLogs_Succeeded_CreatedAt", table: "AdminAuditLogs", columns: new[] { "Succeeded", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_PlatformSettings_UpdatedByUserId", table: "PlatformSettings", column: "UpdatedByUserId");

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminAuditLogs");
        migrationBuilder.DropTable(name: "PlatformSettings");
    }
}
