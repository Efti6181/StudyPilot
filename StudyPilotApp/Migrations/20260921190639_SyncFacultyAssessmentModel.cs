using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyPilotApp.Migrations
{
    /// <inheritdoc />
    public partial class SyncFacultyAssessmentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                
                 
                DROP INDEX IF EXISTS "IX_Assessments_FacultyAssessmentId";
                
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Assessments_FacultyAssessmentId",
                table: "Assessments",
                column: "FacultyAssessmentId");
        }
    }
}
