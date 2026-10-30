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
                nullable: false,
                defaultValue: "Welcome to your dream, {DreamName}! You and your co-dreamers can now work together to make it happen.");

            migrationBuilder.AddColumn<string>(
                name: "DreamWelcomeEmailSubject",
                table: "Cloud",
                type: "TEXT",
                nullable: false,
                defaultValue: "Welcome to {DreamName}");

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
