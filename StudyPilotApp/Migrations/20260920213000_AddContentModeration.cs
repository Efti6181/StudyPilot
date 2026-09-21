using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using StudyPilotApp.Data;

#nullable disable

namespace StudyPilotApp.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920213000_AddContentModeration")]
public partial class AddContentModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContentModerationRecords",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ContentType = table.Column<int>(type: "integer", nullable: false),
                SourceId = table.Column<int>(type: "integer", nullable: false),
                ContentTitle = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                ContentExcerpt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                OwnerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                OwnerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                ModeratedByUserId = table.Column<string>(type: "text", nullable: false),
                ModeratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContentModerationRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContentModerationRecords_AspNetUsers_ModeratedByUserId",
                    column: x => x.ModeratedByUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContentModerationRecords_ContentType_ModeratedAt",
            table: "ContentModerationRecords",
            columns: new[] { "ContentType", "ModeratedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_ContentModerationRecords_ContentType_SourceId",
            table: "ContentModerationRecords",
            columns: new[] { "ContentType", "SourceId" });
        migrationBuilder.CreateIndex(
            name: "IX_ContentModerationRecords_ModeratedByUserId",
            table: "ContentModerationRecords",
            column: "ModeratedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ContentModerationRecords");
    }
}
