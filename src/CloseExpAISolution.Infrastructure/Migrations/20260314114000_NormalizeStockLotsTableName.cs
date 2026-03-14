using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314114000_NormalizeStockLotsTableName")]
    public partial class NormalizeStockLotsTableName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema='public' AND table_name='ProductLots'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema='public' AND table_name='StockLots'
    ) THEN
        ALTER TABLE ""ProductLots"" RENAME TO ""StockLots"";
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema='public' AND table_name='StockLot'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema='public' AND table_name='StockLots'
    ) THEN
        ALTER TABLE ""StockLot"" RENAME TO ""StockLots"";
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
