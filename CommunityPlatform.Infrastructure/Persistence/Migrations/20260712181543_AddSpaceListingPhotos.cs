using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceListingPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "space_listing_photos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OrderIndex = table.Column<short>(type: "smallint", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_space_listing_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_space_listing_photos_SpaceListings_SpaceListingId",
                        column: x => x.SpaceListingId,
                        principalTable: "SpaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_space_listing_photos_listing_order",
                table: "space_listing_photos",
                columns: new[] { "SpaceListingId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "space_listing_photos");
        }
    }
}
