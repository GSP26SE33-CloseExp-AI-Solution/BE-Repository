using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloseExpAISolution.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupermarketRegistrationAndStaffExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SupermarketStaffs_SupermarketId",
                table: "SupermarketStaffs");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCodeHash",
                table: "SupermarketStaffs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCodeHint",
                table: "SupermarketStaffs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManager",
                table: "SupermarketStaffs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentSuperStaffId",
                table: "SupermarketStaffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminReviewNote",
                table: "Supermarkets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicantUserId",
                table: "Supermarkets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationReference",
                table: "Supermarkets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Supermarkets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Supermarkets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Supermarkets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Supermarkets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupermarketStaffs_ParentSuperStaffId",
                table: "SupermarketStaffs",
                column: "ParentSuperStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SupermarketStaffs_SupermarketId_UserId",
                table: "SupermarketStaffs",
                columns: new[] { "SupermarketId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Supermarkets_ApplicantUserId",
                table: "Supermarkets",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Supermarkets_ApplicationReference",
                table: "Supermarkets",
                column: "ApplicationReference",
                unique: true,
                filter: "\"ApplicationReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Supermarkets_ReviewedByUserId",
                table: "Supermarkets",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Supermarkets_Users_ApplicantUserId",
                table: "Supermarkets",
                column: "ApplicantUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Supermarkets_Users_ReviewedByUserId",
                table: "Supermarkets",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupermarketStaffs_SupermarketStaffs_ParentSuperStaffId",
                table: "SupermarketStaffs",
                column: "ParentSuperStaffId",
                principalTable: "SupermarketStaffs",
                principalColumn: "SupermarketStaffId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supermarkets_Users_ApplicantUserId",
                table: "Supermarkets");

            migrationBuilder.DropForeignKey(
                name: "FK_Supermarkets_Users_ReviewedByUserId",
                table: "Supermarkets");

            migrationBuilder.DropForeignKey(
                name: "FK_SupermarketStaffs_SupermarketStaffs_ParentSuperStaffId",
                table: "SupermarketStaffs");

            migrationBuilder.DropIndex(
                name: "IX_SupermarketStaffs_ParentSuperStaffId",
                table: "SupermarketStaffs");

            migrationBuilder.DropIndex(
                name: "IX_SupermarketStaffs_SupermarketId_UserId",
                table: "SupermarketStaffs");

            migrationBuilder.DropIndex(
                name: "IX_Supermarkets_ApplicantUserId",
                table: "Supermarkets");

            migrationBuilder.DropIndex(
                name: "IX_Supermarkets_ApplicationReference",
                table: "Supermarkets");

            migrationBuilder.DropIndex(
                name: "IX_Supermarkets_ReviewedByUserId",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "EmployeeCodeHash",
                table: "SupermarketStaffs");

            migrationBuilder.DropColumn(
                name: "EmployeeCodeHint",
                table: "SupermarketStaffs");

            migrationBuilder.DropColumn(
                name: "IsManager",
                table: "SupermarketStaffs");

            migrationBuilder.DropColumn(
                name: "ParentSuperStaffId",
                table: "SupermarketStaffs");

            migrationBuilder.DropColumn(
                name: "AdminReviewNote",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "ApplicantUserId",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "ApplicationReference",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Supermarkets");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Supermarkets");

            migrationBuilder.CreateIndex(
                name: "IX_SupermarketStaffs_SupermarketId",
                table: "SupermarketStaffs",
                column: "SupermarketId");
        }
    }
}
