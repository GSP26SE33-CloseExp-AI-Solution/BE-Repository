using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeEntityNamingAndOrderAddressCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('"CollectionPoint"') IS NOT NULL AND to_regclass('"CollectionPoints"') IS NULL THEN
                        ALTER TABLE "CollectionPoint" RENAME TO "CollectionPoints";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='PickupPointId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='CollectionId') THEN
                        ALTER TABLE "CollectionPoints" RENAME COLUMN "PickupPointId" TO "CollectionId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='Address')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='AddressLine') THEN
                        ALTER TABLE "CollectionPoints" RENAME COLUMN "Address" TO "AddressLine";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='PickupPointId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='CollectionId') THEN
                        ALTER TABLE "Orders" RENAME COLUMN "PickupPointId" TO "CollectionId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Orders_PickupPointId')
                       AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Orders_CollectionId') THEN
                        ALTER INDEX "IX_Orders_PickupPointId" RENAME TO "IX_Orders_CollectionId";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('"TimeSlots"') IS NOT NULL AND to_regclass('"DeliveryTimeSlots"') IS NULL THEN
                        ALTER TABLE "TimeSlots" RENAME TO "DeliveryTimeSlots";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryTimeSlots' AND column_name='TimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryTimeSlots' AND column_name='DeliveryTimeSlotId') THEN
                        ALTER TABLE "DeliveryTimeSlots" RENAME COLUMN "TimeSlotId" TO "DeliveryTimeSlotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='TimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='DeliveryTimeSlotId') THEN
                        ALTER TABLE "Orders" RENAME COLUMN "TimeSlotId" TO "DeliveryTimeSlotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Orders_TimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Orders_DeliveryTimeSlotId') THEN
                        ALTER INDEX "IX_Orders_TimeSlotId" RENAME TO "IX_Orders_DeliveryTimeSlotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='DeliveryTimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='TimeSlotId') THEN
                        ALTER TABLE "DeliveryGroups" RENAME COLUMN "DeliveryTimeSlotId" TO "TimeSlotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_DeliveryGroups_DeliveryTimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_DeliveryGroups_TimeSlotId') THEN
                        ALTER INDEX "IX_DeliveryGroups_DeliveryTimeSlotId" RENAME TO "IX_DeliveryGroups_TimeSlotId";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Orders" DROP COLUMN IF EXISTS "DeliveryAddress";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='AddressId') THEN
                        ALTER TABLE "Orders" ADD COLUMN "AddressId" uuid NULL;
                    ELSE
                        ALTER TABLE "Orders" ALTER COLUMN "AddressId" DROP NOT NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    default_category uuid;
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='CategoryId') THEN
                        ALTER TABLE "Products" ADD COLUMN "CategoryId" uuid NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Promotions' AND column_name='CategoryId') THEN
                        ALTER TABLE "Promotions" ADD COLUMN "CategoryId" uuid NULL;
                    END IF;

                    SELECT "CategoryId" INTO default_category FROM "Categories" LIMIT 1;
                    IF default_category IS NOT NULL THEN
                        UPDATE "Promotions" SET "CategoryId" = default_category WHERE "CategoryId" IS NULL;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM "Promotions" WHERE "CategoryId" IS NULL) THEN
                        ALTER TABLE "Promotions" ALTER COLUMN "CategoryId" SET NOT NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Orders_AddressId" ON "Orders" ("AddressId");
                CREATE INDEX IF NOT EXISTS "IX_Orders_CollectionId" ON "Orders" ("CollectionId");
                CREATE INDEX IF NOT EXISTS "IX_Products_CategoryId" ON "Products" ("CategoryId");
                CREATE INDEX IF NOT EXISTS "IX_Promotions_CategoryId" ON "Promotions" ("CategoryId");
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_CollectionPoint_PickupPointId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_PickupPoints_PickupPointId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_CollectionPoints_CollectionId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_TimeSlots_TimeSlotId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_CustomerAddresses_AddressId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_Promotions_PromotionId";
                ALTER TABLE "DeliveryGroups" DROP CONSTRAINT IF EXISTS "FK_DeliveryGroups_TimeSlots_TimeSlotId";
                ALTER TABLE "DeliveryGroups" DROP CONSTRAINT IF EXISTS "FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId";
                ALTER TABLE "DeliveryGroups" DROP CONSTRAINT IF EXISTS "FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId";
                ALTER TABLE "Products" DROP CONSTRAINT IF EXISTS "FK_Products_Categories_CategoryId";
                ALTER TABLE "Promotions" DROP CONSTRAINT IF EXISTS "FK_Promotions_Categories_CategoryId";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                                DECLARE
                                        default_category uuid;
                BEGIN
                                        UPDATE "Orders" o
                                        SET "AddressId" = NULL
                                        WHERE "AddressId" IS NOT NULL
                                            AND NOT EXISTS (
                                                    SELECT 1 FROM "CustomerAddresses" ca
                                                    WHERE ca."CustomerAddressId" = o."AddressId"
                                            );

                                        UPDATE "Orders" o
                                        SET "CollectionId" = NULL
                                        WHERE "CollectionId" IS NOT NULL
                                            AND NOT EXISTS (
                                                    SELECT 1 FROM "CollectionPoints" cp
                                                    WHERE cp."CollectionId" = o."CollectionId"
                                            );

                                        UPDATE "Orders" o
                                        SET "PromotionId" = NULL
                                        WHERE "PromotionId" IS NOT NULL
                                            AND NOT EXISTS (
                                                    SELECT 1 FROM "Promotions" p
                                                    WHERE p."PromotionId" = o."PromotionId"
                                            );

                                        UPDATE "Products" pr
                                        SET "CategoryId" = NULL
                                        WHERE "CategoryId" IS NOT NULL
                                            AND NOT EXISTS (
                                                    SELECT 1 FROM "Categories" c
                                                    WHERE c."CategoryId" = pr."CategoryId"
                                            );

                                        SELECT "CategoryId" INTO default_category FROM "Categories" LIMIT 1;
                                        IF default_category IS NOT NULL THEN
                                                UPDATE "Promotions" p
                                                SET "CategoryId" = default_category
                                                WHERE NOT EXISTS (
                                                        SELECT 1 FROM "Categories" c
                                                        WHERE c."CategoryId" = p."CategoryId"
                                                );
                                        END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId') THEN
                        ALTER TABLE "DeliveryGroups"
                        ADD CONSTRAINT "FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId"
                        FOREIGN KEY ("TimeSlotId") REFERENCES "DeliveryTimeSlots" ("DeliveryTimeSlotId") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_CollectionPoints_CollectionId') THEN
                        ALTER TABLE "Orders"
                        ADD CONSTRAINT "FK_Orders_CollectionPoints_CollectionId"
                        FOREIGN KEY ("CollectionId") REFERENCES "CollectionPoints" ("CollectionId") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId') THEN
                        ALTER TABLE "Orders"
                        ADD CONSTRAINT "FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId"
                        FOREIGN KEY ("DeliveryTimeSlotId") REFERENCES "DeliveryTimeSlots" ("DeliveryTimeSlotId") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_CustomerAddresses_AddressId') THEN
                        ALTER TABLE "Orders"
                        ADD CONSTRAINT "FK_Orders_CustomerAddresses_AddressId"
                        FOREIGN KEY ("AddressId") REFERENCES "CustomerAddresses" ("CustomerAddressId") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Orders_Promotions_PromotionId') THEN
                        ALTER TABLE "Orders"
                        ADD CONSTRAINT "FK_Orders_Promotions_PromotionId"
                        FOREIGN KEY ("PromotionId") REFERENCES "Promotions" ("PromotionId");
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Products_Categories_CategoryId') THEN
                        ALTER TABLE "Products"
                        ADD CONSTRAINT "FK_Products_Categories_CategoryId"
                        FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("CategoryId") ON DELETE RESTRICT;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Promotions_Categories_CategoryId') THEN
                        ALTER TABLE "Promotions"
                        ADD CONSTRAINT "FK_Promotions_Categories_CategoryId"
                        FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("CategoryId") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_CollectionPoints_CollectionId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_CustomerAddresses_AddressId";
                ALTER TABLE "Orders" DROP CONSTRAINT IF EXISTS "FK_Orders_Promotions_PromotionId";
                ALTER TABLE "DeliveryGroups" DROP CONSTRAINT IF EXISTS "FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId";
                ALTER TABLE "Products" DROP CONSTRAINT IF EXISTS "FK_Products_Categories_CategoryId";
                ALTER TABLE "Promotions" DROP CONSTRAINT IF EXISTS "FK_Promotions_Categories_CategoryId";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='DeliveryAddress') THEN
                        ALTER TABLE "Orders" ADD COLUMN "DeliveryAddress" text NULL;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='CollectionId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='PickupPointId') THEN
                        ALTER TABLE "Orders" RENAME COLUMN "CollectionId" TO "PickupPointId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Orders_CollectionId')
                       AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_Orders_PickupPointId') THEN
                        ALTER INDEX "IX_Orders_CollectionId" RENAME TO "IX_Orders_PickupPointId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='TimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='DeliveryTimeSlotId') THEN
                        ALTER TABLE "DeliveryGroups" RENAME COLUMN "TimeSlotId" TO "DeliveryTimeSlotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_DeliveryGroups_TimeSlotId')
                       AND NOT EXISTS (SELECT 1 FROM pg_class WHERE relname = 'IX_DeliveryGroups_DeliveryTimeSlotId') THEN
                        ALTER INDEX "IX_DeliveryGroups_TimeSlotId" RENAME TO "IX_DeliveryGroups_DeliveryTimeSlotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='CollectionId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='PickupPointId') THEN
                        ALTER TABLE "CollectionPoints" RENAME COLUMN "CollectionId" TO "PickupPointId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='AddressLine')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='Address') THEN
                        ALTER TABLE "CollectionPoints" RENAME COLUMN "AddressLine" TO "Address";
                    END IF;

                    IF to_regclass('"CollectionPoints"') IS NOT NULL AND to_regclass('"CollectionPoint"') IS NULL THEN
                        ALTER TABLE "CollectionPoints" RENAME TO "CollectionPoint";
                    END IF;
                END $$;
                """);
        }
    }
}
