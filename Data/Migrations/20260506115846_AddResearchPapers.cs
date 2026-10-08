using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResearchAnalytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchPapers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchPapers",
                columns: table => new
                {
                    ResearchPaperId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalPaperId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: true),
                    CitationCount = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SearchResultId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPapers", x => x.ResearchPaperId);
                    table.ForeignKey(
                        name: "FK_ResearchPapers_SearchResults_SearchResultId",
                        column: x => x.SearchResultId,
                        principalTable: "SearchResults",
                        principalColumn: "SearchResultId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPapers_SearchResultId",
                table: "ResearchPapers",
                column: "SearchResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResearchPapers");
        }
    }
}
