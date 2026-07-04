using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCafeRoleAndSpaceBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevieweeType",
                table: "Reviews",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Employer");

            migrationBuilder.AddColumn<Guid>(
                name: "SpaceBookingId",
                table: "Reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CafeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CafeProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    HourlyPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Amenities = table.Column<List<string>>(type: "text[]", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceListings_CafeProfiles_CafeProfileId",
                        column: x => x.CafeProfileId,
                        principalTable: "CafeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpaceAvailabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceAvailabilities_SpaceListings_SpaceListingId",
                        column: x => x.SpaceListingId,
                        principalTable: "SpaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpaceBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpaceListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    TotalPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaceBookings_EmployerProfiles_EmployerProfileId",
                        column: x => x.EmployerProfileId,
                        principalTable: "EmployerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpaceBookings_SpaceListings_SpaceListingId",
                        column: x => x.SpaceListingId,
                        principalTable: "SpaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""SpaceBookings""
                ADD CONSTRAINT ""ExcludeOverlappingBookings""
                EXCLUDE USING gist (
                    ""SpaceListingId"" WITH =,
                    tstzrange(""StartDateTime"", ""EndDateTime"") WITH &&
                )
                WHERE (""Status"" IN ('Pending', 'Approved'));
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_SpaceBookingId",
                table: "Reviews",
                column: "SpaceBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeProfiles_UserId",
                table: "CafeProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaceAvailabilities_SpaceListingId",
                table: "SpaceAvailabilities",
                column: "SpaceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceBookings_EmployerProfileId",
                table: "SpaceBookings",
                column: "EmployerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceBookings_SpaceListingId",
                table: "SpaceBookings",
                column: "SpaceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceListings_CafeProfileId",
                table: "SpaceListings",
                column: "CafeProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_SpaceBookings_SpaceBookingId",
                table: "Reviews",
                column: "SpaceBookingId",
                principalTable: "SpaceBookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_SpaceBookings_SpaceBookingId",
                table: "Reviews");

            migrationBuilder.Sql("ALTER TABLE \"SpaceBookings\" DROP CONSTRAINT IF EXISTS \"ExcludeOverlappingBookings\";");

            migrationBuilder.DropTable(
                name: "SpaceAvailabilities");

            migrationBuilder.DropTable(
                name: "SpaceBookings");

            migrationBuilder.DropTable(
                name: "SpaceListings");

            migrationBuilder.DropTable(
                name: "CafeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_SpaceBookingId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "RevieweeType",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "SpaceBookingId",
                table: "Reviews");
        }
    }
}
