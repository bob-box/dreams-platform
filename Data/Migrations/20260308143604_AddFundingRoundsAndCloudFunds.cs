using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingRoundsAndCloudFunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalFundsAvailable",
                table: "Cloud",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "FundingRound",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    CloudID = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxPercentagePerDream = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingRound", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FundingRound_Cloud_CloudID",
                        column: x => x.CloudID,
                        principalTable: "Cloud",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundingRound_CloudID",
                table: "FundingRound",
                column: "CloudID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundingRound");

            migrationBuilder.DropColumn(
                name: "TotalFundsAvailable",
                table: "Cloud");
        }
    }
}
