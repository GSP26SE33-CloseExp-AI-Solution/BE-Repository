using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionPayOSColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckoutUrl",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayOSOrderCode",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayOSPaymentLinkId",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PayOSOrderCode",
                table: "Transactions",
                column: "PayOSOrderCode",
                unique: true,
                filter: "\"PayOSOrderCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_PayOSOrderCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CheckoutUrl",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayOSOrderCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PayOSPaymentLinkId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Transactions");
        }
    }
}
