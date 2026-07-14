using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "City",
                table: "CafeProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Workshops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "Workshops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "Workshops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Workshops",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Workshops",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueName",
                table: "Workshops",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredCityId",
                table: "EmployeeProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferredDistrictId",
                table: "EmployeeProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CityId",
                table: "CafeProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DistrictId",
                table: "CafeProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "CafeProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "CafeProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_districts_cities_CityId",
                        column: x => x.CityId,
                        principalTable: "cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workshops_DistrictId",
                table: "Workshops",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "ix_workshops_city_id",
                table: "Workshops",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CityId",
                table: "Users",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DistrictId",
                table: "Users",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PreferredCityId",
                table: "EmployeeProfiles",
                column: "PreferredCityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PreferredDistrictId",
                table: "EmployeeProfiles",
                column: "PreferredDistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeProfiles_CityId",
                table: "CafeProfiles",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeProfiles_DistrictId",
                table: "CafeProfiles",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_cities_Name",
                table: "cities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_cities_PlateCode",
                table: "cities",
                column: "PlateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_districts_CityId_Name",
                table: "districts",
                columns: new[] { "CityId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_CafeProfiles_cities_CityId",
                table: "CafeProfiles",
                column: "CityId",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CafeProfiles_districts_DistrictId",
                table: "CafeProfiles",
                column: "DistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_cities_PreferredCityId",
                table: "EmployeeProfiles",
                column: "PreferredCityId",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_districts_PreferredDistrictId",
                table: "EmployeeProfiles",
                column: "PreferredDistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_cities_CityId",
                table: "Users",
                column: "CityId",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_districts_DistrictId",
                table: "Users",
                column: "DistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workshops_cities_CityId",
                table: "Workshops",
                column: "CityId",
                principalTable: "cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workshops_districts_DistrictId",
                table: "Workshops",
                column: "DistrictId",
                principalTable: "districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CafeProfiles_cities_CityId",
                table: "CafeProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CafeProfiles_districts_DistrictId",
                table: "CafeProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_cities_PreferredCityId",
                table: "EmployeeProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_districts_PreferredDistrictId",
                table: "EmployeeProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_cities_CityId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_districts_DistrictId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Workshops_cities_CityId",
                table: "Workshops");

            migrationBuilder.DropForeignKey(
                name: "FK_Workshops_districts_DistrictId",
                table: "Workshops");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropIndex(
                name: "IX_Workshops_DistrictId",
                table: "Workshops");

            migrationBuilder.DropIndex(
                name: "ix_workshops_city_id",
                table: "Workshops");

            migrationBuilder.DropIndex(
                name: "IX_Users_CityId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DistrictId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_PreferredCityId",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_PreferredDistrictId",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CafeProfiles_CityId",
                table: "CafeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CafeProfiles_DistrictId",
                table: "CafeProfiles");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "VenueName",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredCityId",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "PreferredDistrictId",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "CafeProfiles");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "CafeProfiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "CafeProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "CafeProfiles");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Workshops",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "CafeProfiles",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }
    }
}
