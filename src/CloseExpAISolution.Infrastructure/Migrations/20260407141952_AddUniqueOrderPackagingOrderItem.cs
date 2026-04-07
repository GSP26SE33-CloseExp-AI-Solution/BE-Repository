using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueOrderPackagingOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging",
                column: "OrderItemId",
                unique: true,
                filter: "\"OrderItemId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackaging_OrderItemId",
                table: "OrderPackaging",
                column: "OrderItemId");
        }
    }
}
