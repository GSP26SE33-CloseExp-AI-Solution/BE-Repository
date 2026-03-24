using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations;

/// <summary>
/// Idempotent PostgreSQL fixes previously run at API startup (PackagingRecords rename, FinalUnitPrice, legacy text status values).
/// </summary>
public partial class LegacySchemaRepairFromProgramStartup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('"OrderPackaging"') IS NULL
                   AND to_regclass('"PackagingRecords"') IS NOT NULL THEN

                    ALTER TABLE "PackagingRecords" RENAME TO "OrderPackaging";

                    IF to_regclass('"IX_PackagingRecords_OrderId"') IS NOT NULL THEN
                        ALTER INDEX "IX_PackagingRecords_OrderId" RENAME TO "IX_OrderPackaging_OrderId";
                    END IF;

                    IF to_regclass('"IX_PackagingRecords_UserId"') IS NOT NULL THEN
                        ALTER INDEX "IX_PackagingRecords_UserId" RENAME TO "IX_OrderPackaging_UserId";
                    END IF;

                    BEGIN
                        ALTER TABLE "OrderPackaging" RENAME CONSTRAINT "PK_PackagingRecords" TO "PK_OrderPackaging";
                    EXCEPTION
                        WHEN undefined_object THEN NULL;
                    END;

                    BEGIN
                        ALTER TABLE "OrderPackaging" RENAME CONSTRAINT "FK_PackagingRecords_Orders_OrderId" TO "FK_OrderPackaging_Orders_OrderId";
                    EXCEPTION
                        WHEN undefined_object THEN NULL;
                    END;

                    BEGIN
                        ALTER TABLE "OrderPackaging" RENAME CONSTRAINT "FK_PackagingRecords_Users_UserId" TO "FK_OrderPackaging_Users_UserId";
                    EXCEPTION
                        WHEN undefined_object THEN NULL;
                    END;
                END IF;

                IF to_regclass('"StockLots"') IS NOT NULL THEN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'StockLots' AND column_name = 'FinalUnitPrice'
                    ) THEN
                        ALTER TABLE "StockLots" ADD COLUMN "FinalUnitPrice" numeric;
                    END IF;
                END IF;

                IF to_regclass('"Orders"') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM information_schema.columns
                       WHERE table_name = 'Orders' AND column_name = 'Status' AND data_type IN ('text', 'character varying')
                   ) THEN
                    UPDATE "Orders"
                    SET "Status" = CASE "Status"
                        WHEN 'Paid_Processing' THEN 'PaidProcessing'
                        WHEN 'Ready_To_Ship' THEN 'ReadyToShip'
                        WHEN 'Delivered_Wait_Confirm' THEN 'DeliveredWaitConfirm'
                        ELSE "Status"
                    END
                    WHERE "Status" IN ('Paid_Processing', 'Ready_To_Ship', 'Delivered_Wait_Confirm');
                END IF;

                IF to_regclass('"Products"') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM information_schema.columns
                       WHERE table_name = 'Products' AND column_name = 'Status' AND data_type IN ('text', 'character varying')
                   ) THEN
                    UPDATE "Products"
                    SET "Status" = CASE "Status"
                        WHEN 'Active' THEN 'Published'
                        WHEN 'Inactive' THEN 'Hidden'
                        ELSE "Status"
                    END
                    WHERE "Status" IN ('Active', 'Inactive');
                END IF;

                IF to_regclass('"StockLots"') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM information_schema.columns
                       WHERE table_name = 'StockLots' AND column_name = 'Status' AND data_type IN ('text', 'character varying')
                   ) THEN
                    UPDATE "StockLots"
                    SET "Status" = CASE "Status"
                        WHEN 'Active' THEN 'Published'
                        WHEN 'Inactive' THEN 'Hidden'
                        ELSE "Status"
                    END
                    WHERE "Status" IN ('Active', 'Inactive');
                END IF;

                IF to_regclass('"OrderPackaging"') IS NOT NULL
                   AND EXISTS (
                       SELECT 1 FROM information_schema.columns
                       WHERE table_name = 'OrderPackaging' AND column_name = 'Status' AND data_type IN ('text', 'character varying')
                   ) THEN
                    UPDATE "OrderPackaging"
                    SET "Status" = CASE "Status"
                        WHEN 'Collecting' THEN 'Packaging'
                        WHEN 'Confirmed' THEN 'Pending'
                        WHEN 'Packaged' THEN 'Completed'
                        ELSE "Status"
                    END
                    WHERE "Status" IN ('Collecting', 'Confirmed', 'Packaged');
                END IF;

            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("LegacySchemaRepairFromProgramStartup is not reversible.");
    }
}
