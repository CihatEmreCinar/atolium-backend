using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUserFollowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_post_comments_post_comments_ParentCommentId",
                table: "post_comments");

            migrationBuilder.AddForeignKey(
                name: "FK_post_comments_post_comments_ParentCommentId",
                table: "post_comments",
                column: "ParentCommentId",
                principalTable: "post_comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_post_comments_post_comments_ParentCommentId",
                table: "post_comments");

            migrationBuilder.AddForeignKey(
                name: "FK_post_comments_post_comments_ParentCommentId",
                table: "post_comments",
                column: "ParentCommentId",
                principalTable: "post_comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
