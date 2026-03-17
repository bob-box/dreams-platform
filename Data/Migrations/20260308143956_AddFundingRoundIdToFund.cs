using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFundingRoundIdToFund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FundingRoundID",
                table: "Fund",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fund_FundingRoundID",
                table: "Fund",
                column: "FundingRoundID");

            migrationBuilder.AddForeignKey(
                name: "FK_Fund_FundingRound_FundingRoundID",
                table: "Fund",
                column: "FundingRoundID",
                principalTable: "FundingRound",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fund_FundingRound_FundingRoundID",
                table: "Fund");

            migrationBuilder.DropIndex(
                name: "IX_Fund_FundingRoundID",
                table: "Fund");

            migrationBuilder.DropColumn(
                name: "FundingRoundID",
                table: "Fund");
        }
    }
}
