using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDetailInfoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SafetyWarnings",
                table: "Products",
                newName: "Weight");

            migrationBuilder.RenameColumn(
                name: "ResponsibleOrganization",
                table: "Products",
                newName: "SafetyWarning");

            migrationBuilder.RenameColumn(
                name: "NetWeight",
                table: "Products",
                newName: "Distributor");

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

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "ProductLots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedBy",
                table: "ProductLots",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "PublishedAt",
                table: "ProductLots");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                table: "ProductLots");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "Products",
                newName: "SafetyWarnings");

            migrationBuilder.RenameColumn(
                name: "SafetyWarning",
                table: "Products",
                newName: "ResponsibleOrganization");

            migrationBuilder.RenameColumn(
                name: "Distributor",
                table: "Products",
                newName: "NetWeight");
        }
    }
}
