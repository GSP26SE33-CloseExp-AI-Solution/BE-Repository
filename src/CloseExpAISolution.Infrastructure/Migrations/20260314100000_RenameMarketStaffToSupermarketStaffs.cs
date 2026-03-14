using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314100000_RenameMarketStaffToSupermarketStaffs")]
    public partial class RenameMarketStaffToSupermarketStaffs : Migration
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
          AND table_name = 'MarketStaff'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'SupermarketStaffs'
    ) THEN
        ALTER TABLE ""MarketStaff"" RENAME TO ""SupermarketStaffs"";
    END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'SupermarketStaffs'
          AND column_name = 'MarketStaffId'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'SupermarketStaffs'
          AND column_name = 'SupermarketStaffId'
    ) THEN
        ALTER TABLE ""SupermarketStaffs"" RENAME COLUMN ""MarketStaffId"" TO ""SupermarketStaffId"";
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
          AND table_name = 'SupermarketStaffs'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'SupermarketStaffs'
          AND column_name = 'Status'
    ) THEN
        ALTER TABLE ""SupermarketStaffs"" ADD COLUMN ""Status"" text NOT NULL DEFAULT '';
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
          AND table_name = 'SupermarketStaffs'
    ) AND NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename = 'SupermarketStaffs'
          AND indexname = 'IX_SupermarketStaffs_UserId'
    ) THEN
        CREATE INDEX ""IX_SupermarketStaffs_UserId"" ON ""SupermarketStaffs"" (""UserId"");
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
          AND table_name = 'SupermarketStaffs'
    ) AND NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename = 'SupermarketStaffs'
          AND indexname = 'IX_SupermarketStaffs_SupermarketId'
    ) THEN
        CREATE INDEX ""IX_SupermarketStaffs_SupermarketId"" ON ""SupermarketStaffs"" (""SupermarketId"");
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
          AND table_name = 'SupermarketStaffs'
          AND column_name = 'SupermarketStaffId'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'SupermarketStaffs'
          AND column_name = 'MarketStaffId'
    ) THEN
        ALTER TABLE ""SupermarketStaffs"" RENAME COLUMN ""SupermarketStaffId"" TO ""MarketStaffId"";
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
          AND table_name = 'SupermarketStaffs'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'MarketStaff'
    ) THEN
        ALTER TABLE ""SupermarketStaffs"" RENAME TO ""MarketStaff"";
    END IF;
END $$;
");
        }
    }
}
