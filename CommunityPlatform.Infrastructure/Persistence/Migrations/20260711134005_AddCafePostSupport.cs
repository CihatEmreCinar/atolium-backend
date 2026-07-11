using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCafePostSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "WorkshopId",
                table: "posts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployerId",
                table: "posts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "AuthorType",
                table: "posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Employer");

            migrationBuilder.AddColumn<Guid>(
                name: "CafeId",
                table: "posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "posts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Public");

            migrationBuilder.CreateIndex(
                name: "ix_posts_cafe_id",
                table: "posts",
                column: "CafeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Post_AuthorConsistency",
                table: "posts",
                sql: "(\"AuthorType\" = 'Employer' AND \"EmployerId\" IS NOT NULL AND \"CafeId\" IS NULL) OR (\"AuthorType\" = 'Cafe' AND \"CafeId\" IS NOT NULL AND \"EmployerId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_posts_CafeProfiles_CafeId",
                table: "posts",
                column: "CafeId",
                principalTable: "CafeProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_posts_CafeProfiles_CafeId",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "ix_posts_cafe_id",
                table: "posts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Post_AuthorConsistency",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "AuthorType",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "CafeId",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "posts");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkshopId",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployerId",
                table: "posts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
