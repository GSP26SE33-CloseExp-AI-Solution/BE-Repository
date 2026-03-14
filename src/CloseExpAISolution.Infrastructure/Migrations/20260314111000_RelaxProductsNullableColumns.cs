using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314111000_RelaxProductsNullableColumns")]
    public partial class RelaxProductsNullableColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='QuantityType') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""QuantityType"" DROP NOT NULL;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='Products' AND column_name='DefaultPricePerKg') THEN
        ALTER TABLE ""Products"" ALTER COLUMN ""DefaultPricePerKg"" DROP NOT NULL;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
