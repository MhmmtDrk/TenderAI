using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    DocumentTypeName = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    OriginalUrl = table.Column<string>(type: "text", nullable: true),
                    IsDownloaded = table.Column<bool>(type: "boolean", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DownloadError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderDocuments_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_IsDownloaded",
                table: "TenderDocuments",
                column: "IsDownloaded");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderId",
                table: "TenderDocuments",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderId_DocumentType",
                table: "TenderDocuments",
                columns: new[] { "TenderId", "DocumentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderDocuments");
        }
    }
}
