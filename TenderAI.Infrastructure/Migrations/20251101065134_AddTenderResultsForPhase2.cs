using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderResultsForPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    IKN = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    WinnerCompany = table.Column<string>(type: "text", nullable: true),
                    WinnerTaxNumber = table.Column<string>(type: "text", nullable: true),
                    ContractAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    NumberOfBidders = table.Column<int>(type: "integer", nullable: false),
                    AwardDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    RawData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderResults_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderResultItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    TechnicalSpecs = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderResultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderResultItems_TenderResults_TenderResultId",
                        column: x => x.TenderResultId,
                        principalTable: "TenderResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderResultItems_Category",
                table: "TenderResultItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TenderResultItems_TenderResultId",
                table: "TenderResultItems",
                column: "TenderResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderResultItems_TenderResultId_ItemNumber",
                table: "TenderResultItems",
                columns: new[] { "TenderResultId", "ItemNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderResults_AwardDate",
                table: "TenderResults",
                column: "AwardDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenderResults_IKN",
                table: "TenderResults",
                column: "IKN");

            migrationBuilder.CreateIndex(
                name: "IX_TenderResults_IsCompleted",
                table: "TenderResults",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_TenderResults_Status",
                table: "TenderResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TenderResults_TenderId",
                table: "TenderResults",
                column: "TenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderResultItems");

            migrationBuilder.DropTable(
                name: "TenderResults");
        }
    }
}
