using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudIDToDream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dream_Cloud_CloudID",
                table: "Dream");

            migrationBuilder.AlterColumn<int>(
                name: "CloudID",
                table: "Dream",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Dream_Cloud_CloudID",
                table: "Dream",
                column: "CloudID",
                principalTable: "Cloud",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dream_Cloud_CloudID",
                table: "Dream");

            migrationBuilder.AlterColumn<int>(
                name: "CloudID",
                table: "Dream",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Dream_Cloud_CloudID",
                table: "Dream",
                column: "CloudID",
                principalTable: "Cloud",
                principalColumn: "ID");
        }
    }
}
