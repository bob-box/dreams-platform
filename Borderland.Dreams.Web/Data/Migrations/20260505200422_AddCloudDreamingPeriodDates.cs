using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudDreamingPeriodDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DreamCreatePeriodEnd",
                table: "Cloud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DreamCreatePeriodStart",
                table: "Cloud",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DreamEditDeadline",
                table: "Cloud",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DreamCreatePeriodEnd",
                table: "Cloud");

            migrationBuilder.DropColumn(
                name: "DreamCreatePeriodStart",
                table: "Cloud");

            migrationBuilder.DropColumn(
                name: "DreamEditDeadline",
                table: "Cloud");
        }
    }
}
