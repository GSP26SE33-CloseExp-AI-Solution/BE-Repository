using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureDeliveryLogsSchemaAndLegacyCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    -- Normalize legacy naming if an old table still exists.
                    IF to_regclass('"DeliveryLogs"') IS NULL
                       AND to_regclass('"DeliveryRecords"') IS NOT NULL THEN
                        ALTER TABLE "DeliveryRecords" RENAME TO "DeliveryLogs";
                    END IF;

                    -- Ensure canonical table exists for new enum-based delivery flow.
                    IF to_regclass('"DeliveryLogs"') IS NULL THEN
                        CREATE TABLE "DeliveryLogs" (
                            "DeliveryId" uuid NOT NULL,
                            "OrderId" uuid NOT NULL,
                            "UserId" uuid NOT NULL,
                            "Status" integer NULL,
                            "FailedReason" text NULL,
                            "DeliveredAt" timestamp with time zone NULL,
                            "DeliveryLatitude" numeric(10,7) NULL,
                            "DeliveryLongitude" numeric(10,7) NULL,
                            CONSTRAINT "PK_DeliveryLogs" PRIMARY KEY ("DeliveryId"),
                            CONSTRAINT "FK_DeliveryLogs_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("OrderId") ON DELETE CASCADE,
                            CONSTRAINT "FK_DeliveryLogs_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("UserId") ON DELETE CASCADE
                        );
                    END IF;

                    -- Guard legacy null statuses.
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'DeliveryLogs' AND column_name = 'Status'
                    ) THEN
                        UPDATE "DeliveryLogs"
                        SET "Status" = 0
                        WHERE "Status" IS NULL;
                    END IF;

                    IF to_regclass('"IX_DeliveryLogs_OrderId"') IS NULL THEN
                        CREATE INDEX "IX_DeliveryLogs_OrderId" ON "DeliveryLogs" ("OrderId");
                    END IF;

                    IF to_regclass('"IX_DeliveryLogs_UserId"') IS NULL THEN
                        CREATE INDEX "IX_DeliveryLogs_UserId" ON "DeliveryLogs" ("UserId");
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Refunds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                table: "Refunds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Refunds",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "DeliveryLogs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "CollectionPoints",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)",
                oldPrecision: 10,
                oldScale: 7);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "CollectionPoints",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)",
                oldPrecision: 10,
                oldScale: 7);

            migrationBuilder.Sql(
                """
                UPDATE "CollectionPoints"
                SET "Latitude" = COALESCE("Latitude", 0),
                    "Longitude" = COALESCE("Longitude", 0)
                WHERE "Latitude" IS NULL OR "Longitude" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "Refunds",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedBy",
                table: "Refunds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Refunds",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "DeliveryLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "CollectionPoints",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)",
                oldPrecision: 10,
                oldScale: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "CollectionPoints",
                type: "numeric(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)",
                oldPrecision: 10,
                oldScale: 7,
                oldNullable: true);
        }
    }
}
