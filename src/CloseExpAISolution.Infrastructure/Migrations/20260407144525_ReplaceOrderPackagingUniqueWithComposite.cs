using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceOrderPackagingUniqueWithComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPackaging_OrderId",
                table: "OrderPackaging");

            migrationBuilder.DropIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackaging_OrderId_OrderItemId",
                table: "OrderPackaging",
                columns: new[] { "OrderId", "OrderItemId" },
                unique: true,
                filter: "\"OrderItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging",
                column: "OrderItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPackaging_OrderId_OrderItemId",
                table: "OrderPackaging");

            migrationBuilder.DropIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackaging_OrderId",
                table: "OrderPackaging",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging",
                column: "OrderItemId",
                unique: true,
                filter: "\"OrderItemId\" IS NOT NULL");
        }
    }
}
