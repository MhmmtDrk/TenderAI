using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisText = table.Column<string>(type: "text", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    FinancialRisks = table.Column<string>(type: "text", nullable: true),
                    OperationalRisks = table.Column<string>(type: "text", nullable: true),
                    LegalRisks = table.Column<string>(type: "text", nullable: true),
                    KeyPoints = table.Column<string>(type: "text", nullable: true),
                    Recommendations = table.Column<string>(type: "text", nullable: true),
                    AnalysisModel = table.Column<string>(type: "text", nullable: false),
                    AnalysisDuration = table.Column<double>(type: "double precision", nullable: false),
                    TokensUsed = table.Column<int>(type: "integer", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAnalyses_TenderDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "TenderDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalyses_AnalyzedAt",
                table: "DocumentAnalyses",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalyses_DocumentId",
                table: "DocumentAnalyses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalyses_RiskLevel",
                table: "DocumentAnalyses",
                column: "RiskLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAnalyses");
        }
    }
}
