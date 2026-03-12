using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyPricingDoorPickup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DoorPickups_DoorPickupId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DoorPickupId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DoorPickupId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Pricings");

            migrationBuilder.DropTable(
                name: "DoorPickups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoorPickups",
                columns: table => new
                {
                    DoorPickupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoorPickups", x => x.DoorPickupId);
                });

            migrationBuilder.CreateTable(
                name: "Pricings",
                columns: table => new
                {
                    PricingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUnit = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    FinalUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginalUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PricedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PricedBy = table.Column<string>(type: "text", nullable: true),
                    PricingConfidence = table.Column<float>(type: "real", nullable: false),
                    PricingReasons = table.Column<string>(type: "text", nullable: true),
                    SalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    SuggestedUnitPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pricings", x => x.PricingId);
                    table.ForeignKey(
                        name: "FK_Pricings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "DoorPickupId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DoorPickupId",
                table: "Orders",
                column: "DoorPickupId");

            migrationBuilder.CreateIndex(
                name: "IX_Pricings_ProductId",
                table: "Pricings",
                column: "ProductId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DoorPickups_DoorPickupId",
                table: "Orders",
                column: "DoorPickupId",
                principalTable: "DoorPickups",
                principalColumn: "DoorPickupId");
        }
    }
}
