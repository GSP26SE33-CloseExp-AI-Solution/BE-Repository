using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CollectionPointsLatLngIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName",
                table: "MarketPrices");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_CollectedAt",
                table: "MarketPrices",
                columns: new[] { "Barcode", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName_CollectedAt",
                table: "MarketPrices",
                columns: new[] { "Barcode", "Source", "StoreName", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPoints_Latitude_Longitude",
                table: "CollectionPoints",
                columns: new[] { "Latitude", "Longitude" },
                filter: "\"Latitude\" IS NOT NULL AND \"Longitude\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_CollectedAt",
                table: "MarketPrices");

            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName_CollectedAt",
                table: "MarketPrices");

            migrationBuilder.DropIndex(
                name: "IX_CollectionPoints_Latitude_Longitude",
                table: "CollectionPoints");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName",
                table: "MarketPrices",
                columns: new[] { "Barcode", "Source", "StoreName" },
                unique: true);
        }
    }
}
