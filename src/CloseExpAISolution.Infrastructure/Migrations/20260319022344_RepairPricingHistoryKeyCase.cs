using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairPricingHistoryKeyCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('"PricingHistories"') IS NOT NULL THEN
                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'PricingHistories' AND column_name = 'PricingHistoryId')
                            AND NOT EXISTS (
                                SELECT 1 FROM information_schema.columns
                                WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId') THEN
                            ALTER TABLE "PricingHistories" RENAME COLUMN "PricingHistoryId" TO "AIPriceId";
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'PricingHistories' AND column_name = 'aiPriceId')
                            AND NOT EXISTS (
                                SELECT 1 FROM information_schema.columns
                                WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId') THEN
                            ALTER TABLE "PricingHistories" RENAME COLUMN "aiPriceId" TO "AIPriceId";
                        END IF;

                        IF EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'PricingHistories' AND column_name = 'aipriceid')
                            AND NOT EXISTS (
                                SELECT 1 FROM information_schema.columns
                                WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId') THEN
                            ALTER TABLE "PricingHistories" RENAME COLUMN "aipriceid" TO "AIPriceId";
                        END IF;
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
                    IF to_regclass('"PricingHistories"') IS NOT NULL
                       AND EXISTS (
                           SELECT 1 FROM information_schema.columns
                           WHERE table_name = 'PricingHistories' AND column_name = 'AIPriceId')
                       AND NOT EXISTS (
                           SELECT 1 FROM information_schema.columns
                           WHERE table_name = 'PricingHistories' AND column_name = 'PricingHistoryId') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "AIPriceId" TO "PricingHistoryId";
                    END IF;
                END $$;
                """);
        }
    }
}
