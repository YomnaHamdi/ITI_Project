using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PdfDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Letter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: false),
                    EmbeddedPageCount = table.Column<int>(type: "int", nullable: false),
                    EmbeddingsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PdfPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PdfDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    Sentence = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChromaId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsEmbedded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdfPages_PdfDocuments_PdfDocumentId",
                        column: x => x.PdfDocumentId,
                        principalTable: "PdfDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PdfDocuments_Level_Letter",
                table: "PdfDocuments",
                columns: new[] { "Level", "Letter" });

            migrationBuilder.CreateIndex(
                name: "IX_PdfPages_PdfDocumentId",
                table: "PdfPages",
                column: "PdfDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdfPages");

            migrationBuilder.DropTable(
                name: "PdfDocuments");
        }
    }
}
