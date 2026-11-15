using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Borderland.Dreams.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCommentID",
                table: "Comment",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ParentCommentID",
                table: "Comment",
                column: "ParentCommentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Comment_ParentCommentID",
                table: "Comment",
                column: "ParentCommentID",
                principalTable: "Comment",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Comment_ParentCommentID",
                table: "Comment");

            migrationBuilder.DropIndex(
                name: "IX_Comment_ParentCommentID",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "ParentCommentID",
                table: "Comment");
        }
    }
}
