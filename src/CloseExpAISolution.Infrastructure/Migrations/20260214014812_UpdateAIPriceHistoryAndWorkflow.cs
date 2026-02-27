using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAIPriceHistoryAndWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Weight column was already removed in previous migration
            // migrationBuilder.DropColumn(
            //     name: "Weight",
            //     table: "ProductLots");

            migrationBuilder.RenameColumn(
                name: "SuggestedUnitPrice",
                table: "AIPriceHistories",
                newName: "SuggestedPrice");

            migrationBuilder.RenameColumn(
                name: "AIPriceId",
                table: "AIPriceHistories",
                newName: "PriceHistoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "AIPriceHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "MarketMinPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MarketMaxPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "MarketAvgPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<float>(
                name: "AIConfidence",
                table: "AIPriceHistories",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<bool>(
                name: "AcceptedSuggestion",
                table: "AIPriceHistories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "AIPriceHistories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedBy",
                table: "AIPriceHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AIPriceHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffFeedback",
                table: "AIPriceHistories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIConfidence",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "AcceptedSuggestion",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "FinalPrice",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AIPriceHistories");

            migrationBuilder.DropColumn(
                name: "StaffFeedback",
                table: "AIPriceHistories");

            migrationBuilder.RenameColumn(
                name: "SuggestedPrice",
                table: "AIPriceHistories",
                newName: "SuggestedUnitPrice");

            migrationBuilder.RenameColumn(
                name: "PriceHistoryId",
                table: "AIPriceHistories",
                newName: "AIPriceId");

            // Weight column doesn't need to be re-added (was not dropped in Up)
            // migrationBuilder.AddColumn<decimal>(
            //     name: "Weight",
            //     table: "ProductLots",
            //     type: "numeric",
            //     nullable: false,
            //     defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "AIPriceHistories",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MarketMinPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MarketMaxPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MarketAvgPrice",
                table: "AIPriceHistories",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }
    }
}
