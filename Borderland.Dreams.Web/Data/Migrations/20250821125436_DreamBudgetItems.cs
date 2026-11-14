using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class DreamBudgetItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItem_DreamBudget_DreamBudgetID",
                table: "BudgetItem");

            migrationBuilder.DropForeignKey(
                name: "FK_Dream_DreamBudget_BudgetID",
                table: "Dream");

            migrationBuilder.DropForeignKey(
                name: "FK_Dreamer_AspNetUsers_UserId",
                table: "Dreamer");

            migrationBuilder.DropTable(
                name: "DreamBudget");

            migrationBuilder.DropIndex(
                name: "IX_Dream_BudgetID",
                table: "Dream");

            migrationBuilder.DropIndex(
                name: "IX_BudgetItem_DreamBudgetID",
                table: "BudgetItem");

            migrationBuilder.DropColumn(
                name: "BudgetID",
                table: "Dream");

            migrationBuilder.DropColumn(
                name: "DreamBudgetID",
                table: "BudgetItem");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Dreamer",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Dreamer_UserId",
                table: "Dreamer",
                newName: "IX_Dreamer_UserID");

            migrationBuilder.AddColumn<int>(
                name: "DreamID",
                table: "BudgetItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "BudgetItem",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetItem_DreamID",
                table: "BudgetItem",
                column: "DreamID");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItem_Dream_DreamID",
                table: "BudgetItem",
                column: "DreamID",
                principalTable: "Dream",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Dreamer_AspNetUsers_UserID",
                table: "Dreamer",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetItem_Dream_DreamID",
                table: "BudgetItem");

            migrationBuilder.DropForeignKey(
                name: "FK_Dreamer_AspNetUsers_UserID",
                table: "Dreamer");

            migrationBuilder.DropIndex(
                name: "IX_BudgetItem_DreamID",
                table: "BudgetItem");

            migrationBuilder.DropColumn(
                name: "DreamID",
                table: "BudgetItem");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BudgetItem");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Dreamer",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Dreamer_UserID",
                table: "Dreamer",
                newName: "IX_Dreamer_UserId");

            migrationBuilder.AddColumn<int>(
                name: "BudgetID",
                table: "Dream",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DreamBudgetID",
                table: "BudgetItem",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DreamBudget",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BudgetType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DreamBudget", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dream_BudgetID",
                table: "Dream",
                column: "BudgetID");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetItem_DreamBudgetID",
                table: "BudgetItem",
                column: "DreamBudgetID");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetItem_DreamBudget_DreamBudgetID",
                table: "BudgetItem",
                column: "DreamBudgetID",
                principalTable: "DreamBudget",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Dream_DreamBudget_BudgetID",
                table: "Dream",
                column: "BudgetID",
                principalTable: "DreamBudget",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Dreamer_AspNetUsers_UserId",
                table: "Dreamer",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
