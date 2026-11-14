using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_UserId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_DreamTag_Dream_DreamsID",
                table: "DreamTag");

            migrationBuilder.DropForeignKey(
                name: "FK_Expense_Dream_DreamID",
                table: "Expense");

            migrationBuilder.DropForeignKey(
                name: "FK_Fund_AspNetUsers_UserId",
                table: "Fund");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Fund",
                newName: "UserID");

            migrationBuilder.RenameIndex(
                name: "IX_Fund_UserId",
                table: "Fund",
                newName: "IX_Fund_UserID");

            migrationBuilder.RenameColumn(
                name: "DreamsID",
                table: "DreamTag",
                newName: "DreamID");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Comment",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Comment",
                newName: "CreatedDate");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_UserId",
                table: "Comment",
                newName: "IX_Comment_UserID");

            migrationBuilder.AlterColumn<int>(
                name: "DreamID",
                table: "Expense",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_UserID",
                table: "Comment",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DreamTag_Dream_DreamID",
                table: "DreamTag",
                column: "DreamID",
                principalTable: "Dream",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_Dream_DreamID",
                table: "Expense",
                column: "DreamID",
                principalTable: "Dream",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Fund_AspNetUsers_UserID",
                table: "Fund",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_UserID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_DreamTag_Dream_DreamID",
                table: "DreamTag");

            migrationBuilder.DropForeignKey(
                name: "FK_Expense_Dream_DreamID",
                table: "Expense");

            migrationBuilder.DropForeignKey(
                name: "FK_Fund_AspNetUsers_UserID",
                table: "Fund");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Fund",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Fund_UserID",
                table: "Fund",
                newName: "IX_Fund_UserId");

            migrationBuilder.RenameColumn(
                name: "DreamID",
                table: "DreamTag",
                newName: "DreamsID");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Comment",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Comment",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_UserID",
                table: "Comment",
                newName: "IX_Comment_UserId");

            migrationBuilder.AlterColumn<int>(
                name: "DreamID",
                table: "Expense",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_UserId",
                table: "Comment",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DreamTag_Dream_DreamsID",
                table: "DreamTag",
                column: "DreamsID",
                principalTable: "Dream",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expense_Dream_DreamID",
                table: "Expense",
                column: "DreamID",
                principalTable: "Dream",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Fund_AspNetUsers_UserId",
                table: "Fund",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
