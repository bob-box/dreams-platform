using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudIDToTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CloudID",
                table: "Tag",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_CloudID",
                table: "Wallet",
                column: "CloudID");

            migrationBuilder.CreateIndex(
                name: "IX_Tag_CloudID",
                table: "Tag",
                column: "CloudID");

            migrationBuilder.AddForeignKey(
                name: "FK_Tag_Cloud_CloudID",
                table: "Tag",
                column: "CloudID",
                principalTable: "Cloud",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallet_Cloud_CloudID",
                table: "Wallet",
                column: "CloudID",
                principalTable: "Cloud",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tag_Cloud_CloudID",
                table: "Tag");

            migrationBuilder.DropForeignKey(
                name: "FK_Wallet_Cloud_CloudID",
                table: "Wallet");

            migrationBuilder.DropIndex(
                name: "IX_Wallet_CloudID",
                table: "Wallet");

            migrationBuilder.DropIndex(
                name: "IX_Tag_CloudID",
                table: "Tag");

            migrationBuilder.DropColumn(
                name: "CloudID",
                table: "Tag");
        }
    }
}
