using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureCloudDreamWelcomeEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DreamWelcomeEmailBody",
                table: "Cloud",
                type: "TEXT",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "DreamWelcomeEmailSubject",
                table: "Cloud",
                type: "TEXT",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendDreamWelcomeEmail",
                table: "Cloud",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DreamWelcomeEmailBody",
                table: "Cloud");

            migrationBuilder.DropColumn(
                name: "DreamWelcomeEmailSubject",
                table: "Cloud");

            migrationBuilder.DropColumn(
                name: "SendDreamWelcomeEmail",
                table: "Cloud");
        }
    }
}
