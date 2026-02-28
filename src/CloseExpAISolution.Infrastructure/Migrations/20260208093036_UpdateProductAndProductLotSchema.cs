using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductAndProductLotSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductLots_Units_UnitId",
                table: "ProductLots");

            migrationBuilder.DropIndex(
                name: "IX_ProductLots_UnitId",
                table: "ProductLots");

            // Drop columns conditionally to avoid errors if they don't exist
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    -- Drop columns from Products that were moved to ProductLot/Pricing
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Country') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""Country"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='ExpiryDate') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""ExpiryDate"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='FinalPrice') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""FinalPrice"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='ManufactureDate') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""ManufactureDate"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='OriginalPrice') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""OriginalPrice"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='PricedAt') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""PricedAt"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='PricedBy') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""PricedBy"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='PricingConfidence') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""PricingConfidence"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='PricingReasons') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""PricingReasons"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='ShelfLifeDays') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""ShelfLifeDays"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='SuggestedPrice') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""SuggestedPrice"";
                    END IF;
                    -- Drop old columns from AddPricingEntity that are replaced/removed
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Nutrition') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""Nutrition"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='ResponsibleOrg') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""ResponsibleOrg"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Usage') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""Usage"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Warning') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""Warning"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Type') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""Type"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Tags') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""Tags"";
                    END IF;
                END $$;");

            // Drop old columns from AddProductDetailFields that were renamed/removed (only if they exist)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='NetWeight') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""NetWeight"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='ResponsibleOrganization') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""ResponsibleOrganization"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='SafetyWarnings') THEN
                        ALTER TABLE ""Products"" DROP COLUMN ""SafetyWarnings"";
                    END IF;
                END $$;");

            // Drop columns from ProductLots conditionally
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='FinalUnitPrice') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""FinalUnitPrice"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='OriginalUnitPrice') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""OriginalUnitPrice"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='PricedAt') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""PricedAt"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='PricedBy') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""PricedBy"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='PricingConfidence') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""PricingConfidence"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='PricingReasons') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""PricingReasons"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='SuggestedUnitPrice') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""SuggestedUnitPrice"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='UnitId') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""UnitId"";
                    END IF;
                END $$;");

            // Drop RemainingWeight/TotalWeight if they exist (from AddPricingEntity)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='RemainingWeight') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""RemainingWeight"";
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='ProductLots' AND column_name='TotalWeight') THEN
                        ALTER TABLE ""ProductLots"" DROP COLUMN ""TotalWeight"";
                    END IF;
                END $$;");

            migrationBuilder.RenameColumn(
                name: "WeightType",
                table: "Products",
                newName: "QuantityType");

            migrationBuilder.RenameColumn(
                name: "Weight",
                table: "Products",
                newName: "MadeInCountry");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "isPrimary",
                table: "ProductImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Pricings table already exists from AddPricingEntity - add new columns only
            migrationBuilder.AddColumn<string>(
                name: "PricedBy",
                table: "Pricings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "PricedAt",
                table: "Pricings",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<float>(
                name: "PricingConfidence",
                table: "Pricings",
                type: "real",
                nullable: false,
                defaultValue: 0f);
            migrationBuilder.AddColumn<string>(
                name: "PricingReasons",
                table: "Pricings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "Pricings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedUnitPrice",
                table: "Pricings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitPrice",
                table: "Pricings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Units_UnitId",
                table: "Products");

            // Remove columns added to Pricings (don't drop table - AddPricingEntity owns it)
            migrationBuilder.DropColumn(name: "PricedBy", table: "Pricings");
            migrationBuilder.DropColumn(name: "PricedAt", table: "Pricings");
            migrationBuilder.DropColumn(name: "PricingConfidence", table: "Pricings");
            migrationBuilder.DropColumn(name: "PricingReasons", table: "Pricings");
            migrationBuilder.DropColumn(name: "OriginalUnitPrice", table: "Pricings");
            migrationBuilder.DropColumn(name: "SuggestedUnitPrice", table: "Pricings");
            migrationBuilder.DropColumn(name: "FinalUnitPrice", table: "Pricings");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "isPrimary",
                table: "ProductImages");

            migrationBuilder.RenameColumn(
                name: "QuantityType",
                table: "Products",
                newName: "WeightType");

            migrationBuilder.RenameColumn(
                name: "MadeInCountry",
                table: "Products",
                newName: "Weight");

            // Re-add RemainingWeight/TotalWeight if needed for rollback
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingWeight",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            // Re-add old columns that were dropped
            migrationBuilder.AddColumn<string>(
                name: "Nutrition",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleOrg",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Usage",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Warning",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // UpdatedAt, isActive, isFeatured are kept - don't re-add them

            migrationBuilder.AddColumn<string[]>(
                name: "Tags",
                table: "Products",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "NetWeight",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleOrganization",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafetyWarnings",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufactureDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PricedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricedBy",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PricingConfidence",
                table: "Products",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "PricingReasons",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfLifeDays",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedPrice",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalUnitPrice",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PricedAt",
                table: "ProductLots",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricedBy",
                table: "ProductLots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PricingConfidence",
                table: "ProductLots",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "PricingReasons",
                table: "ProductLots",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SuggestedUnitPrice",
                table: "ProductLots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "ProductLots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProductLots_UnitId",
                table: "ProductLots",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLots_Units_UnitId",
                table: "ProductLots",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
