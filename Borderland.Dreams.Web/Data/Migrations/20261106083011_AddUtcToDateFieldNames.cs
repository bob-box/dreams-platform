using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUtcToDateFieldNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Wallet",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Tag",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "FundingRound",
                newName: "StartDateUtc");

            migrationBuilder.RenameColumn(
                name: "ProcessedDate",
                table: "FundingRound",
                newName: "ProcessedDateUtc");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "FundingRound",
                newName: "EndDateUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "FundingRound",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Fund",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Expense",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Dream",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Comment",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "DreamEditDeadline",
                table: "Cloud",
                newName: "DreamEditDeadlineUtc");

            migrationBuilder.RenameColumn(
                name: "DreamCreatePeriodStart",
                table: "Cloud",
                newName: "DreamCreatePeriodStartUtc");

            migrationBuilder.RenameColumn(
                name: "DreamCreatePeriodEnd",
                table: "Cloud",
                newName: "DreamCreatePeriodEndUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Cloud",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "BudgetItem",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AuditLog",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLog_CloudID_CreatedAt",
                table: "AuditLog",
                newName: "IX_AuditLog_CloudID_CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AspNetUsers",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "EmailSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "CloudInfo",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "CloudInfo");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Wallet",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Tag",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "StartDateUtc",
                table: "FundingRound",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "ProcessedDateUtc",
                table: "FundingRound",
                newName: "ProcessedDate");

            migrationBuilder.RenameColumn(
                name: "EndDateUtc",
                table: "FundingRound",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "FundingRound",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Fund",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Expense",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Dream",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Comment",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "DreamEditDeadlineUtc",
                table: "Cloud",
                newName: "DreamEditDeadline");

            migrationBuilder.RenameColumn(
                name: "DreamCreatePeriodStartUtc",
                table: "Cloud",
                newName: "DreamCreatePeriodStart");

            migrationBuilder.RenameColumn(
                name: "DreamCreatePeriodEndUtc",
                table: "Cloud",
                newName: "DreamCreatePeriodEnd");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Cloud",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "BudgetItem",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "AuditLog",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLog_CloudID_CreatedAtUtc",
                table: "AuditLog",
                newName: "IX_AuditLog_CloudID_CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "AspNetUsers",
                newName: "CreatedAt");
        }
    }
}
