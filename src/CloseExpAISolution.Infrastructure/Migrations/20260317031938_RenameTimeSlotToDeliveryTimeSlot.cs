using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTimeSlotToDeliveryTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryGroups_TimeSlots_TimeSlotId",
                table: "DeliveryGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PickupPoints_PickupPointId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_TimeSlots_TimeSlotId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_PackagingRecords_Orders_OrderId",
                table: "PackagingRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PackagingRecords_Users_UserId",
                table: "PackagingRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimeSlots",
                table: "TimeSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PickupPoints",
                table: "PickupPoints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PackagingRecords",
                table: "PackagingRecords");

            migrationBuilder.RenameTable(
                name: "TimeSlots",
                newName: "DeliveryTimeSlots");

            migrationBuilder.RenameTable(
                name: "PickupPoints",
                newName: "CollectionPoint");

            migrationBuilder.RenameTable(
                name: "PackagingRecords",
                newName: "OrderPackaging");

            migrationBuilder.RenameColumn(
                name: "TimeSlotId",
                table: "Orders",
                newName: "DeliveryTimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_TimeSlotId",
                table: "Orders",
                newName: "IX_Orders_DeliveryTimeSlotId");

            migrationBuilder.RenameColumn(
                name: "TimeSlotId",
                table: "DeliveryGroups",
                newName: "DeliveryTimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryGroups_TimeSlotId",
                table: "DeliveryGroups",
                newName: "IX_DeliveryGroups_DeliveryTimeSlotId");

            migrationBuilder.RenameColumn(
                name: "TimeSlotId",
                table: "DeliveryTimeSlots",
                newName: "DeliveryTimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_PackagingRecords_UserId",
                table: "OrderPackaging",
                newName: "IX_OrderPackaging_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PackagingRecords_OrderId",
                table: "OrderPackaging",
                newName: "IX_OrderPackaging_OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeliveryTimeSlots",
                table: "DeliveryTimeSlots",
                column: "DeliveryTimeSlotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CollectionPoint",
                table: "CollectionPoint",
                column: "PickupPointId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderPackaging",
                table: "OrderPackaging",
                column: "PackagingId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId",
                table: "DeliveryGroups",
                column: "DeliveryTimeSlotId",
                principalTable: "DeliveryTimeSlots",
                principalColumn: "DeliveryTimeSlotId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPackaging_Orders_OrderId",
                table: "OrderPackaging",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPackaging_Users_UserId",
                table: "OrderPackaging",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CollectionPoint_PickupPointId",
                table: "Orders",
                column: "PickupPointId",
                principalTable: "CollectionPoint",
                principalColumn: "PickupPointId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId",
                table: "Orders",
                column: "DeliveryTimeSlotId",
                principalTable: "DeliveryTimeSlots",
                principalColumn: "DeliveryTimeSlotId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId",
                table: "DeliveryGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderPackaging_Orders_OrderId",
                table: "OrderPackaging");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderPackaging_Users_UserId",
                table: "OrderPackaging");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CollectionPoint_PickupPointId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderPackaging",
                table: "OrderPackaging");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeliveryTimeSlots",
                table: "DeliveryTimeSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CollectionPoint",
                table: "CollectionPoint");

            migrationBuilder.RenameTable(
                name: "OrderPackaging",
                newName: "PackagingRecords");

            migrationBuilder.RenameTable(
                name: "DeliveryTimeSlots",
                newName: "TimeSlots");

            migrationBuilder.RenameTable(
                name: "CollectionPoint",
                newName: "PickupPoints");

            migrationBuilder.RenameColumn(
                name: "DeliveryTimeSlotId",
                table: "Orders",
                newName: "TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DeliveryTimeSlotId",
                table: "Orders",
                newName: "IX_Orders_TimeSlotId");

            migrationBuilder.RenameColumn(
                name: "DeliveryTimeSlotId",
                table: "DeliveryGroups",
                newName: "TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryGroups_DeliveryTimeSlotId",
                table: "DeliveryGroups",
                newName: "IX_DeliveryGroups_TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderPackaging_UserId",
                table: "PackagingRecords",
                newName: "IX_PackagingRecords_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderPackaging_OrderId",
                table: "PackagingRecords",
                newName: "IX_PackagingRecords_OrderId");

            migrationBuilder.RenameColumn(
                name: "DeliveryTimeSlotId",
                table: "TimeSlots",
                newName: "TimeSlotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PackagingRecords",
                table: "PackagingRecords",
                column: "PackagingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimeSlots",
                table: "TimeSlots",
                column: "TimeSlotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PickupPoints",
                table: "PickupPoints",
                column: "PickupPointId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryGroups_TimeSlots_TimeSlotId",
                table: "DeliveryGroups",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "TimeSlotId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PickupPoints_PickupPointId",
                table: "Orders",
                column: "PickupPointId",
                principalTable: "PickupPoints",
                principalColumn: "PickupPointId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_TimeSlots_TimeSlotId",
                table: "Orders",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "TimeSlotId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackagingRecords_Orders_OrderId",
                table: "PackagingRecords",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackagingRecords_Users_UserId",
                table: "PackagingRecords",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
