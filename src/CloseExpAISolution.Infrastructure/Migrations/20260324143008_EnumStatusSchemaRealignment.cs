using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnumStatusSchemaRealignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualDiscountPercent",
                table: "PricingHistories");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "PricingHistories");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "PricingHistories");

            migrationBuilder.DropColumn(
                name: "DaysToExpire",
                table: "PricingHistories");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "PricingHistories");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "PricingHistories");

            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='Status') THEN ALTER TABLE ""Users"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='UserImages' AND column_name='Status') THEN ALTER TABLE ""UserImages"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Transactions' AND column_name='PaymentStatus') THEN ALTER TABLE ""Transactions"" ALTER COLUMN ""PaymentStatus"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='SupermarketStaffs' AND column_name='Status') THEN ALTER TABLE ""SupermarketStaffs"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Supermarkets' AND column_name='Status') THEN ALTER TABLE ""Supermarkets"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='StockLots' AND column_name='Status') THEN ALTER TABLE ""StockLots"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Refunds' AND column_name='Status') THEN ALTER TABLE ""Refunds"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Promotions' AND column_name='Status') THEN ALTER TABLE ""Promotions"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Products' AND column_name='Status') THEN ALTER TABLE ""Products"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='OrderStatusLogs' AND column_name='ToStatus') THEN ALTER TABLE ""OrderStatusLogs"" ALTER COLUMN ""ToStatus"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='OrderStatusLogs' AND column_name='FromStatus') THEN ALTER TABLE ""OrderStatusLogs"" ALTER COLUMN ""FromStatus"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Orders' AND column_name='Status') THEN ALTER TABLE ""Orders"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='OrderPackaging' AND column_name='Status') THEN ALTER TABLE ""OrderPackaging"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Notifications' AND column_name='Type') THEN ALTER TABLE ""Notifications"" ALTER COLUMN ""Type"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='MarketPrices' AND column_name='Status') THEN ALTER TABLE ""MarketPrices"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryLogs' AND column_name='Status') THEN ALTER TABLE ""DeliveryLogs"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");
            migrationBuilder.Sql(@"DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryGroups' AND column_name='Status') THEN ALTER TABLE ""DeliveryGroups"" ALTER COLUMN ""Status"" DROP DEFAULT; END IF; END $$;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Users"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Unverified' THEN 0
                    WHEN ""Status"" = 'PendingApproval' THEN 1
                    WHEN ""Status"" = 'Active' THEN 2
                    WHEN ""Status"" = 'Rejected' THEN 3
                    WHEN ""Status"" = 'Locked' THEN 4
                    WHEN ""Status"" = 'Banned' THEN 5
                    WHEN ""Status"" = 'Deleted' THEN 6
                    WHEN ""Status"" = 'Hidden' THEN 7
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""UserImages"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Pending' THEN 0
                    WHEN ""Status"" = 'Approved' THEN 1
                    WHEN ""Status"" = 'Rejected' THEN 2
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Transactions"" ALTER COLUMN ""PaymentStatus"" TYPE integer USING
                CASE
                    WHEN ""PaymentStatus"" ~ '^[0-9]+$' THEN ""PaymentStatus""::integer
                    WHEN ""PaymentStatus"" = 'Pending' THEN 0
                    WHEN ""PaymentStatus"" = 'Paid' THEN 1
                    WHEN ""PaymentStatus"" = 'Failed' THEN 2
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""SupermarketStaffs"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Active' THEN 0
                    WHEN ""Status"" = 'Inactive' THEN 1
                    WHEN ""Status"" = 'Terminated' THEN 2
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Supermarkets"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'PendingApproval' THEN 0
                    WHEN ""Status"" = 'Active' THEN 1
                    WHEN ""Status"" = 'Suspended' THEN 2
                    WHEN ""Status"" = 'Closed' THEN 3
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""StockLots"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Draft' THEN 0
                    WHEN ""Status"" = 'Verified' THEN 1
                    WHEN ""Status"" = 'Priced' THEN 2
                    WHEN ""Status"" = 'Published' THEN 3
                    WHEN ""Status"" = 'Active' THEN 3
                    WHEN ""Status"" = 'Expired' THEN 4
                    WHEN ""Status"" = 'SoldOut' THEN 5
                    WHEN ""Status"" = 'Hidden' THEN 6
                    WHEN ""Status"" = 'Inactive' THEN 6
                    WHEN ""Status"" = 'Deleted' THEN 7
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='StockLots' AND column_name='FinalUnitPrice') THEN ALTER TABLE ""StockLots"" ADD COLUMN ""FinalUnitPrice"" numeric; END IF; END $$;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Refunds"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Pending' THEN 0
                    WHEN ""Status"" = 'Approved' THEN 1
                    WHEN ""Status"" = 'Rejected' THEN 2
                    WHEN ""Status"" = 'Completed' THEN 3
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Promotions"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Draft' THEN 0
                    WHEN ""Status"" = 'Active' THEN 1
                    WHEN ""Status"" = 'Expired' THEN 2
                    WHEN ""Status"" = 'Disabled' THEN 3
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Products"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Draft' THEN 0
                    WHEN ""Status"" = 'Verified' THEN 1
                    WHEN ""Status"" = 'Priced' THEN 2
                    WHEN ""Status"" = 'Published' THEN 3
                    WHEN ""Status"" = 'Active' THEN 3
                    WHEN ""Status"" = 'Expired' THEN 4
                    WHEN ""Status"" = 'SoldOut' THEN 5
                    WHEN ""Status"" = 'Hidden' THEN 6
                    WHEN ""Status"" = 'Inactive' THEN 6
                    WHEN ""Status"" = 'Deleted' THEN 7
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PricingHistories' AND column_name='SupermarketStaffId') THEN ALTER TABLE ""PricingHistories"" ADD COLUMN ""SupermarketStaffId"" uuid; END IF; END $$;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""OrderStatusLogs"" ALTER COLUMN ""ToStatus"" TYPE integer USING
                CASE
                    WHEN ""ToStatus"" ~ '^[0-9]+$' THEN ""ToStatus""::integer
                    WHEN ""ToStatus"" = 'Pending' THEN 0
                    WHEN ""ToStatus"" = 'PaidProcessing' THEN 1
                    WHEN ""ToStatus"" = 'Paid_Processing' THEN 1
                    WHEN ""ToStatus"" = 'ReadyToShip' THEN 2
                    WHEN ""ToStatus"" = 'Ready_To_Ship' THEN 2
                    WHEN ""ToStatus"" = 'DeliveredWaitConfirm' THEN 3
                    WHEN ""ToStatus"" = 'Delivered_Wait_Confirm' THEN 3
                    WHEN ""ToStatus"" = 'Completed' THEN 4
                    WHEN ""ToStatus"" = 'Canceled' THEN 5
                    WHEN ""ToStatus"" = 'Cancelled' THEN 5
                    WHEN ""ToStatus"" = 'Refunded' THEN 6
                    WHEN ""ToStatus"" = 'Failed' THEN 7
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""OrderStatusLogs"" ALTER COLUMN ""FromStatus"" TYPE integer USING
                CASE
                    WHEN ""FromStatus"" ~ '^[0-9]+$' THEN ""FromStatus""::integer
                    WHEN ""FromStatus"" = 'Pending' THEN 0
                    WHEN ""FromStatus"" = 'PaidProcessing' THEN 1
                    WHEN ""FromStatus"" = 'Paid_Processing' THEN 1
                    WHEN ""FromStatus"" = 'ReadyToShip' THEN 2
                    WHEN ""FromStatus"" = 'Ready_To_Ship' THEN 2
                    WHEN ""FromStatus"" = 'DeliveredWaitConfirm' THEN 3
                    WHEN ""FromStatus"" = 'Delivered_Wait_Confirm' THEN 3
                    WHEN ""FromStatus"" = 'Completed' THEN 4
                    WHEN ""FromStatus"" = 'Canceled' THEN 5
                    WHEN ""FromStatus"" = 'Cancelled' THEN 5
                    WHEN ""FromStatus"" = 'Refunded' THEN 6
                    WHEN ""FromStatus"" = 'Failed' THEN 7
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Orders"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Pending' THEN 0
                    WHEN ""Status"" = 'PaidProcessing' THEN 1
                    WHEN ""Status"" = 'Paid_Processing' THEN 1
                    WHEN ""Status"" = 'ReadyToShip' THEN 2
                    WHEN ""Status"" = 'Ready_To_Ship' THEN 2
                    WHEN ""Status"" = 'DeliveredWaitConfirm' THEN 3
                    WHEN ""Status"" = 'Delivered_Wait_Confirm' THEN 3
                    WHEN ""Status"" = 'Completed' THEN 4
                    WHEN ""Status"" = 'Canceled' THEN 5
                    WHEN ""Status"" = 'Cancelled' THEN 5
                    WHEN ""Status"" = 'Refunded' THEN 6
                    WHEN ""Status"" = 'Failed' THEN 7
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""OrderPackaging"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Pending' THEN 0
                    WHEN ""Status"" = 'Packaging' THEN 1
                    WHEN ""Status"" = 'Collecting' THEN 1
                    WHEN ""Status"" = 'Confirmed' THEN 0
                    WHEN ""Status"" = 'Completed' THEN 2
                    WHEN ""Status"" = 'Packaged' THEN 2
                    WHEN ""Status"" = 'Failed' THEN 3
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Notifications"" ALTER COLUMN ""Type"" TYPE integer USING
                CASE
                    WHEN ""Type"" ~ '^[0-9]+$' THEN ""Type""::integer
                    WHEN ""Type"" = 'OrderUpdate' THEN 0
                    WHEN ""Type"" = 'Promotion' THEN 1
                    WHEN ""Type"" = 'SystemAlert' THEN 2
                    WHEN ""Type"" = 'DeliveryUpdate' THEN 3
                    WHEN ""Type"" = 'PriceAlert' THEN 4
                    ELSE 2
                END;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""MarketPrices"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Active' THEN 0
                    WHEN ""Status"" = 'Expired' THEN 1
                    WHEN ""Status"" = 'Invalid' THEN 2
                    ELSE 0
                END;");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='DeliveryLogs' AND column_name='Status') THEN
                        ALTER TABLE ""DeliveryLogs"" ALTER COLUMN ""Status"" TYPE integer USING
                        CASE
                            WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                            WHEN ""Status"" = 'ReadyToShip' THEN 0
                            WHEN ""Status"" = 'PickedUp' THEN 1
                            WHEN ""Status"" = 'InTransit' THEN 2
                            WHEN ""Status"" = 'DeliveredWaitConfirm' THEN 3
                            WHEN ""Status"" = 'Failed' THEN 4
                            WHEN ""Status"" = 'Completed' THEN 5
                            ELSE 0
                        END;
                    END IF;
                END $$;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""DeliveryGroups"" ALTER COLUMN ""Status"" TYPE integer USING
                CASE
                    WHEN ""Status"" ~ '^[0-9]+$' THEN ""Status""::integer
                    WHEN ""Status"" = 'Pending' THEN 0
                    WHEN ""Status"" = 'Assigned' THEN 1
                    WHEN ""Status"" = 'InTransit' THEN 2
                    WHEN ""Status"" = 'Completed' THEN 3
                    WHEN ""Status"" = 'Failed' THEN 4
                    ELSE 0
                END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalUnitPrice",
                table: "StockLots");

            migrationBuilder.DropColumn(
                name: "SupermarketStaffId",
                table: "PricingHistories");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "UserImages",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SupermarketStaffs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Supermarkets",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StockLots",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Refunds",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Promotions",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Products",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<float>(
                name: "ActualDiscountPercent",
                table: "PricingHistories",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "PricingHistories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "PricingHistories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DaysToExpire",
                table: "PricingHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "PricingHistories",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffId",
                table: "PricingHistories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "OrderStatusLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "OrderStatusLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "OrderPackaging",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MarketPrices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DeliveryLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DeliveryGroups",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
