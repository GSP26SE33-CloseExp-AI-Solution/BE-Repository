using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314110000_RelaxLegacyProductsColumns")]
    public partial class RelaxLegacyProductsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='Brand') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""Brand"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='Category') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""Category"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='ManufactureDate') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""ManufactureDate"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='ExpiryDate') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""ExpiryDate"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='OriginalPrice') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""OriginalPrice"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='SuggestedPrice') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""SuggestedPrice"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='FinalPrice') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""FinalPrice"" DROP NOT NULL;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
