using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314101500_AlignUnitsTableWithCurrentEntity")]
    public partial class AlignUnitsTableWithCurrentEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Units'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Units'
          AND column_name = 'Symbol'
    ) THEN
        ALTER TABLE ""Units"" ADD COLUMN ""Symbol"" text NOT NULL DEFAULT '';
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Units'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Units'
          AND column_name = 'CreatedAt'
    ) THEN
        ALTER TABLE ""Units"" ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Units'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Units'
          AND column_name = 'UpdatedAt'
    ) THEN
        ALTER TABLE ""Units"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Units'
          AND column_name = 'UpdatedAt'
    ) THEN
        ALTER TABLE ""Units"" DROP COLUMN ""UpdatedAt"";
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Units'
          AND column_name = 'CreatedAt'
    ) THEN
        ALTER TABLE ""Units"" DROP COLUMN ""CreatedAt"";
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Units'
          AND column_name = 'Symbol'
    ) THEN
        ALTER TABLE ""Units"" DROP COLUMN ""Symbol"";
    END IF;
END $$;
");
        }
    }
}
