using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryLogProofImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofImageUrl",
                table: "DeliveryLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofImageUrl",
                table: "DeliveryLogs");
        }
    }
}
