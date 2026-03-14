using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314103000_AlignProductsTableWithCurrentEntity")]
    public partial class AlignProductsTableWithCurrentEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Products') THEN
        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='Sku') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""Sku"" text NOT NULL DEFAULT '';
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='UpdatedBy') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""UpdatedBy"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='UpdatedAt') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='PublishedBy') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""PublishedBy"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='PublishedAt') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""PublishedAt"" timestamp with time zone NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='IsFeatured') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""IsFeatured"" boolean NOT NULL DEFAULT FALSE;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='IsFreshFood') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""IsFreshFood"" boolean NOT NULL DEFAULT FALSE;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='IsActive') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""IsActive"" boolean NOT NULL DEFAULT TRUE;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='VerifiedBy') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""VerifiedBy"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='VerifiedAt') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""VerifiedAt"" timestamp with time zone NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='OcrConfidenceScore') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""OcrConfidenceScore"" numeric NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='OcrRawData') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""OcrRawData"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='QuantityType') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""QuantityType"" text NULL;
        END IF;

        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='DefaultPricePerKg') THEN
            ALTER TABLE ""Products"" ADD COLUMN ""DefaultPricePerKg"" numeric NULL;
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
