using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryGroupLifecycleConfirmedPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cũ: Pending(1) + chưa gán shipper = pool / chờ gán. Mới: trạng thái đó là Confirmed(6).
            migrationBuilder.Sql(
                @"UPDATE ""DeliveryGroups"" SET ""Status"" = 6 WHERE ""Status"" = 1 AND ""DeliveryStaffId"" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""DeliveryGroups"" SET ""Status"" = 1 WHERE ""Status"" = 6 AND ""DeliveryStaffId"" IS NULL;");
        }
    }
}
