using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampusEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    LocationType = table.Column<int>(type: "integer", nullable: false),
                    Venue = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    OnlineUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OrganizerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampusEvents_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventRegistrations",
                columns: table => new
                {
                    CampusEventId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRegistrations", x => new { x.CampusEventId, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_EventRegistrations_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventRegistrations_CampusEvents_CampusEventId",
                        column: x => x.CampusEventId,
                        principalTable: "CampusEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedEvents",
                columns: table => new
                {
                    CampusEventId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedEvents", x => new { x.CampusEventId, x.ApplicationUserId });
                    table.ForeignKey(
                        name: "FK_SavedEvents_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedEvents_CampusEvents_CampusEventId",
                        column: x => x.CampusEventId,
                        principalTable: "CampusEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampusEvents_CreatedByUserId",
                table: "CampusEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CampusEvents_IsPublished_StartAt",
                table: "CampusEvents",
                columns: new[] { "IsPublished", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CampusEvents_LocationType_StartAt",
                table: "CampusEvents",
                columns: new[] { "LocationType", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CampusEvents_Type_StartAt",
                table: "CampusEvents",
                columns: new[] { "Type", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_ApplicationUserId",
                table: "EventRegistrations",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRegistrations_ApplicationUserId_CancelledAt_Registered~",
                table: "EventRegistrations",
                columns: new[] { "ApplicationUserId", "CancelledAt", "RegisteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedEvents_ApplicationUserId",
                table: "SavedEvents",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedEvents_ApplicationUserId_SavedAt",
                table: "SavedEvents",
                columns: new[] { "ApplicationUserId", "SavedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRegistrations");

            migrationBuilder.DropTable(
                name: "SavedEvents");

            migrationBuilder.DropTable(
                name: "CampusEvents");
        }
    }
}
