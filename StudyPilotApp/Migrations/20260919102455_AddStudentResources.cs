using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    StoredFileName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyResources_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyResources_Courses_CourseId_ApplicationUserId",
                        columns: x => new { x.CourseId, x.ApplicationUserId },
                        principalTable: "Courses",
                        principalColumns: new[] { "Id", "ApplicationUserId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_ApplicationUserId",
                table: "StudyResources",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_ApplicationUserId_Category",
                table: "StudyResources",
                columns: new[] { "ApplicationUserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_ApplicationUserId_CreatedAt",
                table: "StudyResources",
                columns: new[] { "ApplicationUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_ApplicationUserId_IsFavorite",
                table: "StudyResources",
                columns: new[] { "ApplicationUserId", "IsFavorite" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_CourseId_ApplicationUserId",
                table: "StudyResources",
                columns: new[] { "CourseId", "ApplicationUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyResources");
        }
    }
}
