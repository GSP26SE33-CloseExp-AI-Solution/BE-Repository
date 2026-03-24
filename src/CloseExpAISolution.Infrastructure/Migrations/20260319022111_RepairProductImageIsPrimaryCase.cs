using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairProductImageIsPrimaryCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ProductImages' AND column_name = 'isPrimary')
                        AND NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'ProductImages' AND column_name = 'IsPrimary') THEN
                        ALTER TABLE "ProductImages" RENAME COLUMN "isPrimary" TO "IsPrimary";
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ProductImages' AND column_name = 'IsPrimary') THEN
                        ALTER TABLE "ProductImages" ADD COLUMN "IsPrimary" boolean NOT NULL DEFAULT FALSE;
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
                        WHERE table_name = 'ProductImages' AND column_name = 'IsPrimary')
                        AND NOT EXISTS (
                            SELECT 1 FROM information_schema.columns
                            WHERE table_name = 'ProductImages' AND column_name = 'isPrimary') THEN
                        ALTER TABLE "ProductImages" RENAME COLUMN "IsPrimary" TO "isPrimary";
                    END IF;
                END $$;
                """);
        }
    }
}
