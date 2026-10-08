using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchAnalytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceScoreWithCitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "SearchResults");

            migrationBuilder.AddColumn<int>(
                name: "Citations",
                table: "SearchResults",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Citations",
                table: "SearchResults");

            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "SearchResults",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
