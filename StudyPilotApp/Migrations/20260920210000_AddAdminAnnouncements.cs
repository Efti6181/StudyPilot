using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920210000_AddAdminAnnouncements")]
public partial class AddAdminAnnouncements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Announcements",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Summary = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                Audience = table.Column<int>(type: "integer", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                IsPublished = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                RecipientCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Announcements", x => x.Id);
                table.ForeignKey(
                    name: "FK_Announcements_AspNetUsers_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Announcements_Audience_Priority_CreatedAt",
            table: "Announcements",
            columns: new[] { "Audience", "Priority", "CreatedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_Announcements_CreatedByUserId",
            table: "Announcements",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_Announcements_IsPublished_PublishedAt",
            table: "Announcements",
            columns: new[] { "IsPublished", "PublishedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Announcements");
    }
}
