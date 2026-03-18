using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameLegacyEntitiesAndRemoveBarcodeProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""AIPriceHistories"" DROP CONSTRAINT IF EXISTS ""FK_AIPriceHistories_StockLots_LotId"";
                ALTER TABLE IF EXISTS ""AIPriceHistories"" DROP CONSTRAINT IF EXISTS ""FK_AIPriceHistories_StockLot_LotId"";

                ALTER TABLE IF EXISTS ""DeliveryGroups"" DROP CONSTRAINT IF EXISTS ""FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId"";
                ALTER TABLE IF EXISTS ""DeliveryGroups"" DROP CONSTRAINT IF EXISTS ""FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId"";
                ALTER TABLE IF EXISTS ""DeliveryGroups"" DROP CONSTRAINT IF EXISTS ""FK_DeliveryGroups_TimeSlots_TimeSlotId"";

                ALTER TABLE IF EXISTS ""Products"" DROP CONSTRAINT IF EXISTS ""FK_Products_Units_UnitId"";
                ALTER TABLE IF EXISTS ""Products"" DROP CONSTRAINT IF EXISTS ""FK_Products_UnitOfMeasures_UnitId"";

                ALTER TABLE IF EXISTS ""StockLots"" DROP CONSTRAINT IF EXISTS ""FK_StockLots_Units_UnitId"";
                ALTER TABLE IF EXISTS ""StockLots"" DROP CONSTRAINT IF EXISTS ""FK_StockLots_UnitOfMeasures_UnitId"";

                DROP TABLE IF EXISTS ""BarcodeProducts"";
            ");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF to_regclass('""Units""') IS NOT NULL THEN
        ALTER TABLE ""Units"" DROP CONSTRAINT IF EXISTS ""PK_Units"";
        ALTER TABLE ""Units"" RENAME TO ""UnitOfMeasures"";
    END IF;

    IF to_regclass('""UnitOfMeasures""') IS NOT NULL THEN
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'PK_UnitOfMeasures' AND conrelid = '""UnitOfMeasures""'::regclass
        ) THEN
            ALTER TABLE ""UnitOfMeasures"" ADD CONSTRAINT ""PK_UnitOfMeasures"" PRIMARY KEY (""UnitId"");
        END IF;
    END IF;

    IF to_regclass('""AIPriceHistories""') IS NOT NULL THEN
        ALTER TABLE ""AIPriceHistories"" DROP CONSTRAINT IF EXISTS ""PK_AIPriceHistories"";
        ALTER TABLE ""AIPriceHistories"" RENAME TO ""PricingHistories"";
    END IF;

    IF to_regclass('""PricingHistories""') IS NOT NULL THEN
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'PricingHistories' AND column_name = 'PricingHistoryId'
        ) AND NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId'
        ) THEN
            ALTER TABLE ""PricingHistories"" RENAME COLUMN ""PricingHistoryId"" TO ""AIPriceId"";
        END IF;

        IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_AIPriceHistories_LotId')
           AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_PricingHistories_LotId') THEN
            ALTER INDEX ""IX_AIPriceHistories_LotId"" RENAME TO ""IX_PricingHistories_LotId"";
        END IF;

        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'PK_PricingHistories' AND conrelid = '""PricingHistories""'::regclass
        ) AND EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId'
        ) THEN
            ALTER TABLE ""PricingHistories"" ADD CONSTRAINT ""PK_PricingHistories"" PRIMARY KEY (""AIPriceId"");
        END IF;
    END IF;

    IF to_regclass('""DeliveryGroups""') IS NOT NULL THEN
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'DeliveryGroups' AND column_name = 'TimeSlotId'
        ) AND NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'DeliveryGroups' AND column_name = 'DeliveryTimeSlotId'
        ) THEN
            ALTER TABLE ""DeliveryGroups"" RENAME COLUMN ""TimeSlotId"" TO ""DeliveryTimeSlotId"";
        END IF;

        IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_DeliveryGroups_TimeSlotId')
           AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_DeliveryGroups_DeliveryTimeSlotId') THEN
            ALTER INDEX ""IX_DeliveryGroups_TimeSlotId"" RENAME TO ""IX_DeliveryGroups_DeliveryTimeSlotId"";
        END IF;
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF to_regclass('""PaymentTransactions""') IS NOT NULL THEN
        ALTER TABLE ""PaymentTransactions"" DROP CONSTRAINT IF EXISTS ""PK_PaymentTransactions"";
        ALTER TABLE ""PaymentTransactions"" RENAME TO ""Transactions"";
    END IF;

    IF to_regclass('""Transactions""') IS NOT NULL THEN
        IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'Transactions' AND column_name = 'PaymentTransactionId'
        ) AND NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_name = 'Transactions' AND column_name = 'TransactionId'
        ) THEN
            ALTER TABLE ""Transactions"" RENAME COLUMN ""PaymentTransactionId"" TO ""TransactionId"";
        END IF;

        IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_PaymentTransactions_OrderId')
           AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Transactions_OrderId') THEN
            ALTER INDEX ""IX_PaymentTransactions_OrderId"" RENAME TO ""IX_Transactions_OrderId"";
        END IF;

        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'PK_Transactions' AND conrelid = '""Transactions""'::regclass
        ) THEN
            ALTER TABLE ""Transactions"" ADD CONSTRAINT ""PK_Transactions"" PRIMARY KEY (""TransactionId"");
        END IF;
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
DECLARE
    fallback_unit_id uuid;
