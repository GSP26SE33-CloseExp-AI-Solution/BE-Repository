using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncEntityAndTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Align table name with current entity: ProductLots -> StockLot (only if ProductLots exists)
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'ProductLots') THEN
    ALTER TABLE ""ProductLots"" RENAME TO ""StockLot"";
  END IF;
END $$;");

            // Rename indexes only if they exist (names can differ per database)
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_ProductLots_ProductId') THEN
    ALTER INDEX ""IX_ProductLots_ProductId"" RENAME TO ""IX_StockLot_ProductId"";
  END IF;
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_ProductLots_UnitId') THEN
    ALTER INDEX ""IX_ProductLots_UnitId"" RENAME TO ""IX_StockLot_UnitId"";
  END IF;
END $$;");

            // Align column name with InventoryDisposal entity: DestroyId -> DisposalId (only if DestroyId exists)
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'InventoryDisposals' AND column_name = 'DestroyId') THEN
    ALTER TABLE ""InventoryDisposals"" RENAME COLUMN ""DestroyId"" TO ""DisposalId"";
  END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'InventoryDisposals' AND column_name = 'DisposalId') THEN
    ALTER TABLE ""InventoryDisposals"" RENAME COLUMN ""DisposalId"" TO ""DestroyId"";
  END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_StockLot_ProductId') THEN
    ALTER INDEX ""IX_StockLot_ProductId"" RENAME TO ""IX_ProductLots_ProductId"";
  END IF;
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_StockLot_UnitId') THEN
    ALTER INDEX ""IX_StockLot_UnitId"" RENAME TO ""IX_ProductLots_UnitId"";
  END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'StockLot') THEN
    ALTER TABLE ""StockLot"" RENAME TO ""ProductLots"";
  END IF;
END $$;");
        }
    }
}
