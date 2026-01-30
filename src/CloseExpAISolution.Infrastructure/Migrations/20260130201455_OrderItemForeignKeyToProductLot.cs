using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemForeignKeyToProductLot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "OrderItems",
                newName: "LotId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                newName: "IX_OrderItems_LotId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductLots_LotId",
                table: "OrderItems",
                column: "LotId",
                principalTable: "ProductLots",
                principalColumn: "LotId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductLots_LotId",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "LotId",
                table: "OrderItems",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_LotId",
                table: "OrderItems",
                newName: "IX_OrderItems_ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
