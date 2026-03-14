using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314115500_AlignStockLotsTableWithCurrentEntity")]
    public partial class AlignStockLotsTableWithCurrentEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='StockLots') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='UnitId') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""UnitId"" uuid NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='OriginalUnitPrice') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""OriginalUnitPrice"" numeric NOT NULL DEFAULT 0;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='SuggestedUnitPrice') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""SuggestedUnitPrice"" numeric NOT NULL DEFAULT 0;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='FinalUnitPrice') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""FinalUnitPrice"" numeric NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='Weight') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""Weight"" numeric NOT NULL DEFAULT 0;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='UpdatedAt') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='PublishedBy') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""PublishedBy"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='PublishedAt') THEN
            ALTER TABLE ""StockLots"" ADD COLUMN ""PublishedAt"" timestamp with time zone NULL;
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='StockLots' AND column_name='Status') THEN
            ALTER TABLE ""StockLots"" ALTER COLUMN ""Status"" SET DEFAULT '';
        END IF;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
