using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricalTenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IKN = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    AuthorityName = table.Column<string>(type: "text", nullable: false),
                    TenderType = table.Column<string>(type: "text", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ContractAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    WinnerCompany = table.Column<string>(type: "text", nullable: false),
                    BidderCount = table.Column<int>(type: "integer", nullable: false),
                    TenderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: false),
                    OkasCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalTenders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IKN = table.Column<string>(type: "text", nullable: false),
                    AuthorityName = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TenderType = table.Column<string>(type: "text", nullable: false),
                    ProcurementMethod = table.Column<string>(type: "text", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BidDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Province = table.Column<string>(type: "text", nullable: false),
                    District = table.Column<string>(type: "text", nullable: false),
                    OkasCode = table.Column<string>(type: "text", nullable: false),
                    AdministrativeSpecPdfUrl = table.Column<string>(type: "text", nullable: true),
                    TechnicalSpecPdfUrl = table.Column<string>(type: "text", nullable: true),
                    ContractDraftPdfUrl = table.Column<string>(type: "text", nullable: true),
                    BftcPdfUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsElectronic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Brand = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    TechnicalSpecifications = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    IsInStock = table.Column<bool>(type: "boolean", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalBftcItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HistoricalTenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalBftcItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricalBftcItems_HistoricalTenders_HistoricalTenderId",
                        column: x => x.HistoricalTenderId,
                        principalTable: "HistoricalTenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BftcItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    UserUnitCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    HistoricalAveragePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    HistoricalMinPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    HistoricalMaxPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    HistoricalTenderCount = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BftcItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BftcItems_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RiskMarginRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    RiskMarginAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RiskAdjustedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProfitMarginRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ProfitMarginAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RecommendedBidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HistoricalAverageBid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CompetitivePosition = table.Column<string>(type: "text", nullable: true),
                    CompetitiveRatio = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    Recommendation = table.Column<string>(type: "text", nullable: false),
                    RecommendationReason = table.Column<string>(type: "text", nullable: true),
                    WinProbability = table.Column<double>(type: "double precision", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceAnalyses_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialRiskScore = table.Column<double>(type: "double precision", nullable: false),
                    OperationalRiskScore = table.Column<double>(type: "double precision", nullable: false),
                    LegalRiskScore = table.Column<double>(type: "double precision", nullable: false),
                    TotalRiskScore = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    RequiresSimilarWorkCertificate = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredSimilarWorkCount = table.Column<int>(type: "integer", nullable: true),
                    TemporaryGuaranteeRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    FinalGuaranteeRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    RequiredTseCertificates = table.Column<string>(type: "text", nullable: true),
                    RequiredIsoCertificates = table.Column<string>(type: "text", nullable: true),
                    DeliveryDays = table.Column<int>(type: "integer", nullable: true),
                    WarrantyMonths = table.Column<int>(type: "integer", nullable: true),
                    PaymentTermDays = table.Column<int>(type: "integer", nullable: true),
                    HasAdvancePayment = table.Column<bool>(type: "boolean", nullable: false),
                    AdvancePaymentRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    HasPriceAdjustment = table.Column<bool>(type: "boolean", nullable: false),
                    DelayPenaltyRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    RequiresTraining = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresInstallation = table.Column<bool>(type: "boolean", nullable: false),
                    AnalysisSummary = table.Column<string>(type: "text", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskAnalyses_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalCompatibilityScore = table.Column<double>(type: "double precision", nullable: false),
                    TechnicalRequirementsSummary = table.Column<string>(type: "text", nullable: true),
                    MissingProducts = table.Column<string>(type: "text", nullable: true),
                    EstimatedOperationalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalAnalyses_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderAnnouncements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnnouncementType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAnnouncements_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    TechnicalSpecifications = table.Column<string>(type: "text", nullable: true),
                    HasBrandRequirement = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredBrand = table.Column<string>(type: "text", nullable: true),
                    AcceptsEquivalent = table.Column<bool>(type: "boolean", nullable: false),
                    CompatibilityScore = table.Column<double>(type: "double precision", nullable: true),
                    MatchedUserProductId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalItems_TechnicalAnalyses_TechnicalAnalysisId",
                        column: x => x.TechnicalAnalysisId,
                        principalTable: "TechnicalAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BftcItems_TenderId",
                table: "BftcItems",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_BftcItems_TenderId_ItemNumber",
                table: "BftcItems",
                columns: new[] { "TenderId", "ItemNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalBftcItems_HistoricalTenderId",
                table: "HistoricalBftcItems",
                column: "HistoricalTenderId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalTenders_IKN",
                table: "HistoricalTenders",
                column: "IKN");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalTenders_OkasCode",
                table: "HistoricalTenders",
                column: "OkasCode");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalTenders_Province",
                table: "HistoricalTenders",
                column: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalTenders_TenderDate",
                table: "HistoricalTenders",
                column: "TenderDate");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAnalyses_TenderId",
                table: "PriceAnalyses",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskAnalyses_TenderId",
                table: "RiskAnalyses",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalAnalyses_TenderId",
                table: "TechnicalAnalyses",
                column: "TenderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalItems_TechnicalAnalysisId",
                table: "TechnicalItems",
                column: "TechnicalAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAnnouncements_TenderId_AnnouncementType",
                table: "TenderAnnouncements",
                columns: new[] { "TenderId", "AnnouncementType" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_BidDeadline",
                table: "Tenders",
                column: "BidDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_IKN",
                table: "Tenders",
                column: "IKN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_OkasCode",
                table: "Tenders",
                column: "OkasCode");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_Province",
                table: "Tenders",
                column: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_Status",
                table: "Tenders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserProducts_Category",
                table: "UserProducts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_UserProducts_UserId",
                table: "UserProducts",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BftcItems");

            migrationBuilder.DropTable(
                name: "HistoricalBftcItems");

            migrationBuilder.DropTable(
                name: "PriceAnalyses");

            migrationBuilder.DropTable(
                name: "RiskAnalyses");

            migrationBuilder.DropTable(
                name: "TechnicalItems");

            migrationBuilder.DropTable(
                name: "TenderAnnouncements");

            migrationBuilder.DropTable(
                name: "UserProducts");

            migrationBuilder.DropTable(
                name: "HistoricalTenders");

            migrationBuilder.DropTable(
                name: "TechnicalAnalyses");

            migrationBuilder.DropTable(
                name: "Tenders");
        }
    }
}
