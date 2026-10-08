using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchAnalytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSearchResultForAuthors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "SearchResults",
                newName: "ExternalAuthorId");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "SearchResults",
                newName: "AuthorName");

            migrationBuilder.AddColumn<int>(
                name: "PaperCount",
                table: "SearchResults",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaperCount",
                table: "SearchResults");

            migrationBuilder.RenameColumn(
                name: "ExternalAuthorId",
                table: "SearchResults",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "AuthorName",
                table: "SearchResults",
                newName: "Title");
        }
    }
}
