using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarcodeProducts",
                columns: table => new
                {
                    BarcodeProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<string>(type: "text", nullable: true),
                    Ingredients = table.Column<string>(type: "text", nullable: true),
                    NutritionFactsJson = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Gs1Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsVietnameseProduct = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    ScanCount = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeProducts", x => x.BarcodeProductId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeProducts_Barcode",
                table: "BarcodeProducts",
                column: "Barcode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeProducts");
        }
    }
}
