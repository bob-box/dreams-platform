using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_UserID",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_DreamID",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_UserID",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Comment");

            migrationBuilder.AddColumn<string>(
                name: "AuthorID",
                table: "Comment",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "Comment",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "SendDreamWelcomeEmail",
                table: "Cloud",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "DreamWelcomeEmailSubject",
                table: "Cloud",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "DreamWelcomeEmailBody",
                table: "Cloud",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_AuthorID",
                table: "Comment",
                column: "AuthorID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_DreamID_CreatedAtUtc",
                table: "Comment",
                columns: new[] { "DreamID", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_AuthorID",
                table: "Comment",
                column: "AuthorID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_AuthorID",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_AuthorID",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_DreamID_CreatedAtUtc",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "AuthorID",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "Comment");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "Comment",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserID",
                table: "Comment",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<bool>(
                name: "SendDreamWelcomeEmail",
                table: "Cloud",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "DreamWelcomeEmailSubject",
                table: "Cloud",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DreamWelcomeEmailBody",
                table: "Cloud",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_DreamID",
                table: "Comment",
                column: "DreamID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_UserID",
                table: "Comment",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_UserID",
                table: "Comment",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
