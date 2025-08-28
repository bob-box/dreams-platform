using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeWalletUniquePerCloud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallet_UserID",
                table: "Wallet");

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_UserID_CloudID",
                table: "Wallet",
                columns: new[] { "UserID", "CloudID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallet_UserID_CloudID",
                table: "Wallet");

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_UserID",
                table: "Wallet",
                column: "UserID");
        }
    }
}
