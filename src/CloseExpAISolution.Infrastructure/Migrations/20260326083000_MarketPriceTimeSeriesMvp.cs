using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    public partial class MarketPriceTimeSeriesMvp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_MarketPrices_Barcode_Source_StoreName"";");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_CollectedAt",
                table: "MarketPrices",
                columns: new[] { "Barcode", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName_CollectedAt",
                table: "MarketPrices",
                columns: new[] { "Barcode", "Source", "StoreName", "CollectedAt" });

            migrationBuilder.Sql(@"
DO $$
BEGIN
    BEGIN
        CREATE EXTENSION IF NOT EXISTS timescaledb;
    EXCEPTION WHEN OTHERS THEN
        RAISE NOTICE 'timescaledb extension unavailable, continue with PostgreSQL fallback';
    END;
END$$;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    BEGIN
        IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'timescaledb') THEN
            PERFORM create_hypertable('""MarketPrices""', 'CollectedAt', if_not_exists => TRUE);
        END IF;
    EXCEPTION WHEN OTHERS THEN
        RAISE NOTICE 'create_hypertable skipped';
    END;
END$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW market_price_features_24h AS
SELECT
    mp.""Barcode"" AS barcode,
    MIN(mp.""Price"") AS min_price,
    MAX(mp.""Price"") AS max_price,
    AVG(mp.""Price"")::numeric(18,2) AS avg_price,
    MAX(COALESCE(mp.""LastUpdated"", mp.""CollectedAt"")) AS freshness,
    COUNT(*)::int AS observation_count
FROM ""MarketPrices"" mp
WHERE mp.""Status"" = 0
  AND mp.""CollectedAt"" >= (NOW() AT TIME ZONE 'UTC') - INTERVAL '24 hour'
GROUP BY mp.""Barcode"";");

            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW market_price_features_7d AS
SELECT
    mp.""Barcode"" AS barcode,
    MIN(mp.""Price"") AS min_price,
    MAX(mp.""Price"") AS max_price,
    AVG(mp.""Price"")::numeric(18,2) AS avg_price,
    MAX(COALESCE(mp.""LastUpdated"", mp.""CollectedAt"")) AS freshness,
    COUNT(*)::int AS observation_count
FROM ""MarketPrices"" mp
WHERE mp.""Status"" = 0
  AND mp.""CollectedAt"" >= (NOW() AT TIME ZONE 'UTC') - INTERVAL '7 day'
GROUP BY mp.""Barcode"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS market_price_features_24h;");
            migrationBuilder.Sql(@"DROP VIEW IF EXISTS market_price_features_7d;");

            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_CollectedAt",
                table: "MarketPrices");

            migrationBuilder.DropIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName_CollectedAt",
                table: "MarketPrices");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_Barcode_Source_StoreName",
                table: "MarketPrices",
                columns: new[] { "Barcode", "Source", "StoreName" },
                unique: true);
        }
    }
}
