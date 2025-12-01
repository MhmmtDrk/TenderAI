using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenderAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEkapIdToTender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EkapId",
                table: "Tenders",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EkapId",
                table: "Tenders");
        }
    }
}
