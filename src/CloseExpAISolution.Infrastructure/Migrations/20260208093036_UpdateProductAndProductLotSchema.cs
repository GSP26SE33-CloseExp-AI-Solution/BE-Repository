using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductAndProductLotSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductLots_Units_UnitId",
                table: "ProductLots");

            migrationBuilder.DropIndex(
                name: "IX_ProductLots_UnitId",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ManufactureDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PricedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PricedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PricingConfidence",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PricingReasons",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShelfLifeDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SuggestedPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FinalUnitPrice",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "PricedAt",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "PricedBy",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "PricingConfidence",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "PricingReasons",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "SuggestedUnitPrice",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "ProductLots");

            migrationBuilder.RenameColumn(
                name: "WeightType",
                table: "Products",
                newName: "QuantityType");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "Products",
                newName: "MadeInCountry");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "isPrimary",
                table: "ProductImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Pricings",
                columns: table => new
                {
                    PricingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseUnit = table.Column<string>(type: "text", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    PricedBy = table.Column<string>(type: "text", nullable: true),
                    PricedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PricingConfidence = table.Column<float>(type: "real", nullable: false),
                    PricingReasons = table.Column<string>(type: "text", nullable: true),
                    OriginalUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SuggestedUnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    FinalUnitPrice = table.Column<decimal>(type: "numeric", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Pricings_ProductId",
                table: "Pricings",
                column: "ProductId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Pricings");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "isPrimary",
                table: "ProductImages");

            migrationBuilder.RenameColumn(
                name: "QuantityType",
                table: "Products",
                newName: "WeightType");

            migrationBuilder.RenameColumn(
                name: "MadeInCountry",
                table: "Products",
                newName: "Weight");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufactureDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PricedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricedBy",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PricingConfidence",
                table: "Products",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "PricingReasons",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfLifeDays",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitPrice",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PricedAt",
                table: "ProductLots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricedBy",
                table: "ProductLots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PricingConfidence",
                table: "ProductLots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "PricingReasons",
                table: "ProductLots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedUnitPrice",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "ProductLots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProductLots_UnitId",
                table: "ProductLots",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLots_Units_UnitId",
                table: "ProductLots",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
