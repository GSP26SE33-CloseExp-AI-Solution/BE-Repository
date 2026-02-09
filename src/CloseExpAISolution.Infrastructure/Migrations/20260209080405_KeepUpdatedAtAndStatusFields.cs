using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class KeepUpdatedAtAndStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sku already exists - skip it
            // migrationBuilder.AddColumn<string>(
            //     name: "Sku",
            //     table: "Products",
            //     type: "text",
            //     nullable: false,
            //     defaultValue: "");

            // Add columns only if they don't exist
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='UpdatedAt') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT '0001-01-01 00:00:00+00';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='isActive') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""isActive"" boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='isFeatured') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""isFeatured"" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sku was not added by this migration, so don't drop it
            // migrationBuilder.DropColumn(
            //     name: "Sku",
            //     table: "Products");

            // Drop columns only if they exist (and were added by this migration)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='UpdatedAt') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""UpdatedAt"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='isActive') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""isActive"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='isFeatured') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""isFeatured"";
                    END IF;
                END $$;");
        }
    }
}
