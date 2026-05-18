using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemPurchaseUnitId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseUnitId",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            // Historical rows: customer purchased in the lot's unit before PurchaseUnitId existed.
            migrationBuilder.Sql("""
                UPDATE "OrderItems" AS oi
                SET "PurchaseUnitId" = sl."UnitId"
                FROM "StockLots" AS sl
                WHERE oi."LotId" = sl."LotId"
                  AND oi."PurchaseUnitId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PurchaseUnitId",
                table: "OrderItems",
                column: "PurchaseUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_UnitOfMeasures_PurchaseUnitId",
                table: "OrderItems",
                column: "PurchaseUnitId",
                principalTable: "UnitOfMeasures",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_UnitOfMeasures_PurchaseUnitId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_PurchaseUnitId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PurchaseUnitId",
                table: "OrderItems");
        }
    }
}
