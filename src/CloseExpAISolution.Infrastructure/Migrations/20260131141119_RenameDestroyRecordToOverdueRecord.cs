using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Domain.Migrations
{
    /// <inheritdoc />
    public partial class RenameDestroyRecordToOverdueRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DestroyRecords",
                newName: "OverdueRecords");

            migrationBuilder.RenameColumn(
                name: "DestroyId",
                table: "OverdueRecords",
                newName: "OverdueId");

            migrationBuilder.RenameIndex(
                name: "PK_DestroyRecords",
                table: "OverdueRecords",
                newName: "PK_OverdueRecords");

            migrationBuilder.RenameIndex(
                name: "IX_DestroyRecords_LotId",
                table: "OverdueRecords",
                newName: "IX_OverdueRecords_LotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_OverdueRecords_LotId",
                table: "OverdueRecords",
                newName: "IX_DestroyRecords_LotId");

            migrationBuilder.RenameIndex(
                name: "PK_OverdueRecords",
                table: "OverdueRecords",
                newName: "PK_DestroyRecords");

            migrationBuilder.RenameColumn(
                name: "OverdueId",
                table: "OverdueRecords",
                newName: "DestroyId");

            migrationBuilder.RenameTable(
                name: "OverdueRecords",
                newName: "DestroyRecords");
        }
    }
}