BEGIN
    IF to_regclass('""UnitOfMeasures""') IS NOT NULL THEN
        SELECT ""UnitId"" INTO fallback_unit_id FROM ""UnitOfMeasures"" LIMIT 1;

        IF fallback_unit_id IS NOT NULL THEN
            UPDATE ""Products""
            SET ""UnitId"" = fallback_unit_id
            WHERE ""UnitId"" IS NULL
               OR ""UnitId"" NOT IN (SELECT ""UnitId"" FROM ""UnitOfMeasures"");

            UPDATE ""StockLots""
            SET ""UnitId"" = fallback_unit_id
            WHERE ""UnitId"" IS NULL
               OR ""UnitId"" NOT IN (SELECT ""UnitId"" FROM ""UnitOfMeasures"");
        END IF;
    END IF;
END $$;
");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId",
                table: "DeliveryGroups",
                column: "DeliveryTimeSlotId",
                principalTable: "DeliveryTimeSlots",
                principalColumn: "DeliveryTimeSlotId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PricingHistories_StockLots_LotId",
                table: "PricingHistories",
                column: "LotId",
                principalTable: "StockLots",
                principalColumn: "LotId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnitOfMeasures_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "UnitOfMeasures",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLots_UnitOfMeasures_UnitId",
                table: "StockLots",
                column: "UnitId",
                principalTable: "UnitOfMeasures",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId",
                table: "DeliveryGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_PricingHistories_StockLots_LotId",
                table: "PricingHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnitOfMeasures_UnitId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLots_UnitOfMeasures_UnitId",
                table: "StockLots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnitOfMeasures",
                table: "UnitOfMeasures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PricingHistories",
                table: "PricingHistories");

            migrationBuilder.RenameTable(
                name: "UnitOfMeasures",
                newName: "Units");

            migrationBuilder.RenameTable(
                name: "PricingHistories",
                newName: "AIPriceHistories");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "PaymentTransactions");

            migrationBuilder.RenameColumn(
                name: "TransactionId",
                table: "PaymentTransactions",
                newName: "PaymentTransactionId");

            migrationBuilder.RenameColumn(
                name: "DeliveryTimeSlotId",
                table: "DeliveryGroups",
                newName: "TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryGroups_DeliveryTimeSlotId",
                table: "DeliveryGroups",
                newName: "IX_DeliveryGroups_TimeSlotId");

            migrationBuilder.RenameIndex(
                name: "IX_PricingHistories_LotId",
                table: "AIPriceHistories",
                newName: "IX_AIPriceHistories_LotId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_OrderId",
                table: "PaymentTransactions",
                newName: "IX_PaymentTransactions_OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Units",
                table: "Units",
                column: "UnitId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AIPriceHistories",
                table: "AIPriceHistories",
                column: "AIPriceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentTransactions",
                table: "PaymentTransactions",
                column: "PaymentTransactionId");

            migrationBuilder.CreateTable(
                name: "BarcodeProducts",
                columns: table => new
                {
                    BarcodeProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Brand = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Gs1Prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Ingredients = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsVietnameseProduct = table.Column<bool>(type: "boolean", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    NutritionFactsJson = table.Column<string>(type: "text", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScanCount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeProducts", x => x.BarcodeProductId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeProducts_Barcode",
                table: "BarcodeProducts",
                column: "Barcode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AIPriceHistories_StockLots_LotId",
                table: "AIPriceHistories",
                column: "LotId",
                principalTable: "StockLots",
                principalColumn: "LotId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId",
                table: "DeliveryGroups",
                column: "TimeSlotId",
                principalTable: "DeliveryTimeSlots",
                principalColumn: "DeliveryTimeSlotId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLots_Units_UnitId",
                table: "StockLots",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
