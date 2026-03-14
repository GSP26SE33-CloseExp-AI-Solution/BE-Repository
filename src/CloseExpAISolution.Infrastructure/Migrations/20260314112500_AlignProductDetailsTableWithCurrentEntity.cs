using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314112500_AlignProductDetailsTableWithCurrentEntity")]
    public partial class AlignProductDetailsTableWithCurrentEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='ProductDetails') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='ProductDetails' AND column_name='SafetyWarnings') THEN
            ALTER TABLE ""ProductDetails"" ADD COLUMN ""SafetyWarnings"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='ProductDetails' AND column_name='Manufacturer') THEN
            ALTER TABLE ""ProductDetails"" ADD COLUMN ""Manufacturer"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='ProductDetails' AND column_name='Distributor') THEN
            ALTER TABLE ""ProductDetails"" ADD COLUMN ""Distributor"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='ProductDetails' AND column_name='CountryOfOrigin') THEN
            ALTER TABLE ""ProductDetails"" ADD COLUMN ""CountryOfOrigin"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='ProductDetails' AND column_name='UsageInstructions') THEN
            ALTER TABLE ""ProductDetails"" ADD COLUMN ""UsageInstructions"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='ProductDetails' AND column_name='StorageInstructions') THEN
            ALTER TABLE ""ProductDetails"" ADD COLUMN ""StorageInstructions"" text NULL;
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
