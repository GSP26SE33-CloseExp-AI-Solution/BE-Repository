using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncEntitiesAfterRestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========================================================================
            // FULLY IDEMPOTENT MIGRATION — handles schema drift between snapshot and DB
            // All operations use IF EXISTS / IF NOT EXISTS / DO blocks for safety
            // ========================================================================

            // ===== 1. SAFE DROP OPERATIONS =====
            migrationBuilder.Sql(@"ALTER TABLE ""DeliveryGroups"" DROP CONSTRAINT IF EXISTS ""FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" DROP CONSTRAINT IF EXISTS ""FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" DROP CONSTRAINT IF EXISTS ""FK_Orders_Promotions_PromotionId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP CONSTRAINT IF EXISTS ""FK_Products_UnitOfMeasures_UnitId"";");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""PriceFeedbacks"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockLots_ProductId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_SupermarketId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_UnitId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PricingHistories_LotId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_UserId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Notifications_UserId"";");

            migrationBuilder.Sql(@"ALTER TABLE ""StockLots"" DROP COLUMN IF EXISTS ""FinalUnitPrice"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""DefaultPricePerKg"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""IsActive"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""IsFreshFood"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""OcrConfidenceScore"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""OcrRawData"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""QuantityType"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Products"" DROP COLUMN IF EXISTS ""UnitId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""FinalPrice"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""OriginalPrice"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""StaffFeedback"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""SuggestedUnitPrice"";");

            // ===== 2. SAFE RENAME OPERATIONS =====
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='DeliveryTimeSlotId') THEN ALTER TABLE ""Orders"" RENAME COLUMN ""DeliveryTimeSlotId"" TO ""TimeSlotId""; END IF; END $$;");
            migrationBuilder.Sql(@"ALTER INDEX IF EXISTS ""IX_Orders_DeliveryTimeSlotId"" RENAME TO ""IX_Orders_TimeSlotId"";");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryTimeSlots' AND column_name='DeliveryTimeSlotId') THEN ALTER TABLE ""DeliveryTimeSlots"" RENAME COLUMN ""DeliveryTimeSlotId"" TO ""TimeSlotId""; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='DeliveryTimeSlotId') THEN ALTER TABLE ""DeliveryGroups"" RENAME COLUMN ""DeliveryTimeSlotId"" TO ""TimeSlotId""; END IF; END $$;");
            migrationBuilder.Sql(@"ALTER INDEX IF EXISTS ""IX_DeliveryGroups_DeliveryTimeSlotId"" RENAME TO ""IX_DeliveryGroups_TimeSlotId"";");

            // ===== 3. SAFE ALTER COLUMN (type changes) =====
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""FailedLoginCount"" TYPE smallint USING ""FailedLoginCount""::smallint;");
            migrationBuilder.Sql(@"ALTER TABLE ""Supermarkets"" ALTER COLUMN ""Longitude"" TYPE numeric(10,7) USING ""Longitude""::numeric(10,7);");
            migrationBuilder.Sql(@"ALTER TABLE ""Supermarkets"" ALTER COLUMN ""Latitude"" TYPE numeric(10,7) USING ""Latitude""::numeric(10,7);");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""Source"" TYPE character varying(50);");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""MarketMinPrice"" TYPE numeric(18,2);");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""MarketMaxPrice"" TYPE numeric(18,2);");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""MarketAvgPrice"" TYPE numeric(18,2);");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""AIConfidence"" TYPE numeric(5,4) USING ""AIConfidence""::numeric(5,4);");
            migrationBuilder.Sql(@"ALTER TABLE ""OrderItems"" ALTER COLUMN ""Quantity"" TYPE smallint USING ""Quantity""::smallint;");
            migrationBuilder.Sql(@"ALTER TABLE ""MarketPrices"" ALTER COLUMN ""Confidence"" TYPE numeric(5,4) USING ""Confidence""::numeric(5,4);");
            migrationBuilder.Sql(@"ALTER TABLE ""Feedbacks"" ALTER COLUMN ""Rating"" TYPE smallint USING ""Rating""::smallint;");

            // ===== 4. SAFE ADD COLUMN (IF NOT EXISTS) =====
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='UserImages' AND column_name='Status') THEN ALTER TABLE ""UserImages"" ADD COLUMN ""Status"" text NOT NULL DEFAULT ''; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Promotions' AND column_name='MaxUsage') THEN ALTER TABLE ""Promotions"" ADD COLUMN ""MaxUsage"" integer NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Promotions' AND column_name='UsedCount') THEN ALTER TABLE ""Promotions"" ADD COLUMN ""UsedCount"" integer NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='StockLots' AND column_name='FinalUnitPrice') THEN ALTER TABLE ""StockLots"" ADD COLUMN ""FinalUnitPrice"" numeric; END IF; END $$;");

            // PricingHistories — merged PriceFeedback fields
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='ActualDiscountPercent') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""ActualDiscountPercent"" real; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='Barcode') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""Barcode"" character varying(20); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='CategoryName') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""CategoryName"" character varying(100); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='DaysToExpire') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""DaysToExpire"" integer; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='Feedback') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""Feedback"" character varying(1000); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='MarketPriceRef') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""MarketPriceRef"" numeric(18,2); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='ProductName') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""ProductName"" character varying(500); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='RejectionReason') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""RejectionReason"" character varying(500); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='StaffId') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""StaffId"" character varying(100); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SuggestedPrice') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""SuggestedPrice"" numeric(18,2) NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SupermarketId') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""SupermarketId"" uuid; END IF; END $$;");

            // Notifications
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Notifications' AND column_name='Title') THEN ALTER TABLE ""Notifications"" ADD COLUMN ""Title"" text NOT NULL DEFAULT ''; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Notifications' AND column_name='Type') THEN ALTER TABLE ""Notifications"" ADD COLUMN ""Type"" text NOT NULL DEFAULT ''; END IF; END $$;");

            // Geo columns
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CustomerAddresses' AND column_name='Latitude') THEN ALTER TABLE ""CustomerAddresses"" ADD COLUMN ""Latitude"" numeric(10,7) NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CustomerAddresses' AND column_name='Longitude') THEN ALTER TABLE ""CustomerAddresses"" ADD COLUMN ""Longitude"" numeric(10,7) NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='Latitude') THEN ALTER TABLE ""CollectionPoints"" ADD COLUMN ""Latitude"" numeric(10,7) NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='CollectionPoints' AND column_name='Longitude') THEN ALTER TABLE ""CollectionPoints"" ADD COLUMN ""Longitude"" numeric(10,7) NOT NULL DEFAULT 0; END IF; END $$;");

            // AIVerificationLogs
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='AIVerificationLogs' AND column_name='RawData') THEN ALTER TABLE ""AIVerificationLogs"" ADD COLUMN ""RawData"" text; END IF; END $$;");

            // ===== 5. SAFE CREATE TABLE (IF NOT EXISTS) =====
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""DeliveryFeeConfigs"" (
                    ""ConfigId"" uuid NOT NULL, ""MinDistance"" numeric(10,2) NOT NULL, ""MaxDistance"" numeric(10,2) NOT NULL,
                    ""BaseFee"" numeric(18,2) NOT NULL, ""FeePerKm"" numeric(18,2) NOT NULL, ""Area"" text,
                    ""IsActive"" boolean NOT NULL, ""CreatedAt"" timestamp with time zone NOT NULL, ""UpdatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_DeliveryFeeConfigs"" PRIMARY KEY (""ConfigId"")
                );");
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""OrderStatusLogs"" (
                    ""LogId"" uuid NOT NULL, ""OrderId"" uuid NOT NULL, ""FromStatus"" text NOT NULL, ""ToStatus"" text NOT NULL,
                    ""ChangedBy"" text, ""Note"" text, ""ChangedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_OrderStatusLogs"" PRIMARY KEY (""LogId""),
                    CONSTRAINT ""FK_OrderStatusLogs_Orders_OrderId"" FOREIGN KEY (""OrderId"") REFERENCES ""Orders"" (""OrderId"") ON DELETE CASCADE
                );");
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""PromotionUsages"" (
                    ""UsageId"" uuid NOT NULL, ""PromotionId"" uuid NOT NULL, ""UserId"" uuid NOT NULL, ""OrderId"" uuid NOT NULL,
                    ""DiscountAmount"" numeric NOT NULL, ""UsedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_PromotionUsages"" PRIMARY KEY (""UsageId""),
                    CONSTRAINT ""FK_PromotionUsages_Orders_OrderId"" FOREIGN KEY (""OrderId"") REFERENCES ""Orders"" (""OrderId"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_PromotionUsages_Promotions_PromotionId"" FOREIGN KEY (""PromotionId"") REFERENCES ""Promotions"" (""PromotionId"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_PromotionUsages_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""UserId"") ON DELETE RESTRICT
                );");
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Refunds"" (
                    ""RefundId"" uuid NOT NULL, ""OrderId"" uuid NOT NULL, ""TransactionId"" uuid NOT NULL,
                    ""Amount"" numeric NOT NULL, ""Reason"" text NOT NULL, ""Status"" text NOT NULL,
                    ""ProcessedBy"" text, ""ProcessedAt"" timestamp with time zone, ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Refunds"" PRIMARY KEY (""RefundId""),
                    CONSTRAINT ""FK_Refunds_Orders_OrderId"" FOREIGN KEY (""OrderId"") REFERENCES ""Orders"" (""OrderId"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_Refunds_Transactions_TransactionId"" FOREIGN KEY (""TransactionId"") REFERENCES ""Transactions"" (""TransactionId"") ON DELETE RESTRICT
                );");

            // ===== 6. SAFE CREATE INDEX (IF NOT EXISTS) =====
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Users_Phone"" ON ""Users"" (""Phone"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Users_Status"" ON ""Users"" (""Status"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Supermarkets_Status"" ON ""Supermarkets"" (""Status"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_StockLots_ExpiryDate"" ON ""StockLots"" (""ExpiryDate"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_StockLots_ProductId_Status"" ON ""StockLots"" (""ProductId"", ""Status"");");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Products_Barcode"" ON ""Products"" (""Barcode"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Products_SupermarketId_Status"" ON ""Products"" (""SupermarketId"", ""Status"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PricingHistories_LotId_CreatedAt"" ON ""PricingHistories"" (""LotId"", ""CreatedAt"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PricingHistories_SupermarketId"" ON ""PricingHistories"" (""SupermarketId"");");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Orders_OrderCode"" ON ""Orders"" (""OrderCode"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Orders_OrderDate"" ON ""Orders"" (""OrderDate"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Orders_UserId_Status"" ON ""Orders"" (""UserId"", ""Status"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId_IsRead"" ON ""Notifications"" (""UserId"", ""IsRead"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_OrderStatusLogs_OrderId"" ON ""OrderStatusLogs"" (""OrderId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PromotionUsages_OrderId"" ON ""PromotionUsages"" (""OrderId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PromotionUsages_PromotionId_UserId"" ON ""PromotionUsages"" (""PromotionId"", ""UserId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PromotionUsages_UserId"" ON ""PromotionUsages"" (""UserId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Refunds_OrderId"" ON ""Refunds"" (""OrderId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Refunds_TransactionId"" ON ""Refunds"" (""TransactionId"");");

            // ===== 7. SAFE ADD FOREIGN KEY (IF NOT EXISTS) =====
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId') THEN ALTER TABLE ""DeliveryGroups"" ADD CONSTRAINT ""FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId"" FOREIGN KEY (""TimeSlotId"") REFERENCES ""DeliveryTimeSlots"" (""TimeSlotId"") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Orders_DeliveryTimeSlots_TimeSlotId') THEN ALTER TABLE ""Orders"" ADD CONSTRAINT ""FK_Orders_DeliveryTimeSlots_TimeSlotId"" FOREIGN KEY (""TimeSlotId"") REFERENCES ""DeliveryTimeSlots"" (""TimeSlotId"") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Orders_Promotions_PromotionId') THEN ALTER TABLE ""Orders"" ADD CONSTRAINT ""FK_Orders_Promotions_PromotionId"" FOREIGN KEY (""PromotionId"") REFERENCES ""Promotions"" (""PromotionId"") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_PricingHistories_Supermarkets_SupermarketId') THEN ALTER TABLE ""PricingHistories"" ADD CONSTRAINT ""FK_PricingHistories_Supermarkets_SupermarketId"" FOREIGN KEY (""SupermarketId"") REFERENCES ""Supermarkets"" (""SupermarketId"") ON DELETE RESTRICT; END IF; END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down() uses the same safe pattern for rollback
            migrationBuilder.Sql(@"ALTER TABLE ""DeliveryGroups"" DROP CONSTRAINT IF EXISTS ""FK_DeliveryGroups_DeliveryTimeSlots_TimeSlotId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" DROP CONSTRAINT IF EXISTS ""FK_Orders_DeliveryTimeSlots_TimeSlotId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Orders"" DROP CONSTRAINT IF EXISTS ""FK_Orders_Promotions_PromotionId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP CONSTRAINT IF EXISTS ""FK_PricingHistories_Supermarkets_SupermarketId"";");

            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""DeliveryFeeConfigs"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""OrderStatusLogs"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""PromotionUsages"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Refunds"";");

            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Email"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Phone"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Users_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Supermarkets_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockLots_ExpiryDate"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockLots_ProductId_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_Barcode"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_SupermarketId_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PricingHistories_LotId_CreatedAt"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_PricingHistories_SupermarketId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_OrderCode"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_OrderDate"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_UserId_Status"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Notifications_UserId_IsRead"";");

            migrationBuilder.Sql(@"ALTER TABLE ""UserImages"" DROP COLUMN IF EXISTS ""Status"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Promotions"" DROP COLUMN IF EXISTS ""MaxUsage"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Promotions"" DROP COLUMN IF EXISTS ""UsedCount"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""ActualDiscountPercent"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""Barcode"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""CategoryName"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""DaysToExpire"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""Feedback"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""MarketPriceRef"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""ProductName"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""RejectionReason"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""StaffId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""SuggestedPrice"";");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" DROP COLUMN IF EXISTS ""SupermarketId"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Notifications"" DROP COLUMN IF EXISTS ""Title"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Notifications"" DROP COLUMN IF EXISTS ""Type"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CustomerAddresses"" DROP COLUMN IF EXISTS ""Latitude"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CustomerAddresses"" DROP COLUMN IF EXISTS ""Longitude"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CollectionPoints"" DROP COLUMN IF EXISTS ""Latitude"";");
            migrationBuilder.Sql(@"ALTER TABLE ""CollectionPoints"" DROP COLUMN IF EXISTS ""Longitude"";");
            migrationBuilder.Sql(@"ALTER TABLE ""AIVerificationLogs"" DROP COLUMN IF EXISTS ""RawData"";");

            // Reverse renames
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='TimeSlotId') THEN ALTER TABLE ""Orders"" RENAME COLUMN ""TimeSlotId"" TO ""DeliveryTimeSlotId""; END IF; END $$;");
            migrationBuilder.Sql(@"ALTER INDEX IF EXISTS ""IX_Orders_TimeSlotId"" RENAME TO ""IX_Orders_DeliveryTimeSlotId"";");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryTimeSlots' AND column_name='TimeSlotId') THEN ALTER TABLE ""DeliveryTimeSlots"" RENAME COLUMN ""TimeSlotId"" TO ""DeliveryTimeSlotId""; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='TimeSlotId') THEN ALTER TABLE ""DeliveryGroups"" RENAME COLUMN ""TimeSlotId"" TO ""DeliveryTimeSlotId""; END IF; END $$;");
            migrationBuilder.Sql(@"ALTER INDEX IF EXISTS ""IX_DeliveryGroups_TimeSlotId"" RENAME TO ""IX_DeliveryGroups_DeliveryTimeSlotId"";");

            // Reverse type changes
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ALTER COLUMN ""FailedLoginCount"" TYPE integer USING ""FailedLoginCount""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""Supermarkets"" ALTER COLUMN ""Longitude"" TYPE numeric;");
            migrationBuilder.Sql(@"ALTER TABLE ""Supermarkets"" ALTER COLUMN ""Latitude"" TYPE numeric;");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""Source"" TYPE text;");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""MarketMinPrice"" TYPE numeric;");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""MarketMaxPrice"" TYPE numeric;");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""MarketAvgPrice"" TYPE numeric;");
            migrationBuilder.Sql(@"ALTER TABLE ""PricingHistories"" ALTER COLUMN ""AIConfidence"" TYPE real USING ""AIConfidence""::real;");
            migrationBuilder.Sql(@"ALTER TABLE ""OrderItems"" ALTER COLUMN ""Quantity"" TYPE integer USING ""Quantity""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""MarketPrices"" ALTER COLUMN ""Confidence"" TYPE real USING ""Confidence""::real;");
            migrationBuilder.Sql(@"ALTER TABLE ""Feedbacks"" ALTER COLUMN ""Rating"" TYPE integer USING ""Rating""::integer;");

            // Re-add old columns
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='StockLots' AND column_name='FinalUnitPrice') THEN ALTER TABLE ""StockLots"" ADD COLUMN ""FinalUnitPrice"" numeric; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='DefaultPricePerKg') THEN ALTER TABLE ""Products"" ADD COLUMN ""DefaultPricePerKg"" numeric; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='IsActive') THEN ALTER TABLE ""Products"" ADD COLUMN ""IsActive"" boolean NOT NULL DEFAULT false; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='IsFreshFood') THEN ALTER TABLE ""Products"" ADD COLUMN ""IsFreshFood"" boolean NOT NULL DEFAULT false; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='OcrConfidenceScore') THEN ALTER TABLE ""Products"" ADD COLUMN ""OcrConfidenceScore"" numeric; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='OcrRawData') THEN ALTER TABLE ""Products"" ADD COLUMN ""OcrRawData"" text; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='QuantityType') THEN ALTER TABLE ""Products"" ADD COLUMN ""QuantityType"" text; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='UnitId') THEN ALTER TABLE ""Products"" ADD COLUMN ""UnitId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='FinalPrice') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""FinalPrice"" numeric; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='OriginalPrice') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""OriginalPrice"" numeric NOT NULL DEFAULT 0; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='StaffFeedback') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""StaffFeedback"" text; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SuggestedUnitPrice') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""SuggestedUnitPrice"" numeric NOT NULL DEFAULT 0; END IF; END $$;");

            // Re-create PriceFeedbacks table
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""PriceFeedbacks"" (
                    ""Id"" uuid NOT NULL, ""ActualDiscountPercent"" real NOT NULL, ""Barcode"" character varying(20) NOT NULL,
                    ""Category"" character varying(100), ""CreatedAt"" timestamp with time zone NOT NULL, ""DaysToExpire"" integer NOT NULL,
                    ""FinalPrice"" numeric(18,2) NOT NULL, ""MarketPriceRef"" numeric(18,2), ""MarketPriceSource"" character varying(50),
                    ""OriginalPrice"" numeric(18,2) NOT NULL, ""ProductName"" character varying(500), ""RejectionReason"" character varying(500),
                    ""StaffFeedback"" character varying(1000), ""StaffId"" character varying(100), ""SuggestedPrice"" numeric(18,2) NOT NULL,
                    ""SupermarketId"" uuid, ""WasAccepted"" boolean NOT NULL,
                    CONSTRAINT ""PK_PriceFeedbacks"" PRIMARY KEY (""Id"")
                );");

            // Re-create old indexes
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_StockLots_ProductId"" ON ""StockLots"" (""ProductId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Products_SupermarketId"" ON ""Products"" (""SupermarketId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Products_UnitId"" ON ""Products"" (""UnitId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PricingHistories_LotId"" ON ""PricingHistories"" (""LotId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Orders_UserId"" ON ""Orders"" (""UserId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Notifications_UserId"" ON ""Notifications"" (""UserId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PriceFeedbacks_Barcode"" ON ""PriceFeedbacks"" (""Barcode"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_PriceFeedbacks_CreatedAt"" ON ""PriceFeedbacks"" (""CreatedAt"");");

            // Re-add old FKs
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId') THEN ALTER TABLE ""DeliveryGroups"" ADD CONSTRAINT ""FK_DeliveryGroups_DeliveryTimeSlots_DeliveryTimeSlotId"" FOREIGN KEY (""DeliveryTimeSlotId"") REFERENCES ""DeliveryTimeSlots"" (""DeliveryTimeSlotId"") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId') THEN ALTER TABLE ""Orders"" ADD CONSTRAINT ""FK_Orders_DeliveryTimeSlots_DeliveryTimeSlotId"" FOREIGN KEY (""DeliveryTimeSlotId"") REFERENCES ""DeliveryTimeSlots"" (""DeliveryTimeSlotId"") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Orders_Promotions_PromotionId') THEN ALTER TABLE ""Orders"" ADD CONSTRAINT ""FK_Orders_Promotions_PromotionId"" FOREIGN KEY (""PromotionId"") REFERENCES ""Promotions"" (""PromotionId""); END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='FK_Products_UnitOfMeasures_UnitId') THEN ALTER TABLE ""Products"" ADD CONSTRAINT ""FK_Products_UnitOfMeasures_UnitId"" FOREIGN KEY (""UnitId"") REFERENCES ""UnitOfMeasures"" (""UnitId"") ON DELETE RESTRICT; END IF; END $$;");
        }
    }
}
