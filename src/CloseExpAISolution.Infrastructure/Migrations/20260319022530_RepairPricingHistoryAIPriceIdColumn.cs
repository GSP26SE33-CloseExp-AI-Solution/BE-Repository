using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairPricingHistoryAIPriceIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                DO $$
                DECLARE
                    pk_name text;
                BEGIN
                    IF to_regclass('"PricingHistories"') IS NULL THEN
                        RETURN;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId') THEN
                        ALTER TABLE "PricingHistories" ADD COLUMN "AIPriceId" uuid NULL;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'PricingHistories' AND column_name = 'PricingHistoryId') THEN
                        UPDATE "PricingHistories"
                        SET "AIPriceId" = "PricingHistoryId"
                        WHERE "AIPriceId" IS NULL;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'PricingHistories' AND column_name = 'aiPriceId') THEN
                        UPDATE "PricingHistories"
                        SET "AIPriceId" = "aiPriceId"
                        WHERE "AIPriceId" IS NULL;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'PricingHistories' AND column_name = 'aipriceid') THEN
                        EXECUTE 'UPDATE "PricingHistories" SET "AIPriceId" = "aipriceid" WHERE "AIPriceId" IS NULL';
                    END IF;

                    UPDATE "PricingHistories"
                    SET "AIPriceId" = gen_random_uuid()
                    WHERE "AIPriceId" IS NULL;

                    SELECT c.conname INTO pk_name
                    FROM pg_constraint c
                    JOIN pg_class t ON t.oid = c.conrelid
                    WHERE c.contype = 'p'
                      AND t.relname = 'PricingHistories'
                    LIMIT 1;

                    IF pk_name IS NOT NULL AND pk_name <> 'PK_PricingHistories' THEN
                        EXECUTE format('ALTER TABLE "PricingHistories" DROP CONSTRAINT %I', pk_name);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conrelid = '"PricingHistories"'::regclass
                          AND conname = 'PK_PricingHistories') THEN
                        ALTER TABLE "PricingHistories"
                        ADD CONSTRAINT "PK_PricingHistories" PRIMARY KEY ("AIPriceId");
                    END IF;

                    ALTER TABLE "PricingHistories" ALTER COLUMN "AIPriceId" SET NOT NULL;
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
                    IF to_regclass('"PricingHistories"') IS NOT NULL
                       AND EXISTS (
                           SELECT 1 FROM information_schema.columns
                           WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId') THEN
                        ALTER TABLE "PricingHistories" DROP CONSTRAINT IF EXISTS "PK_PricingHistories";
                        ALTER TABLE "PricingHistories" DROP COLUMN "AIPriceId";
                    END IF;
                END $$;
                """);
        }
    }
}
