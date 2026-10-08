using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameCloudAboutToCloudInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CloudAbout_CloudID",
                table: "CloudAbout");

            migrationBuilder.RenameTable(
                name: "CloudAbout",
                newName: "CloudInfo");

            migrationBuilder.CreateIndex(
                name: "IX_CloudInfo_CloudID",
                table: "CloudInfo",
                column: "CloudID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CloudInfo_CloudID",
                table: "CloudInfo");

            migrationBuilder.RenameTable(
                name: "CloudInfo",
                newName: "CloudAbout");

            migrationBuilder.CreateIndex(
                name: "IX_CloudAbout_CloudID",
                table: "CloudAbout",
                column: "CloudID",
                unique: true);
        }
    }
}
