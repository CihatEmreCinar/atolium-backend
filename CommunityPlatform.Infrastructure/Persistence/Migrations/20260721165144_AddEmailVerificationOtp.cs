using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OtpAttemptCount",
                table: "AccountActionTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpExpiresAt",
                table: "AccountActionTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtpHash",
                table: "AccountActionTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OtpUsedAt",
                table: "AccountActionTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountActionTokens_UserId_Purpose_OtpExpiresAt",
                table: "AccountActionTokens",
                columns: new[] { "UserId", "Purpose", "OtpExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccountActionTokens_UserId_Purpose_OtpExpiresAt",
                table: "AccountActionTokens");

            migrationBuilder.DropColumn(
                name: "OtpAttemptCount",
                table: "AccountActionTokens");

            migrationBuilder.DropColumn(
                name: "OtpExpiresAt",
                table: "AccountActionTokens");

            migrationBuilder.DropColumn(
                name: "OtpHash",
                table: "AccountActionTokens");

            migrationBuilder.DropColumn(
                name: "OtpUsedAt",
                table: "AccountActionTokens");
        }
    }
}
