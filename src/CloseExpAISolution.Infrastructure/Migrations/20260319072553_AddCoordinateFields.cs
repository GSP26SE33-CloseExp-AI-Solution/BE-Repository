using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLatitude",
                table: "DeliveryLogs",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLongitude",
                table: "DeliveryLogs",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CenterLatitude",
                table: "DeliveryGroups",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CenterLongitude",
                table: "DeliveryGroups",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "CustomerAddresses",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "CustomerAddresses",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "CollectionPoints",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "CollectionPoints",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryLatitude",
                table: "DeliveryLogs");

            migrationBuilder.DropColumn(
                name: "DeliveryLongitude",
                table: "DeliveryLogs");

            migrationBuilder.DropColumn(
                name: "CenterLatitude",
                table: "DeliveryGroups");

            migrationBuilder.DropColumn(
                name: "CenterLongitude",
                table: "DeliveryGroups");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "CollectionPoints");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "CollectionPoints");
        }
    }
}
