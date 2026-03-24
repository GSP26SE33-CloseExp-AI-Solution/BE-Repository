using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairPricingHistoryColumnCasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('"PricingHistories"') IS NULL THEN
                        RETURN;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='lotId')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='LotId') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "lotId" TO "LotId";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='originalPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='OriginalPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "originalPrice" TO "OriginalPrice";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='suggestedUnitPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SuggestedUnitPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "suggestedUnitPrice" TO "SuggestedUnitPrice";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='finalPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='FinalPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "finalPrice" TO "FinalPrice";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='marketMinPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='MarketMinPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "marketMinPrice" TO "MarketMinPrice";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='marketMaxPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='MarketMaxPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "marketMaxPrice" TO "MarketMaxPrice";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='marketAvgPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='MarketAvgPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "marketAvgPrice" TO "MarketAvgPrice";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='aiConfidence')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='AIConfidence') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "aiConfidence" TO "AIConfidence";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='acceptedSuggestion')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='AcceptedSuggestion') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "acceptedSuggestion" TO "AcceptedSuggestion";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='staffFeedback')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='StaffFeedback') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "staffFeedback" TO "StaffFeedback";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='confirmedBy')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='ConfirmedBy') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "confirmedBy" TO "ConfirmedBy";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='confirmedAt')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='ConfirmedAt') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "confirmedAt" TO "ConfirmedAt";
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='createdAt')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='CreatedAt') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "createdAt" TO "CreatedAt";
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SuggestedUnitPrice') THEN
                        ALTER TABLE "PricingHistories" ADD COLUMN "SuggestedUnitPrice" numeric NOT NULL DEFAULT 0;
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
                    IF to_regclass('"PricingHistories"') IS NULL THEN
                        RETURN;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SuggestedUnitPrice')
                       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='suggestedUnitPrice') THEN
                        ALTER TABLE "PricingHistories" RENAME COLUMN "SuggestedUnitPrice" TO "suggestedUnitPrice";
                    END IF;
                END $$;
                """);
        }
    }
}
