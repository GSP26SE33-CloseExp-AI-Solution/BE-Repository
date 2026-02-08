using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMarketPriceUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_Source",
                table: "MarketPrices");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName",
                table: "MarketPrices",
                columns: new[] { "Barcode", "Source", "StoreName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName",
                table: "MarketPrices");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_Source",
                table: "MarketPrices",
                columns: new[] { "Barcode", "Source" },
                unique: true);
        }
    }
}
