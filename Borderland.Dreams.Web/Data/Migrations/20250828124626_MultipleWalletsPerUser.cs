using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultipleWalletsPerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Wallet_WalletID",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_WalletID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WalletID",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "CloudID",
                table: "Wallet",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserID",
                table: "Wallet",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_UserID",
                table: "Wallet",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallet_AspNetUsers_UserID",
                table: "Wallet",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallet_AspNetUsers_UserID",
                table: "Wallet");

            migrationBuilder.DropIndex(
                name: "IX_Wallet_UserID",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "CloudID",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Wallet");

            migrationBuilder.AddColumn<int>(
                name: "WalletID",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_WalletID",
                table: "AspNetUsers",
                column: "WalletID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Wallet_WalletID",
                table: "AspNetUsers",
                column: "WalletID",
                principalTable: "Wallet",
                principalColumn: "ID");
        }
    }
}
