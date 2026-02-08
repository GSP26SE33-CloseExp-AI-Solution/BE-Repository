using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddProductWeightTypeAndDefaultPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultPricePerKg",
                table: "Products",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeightType",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPricePerKg",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WeightType",
                table: "Products");
        }
    }
}
