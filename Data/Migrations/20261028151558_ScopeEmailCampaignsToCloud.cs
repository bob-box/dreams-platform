using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScopeEmailCampaignsToCloud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CloudID",
                table: "EmailCampaign",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CloudID",
                table: "EmailCampaign");
        }
    }
}
