using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "Refunds" (
                    "RefundId" uuid NOT NULL,
                    "OrderId" uuid NOT NULL,
                    "TransactionId" uuid NOT NULL,
                    "Amount" numeric(18,2) NOT NULL,
                    "Reason" character varying(2000) NOT NULL,
                    "Status" integer NOT NULL,
                    "ProcessedBy" character varying(200),
                    "ProcessedAt" timestamp with time zone,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_Refunds" PRIMARY KEY ("RefundId"),
                    CONSTRAINT "FK_Refunds_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("OrderId") ON DELETE RESTRICT,
                    CONSTRAINT "FK_Refunds_Transactions_TransactionId" FOREIGN KEY ("TransactionId") REFERENCES "Transactions" ("TransactionId") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Refunds_OrderId"" ON ""Refunds"" (""OrderId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Refunds_TransactionId"" ON ""Refunds"" (""TransactionId"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Refunds"";");
        }
    }
}
