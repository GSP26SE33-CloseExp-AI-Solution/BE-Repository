using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CloseExpAISolution.Infrastructure.Context;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260314091500_RenameUserUpdateAtToUpdatedAt")]
    public partial class RenameUserUpdateAtToUpdatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Users'
          AND column_name = 'UpdateAt'
    ) THEN
        ALTER TABLE ""Users"" RENAME COLUMN ""UpdateAt"" TO ""UpdatedAt"";
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
          AND table_name = 'Users'
          AND column_name = 'UpdatedAt'
    ) THEN
        ALTER TABLE ""Users"" RENAME COLUMN ""UpdatedAt"" TO ""UpdateAt"";
    END IF;
END $$;
");
        }
    }
}
