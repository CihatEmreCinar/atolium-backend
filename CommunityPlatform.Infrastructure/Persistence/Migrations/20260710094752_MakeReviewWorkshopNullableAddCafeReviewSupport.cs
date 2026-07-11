using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeReviewWorkshopNullableAddCafeReviewSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_SpaceBookingId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_WorkshopId_UserId",
                table: "Reviews");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkshopId",
                table: "Reviews",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "AvgRating",
                table: "CafeProfiles",
                type: "numeric(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "CafeProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_SpaceBookingId_UserId",
                table: "Reviews",
                columns: new[] { "SpaceBookingId", "UserId" },
                unique: true,
                filter: "\"SpaceBookingId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_WorkshopId_UserId",
                table: "Reviews",
                columns: new[] { "WorkshopId", "UserId" },
                unique: true,
                filter: "\"WorkshopId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Review_RevieweeConsistency",
                table: "Reviews",
                sql: "(\"RevieweeType\" = 'Employer' AND \"WorkshopId\" IS NOT NULL AND \"SpaceBookingId\" IS NULL) OR (\"RevieweeType\" = 'Cafe' AND \"SpaceBookingId\" IS NOT NULL AND \"WorkshopId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_SpaceBookingId_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_WorkshopId_UserId",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Review_RevieweeConsistency",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "CafeProfiles");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "CafeProfiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkshopId",
                table: "Reviews",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_SpaceBookingId",
                table: "Reviews",
                column: "SpaceBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_WorkshopId_UserId",
                table: "Reviews",
                columns: new[] { "WorkshopId", "UserId" },
                unique: true);
        }
    }
}
