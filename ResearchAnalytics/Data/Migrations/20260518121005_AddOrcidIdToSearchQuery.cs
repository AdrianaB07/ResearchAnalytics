using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchAnalytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrcidIdToSearchQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrcidId",
                table: "SearchQueries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrcidId",
                table: "SearchQueries");
        }
    }
}
