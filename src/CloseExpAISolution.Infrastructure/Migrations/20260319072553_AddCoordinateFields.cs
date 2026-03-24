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
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('"DeliveryLogs"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DeliveryLogs' AND column_name = 'DeliveryLatitude') THEN
                        ALTER TABLE "DeliveryLogs" ADD "DeliveryLatitude" numeric(10,7);
                    END IF;

                    IF to_regclass('"DeliveryLogs"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DeliveryLogs' AND column_name = 'DeliveryLongitude') THEN
                        ALTER TABLE "DeliveryLogs" ADD "DeliveryLongitude" numeric(10,7);
                    END IF;

                    IF to_regclass('"DeliveryGroups"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DeliveryGroups' AND column_name = 'CenterLatitude') THEN
                        ALTER TABLE "DeliveryGroups" ADD "CenterLatitude" numeric(10,7);
                    END IF;

                    IF to_regclass('"DeliveryGroups"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DeliveryGroups' AND column_name = 'CenterLongitude') THEN
                        ALTER TABLE "DeliveryGroups" ADD "CenterLongitude" numeric(10,7);
                    END IF;

                    IF to_regclass('"CustomerAddresses"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CustomerAddresses' AND column_name = 'Latitude') THEN
                        ALTER TABLE "CustomerAddresses" ADD "Latitude" numeric(10,7) NOT NULL DEFAULT 0;
                    END IF;

                    IF to_regclass('"CustomerAddresses"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CustomerAddresses' AND column_name = 'Longitude') THEN
                        ALTER TABLE "CustomerAddresses" ADD "Longitude" numeric(10,7) NOT NULL DEFAULT 0;
                    END IF;

                    IF to_regclass('"CollectionPoints"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CollectionPoints' AND column_name = 'Latitude') THEN
                        ALTER TABLE "CollectionPoints" ADD "Latitude" numeric(10,7) NOT NULL DEFAULT 0;
                    END IF;

                    IF to_regclass('"CollectionPoints"') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'CollectionPoints' AND column_name = 'Longitude') THEN
                        ALTER TABLE "CollectionPoints" ADD "Longitude" numeric(10,7) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('"DeliveryLogs"') IS NOT NULL
                       AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DeliveryLogs' AND column_name = 'DeliveryLatitude') THEN
                        ALTER TABLE "DeliveryLogs" DROP COLUMN "DeliveryLatitude";
                    END IF;

                    IF to_regclass('"DeliveryLogs"') IS NOT NULL
                       AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'DeliveryLogs' AND column_name = 'DeliveryLongitude') THEN
                        ALTER TABLE "DeliveryLogs" DROP COLUMN "DeliveryLongitude";
                    END IF;
                END $$;
                """);
        }
    }
}
