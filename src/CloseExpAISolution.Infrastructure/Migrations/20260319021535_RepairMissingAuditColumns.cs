using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Products' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE "Products" ADD COLUMN "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Products' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE "Products" ADD COLUMN "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ProductImages' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE "ProductImages" ADD COLUMN "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
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
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ProductImages' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE "ProductImages" DROP COLUMN "CreatedAt";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Products' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE "Products" DROP COLUMN "UpdatedAt";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Products' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE "Products" DROP COLUMN "CreatedAt";
                    END IF;
                END $$;
                """);
        }
    }
}
