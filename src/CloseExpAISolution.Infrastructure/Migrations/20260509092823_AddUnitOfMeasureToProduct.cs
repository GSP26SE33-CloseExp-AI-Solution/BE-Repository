using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitOfMeasureToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UnitId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM "UnitOfMeasures") THEN
                        INSERT INTO "UnitOfMeasures" ("UnitId", "Name", "Type", "Symbol", "CreatedAt", "UpdatedAt")
                        VALUES ('11111111-1111-1111-1111-111111111111', 'Unknown', 'System', 'N/A', NOW(), NOW());
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                UPDATE "Products" p
                SET "UnitId" = sl."UnitId"
                FROM "StockLots" sl
                WHERE p."ProductId" = sl."ProductId"
                  AND p."UnitId" IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Products"
                SET "UnitId" = (
                    SELECT u."UnitId"
                    FROM "UnitOfMeasures" u
                    ORDER BY u."CreatedAt", u."UnitId"
                    LIMIT 1
                )
                WHERE "UnitId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                table: "Products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_UnitId",
                table: "Products",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_UnitOfMeasures_UnitId",
                table: "Products",
                column: "UnitId",
                principalTable: "UnitOfMeasures",
                principalColumn: "UnitId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_UnitOfMeasures_UnitId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_UnitId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Products");
        }
    }
}
