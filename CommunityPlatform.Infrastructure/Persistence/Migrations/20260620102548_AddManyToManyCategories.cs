using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManyToManyCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployerProfiles_Categories_CategoryId",
                table: "EmployerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Workshops_Categories_CategoryId",
                table: "Workshops");

            migrationBuilder.DropIndex(
                name: "IX_Workshops_CategoryId",
                table: "Workshops");

            migrationBuilder.DropIndex(
                name: "IX_EmployerProfiles_CategoryId",
                table: "EmployerProfiles");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Workshops");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "EmployerProfiles");

            migrationBuilder.CreateTable(
                name: "EmployerProfileCategories",
                columns: table => new
                {
                    EmployerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployerProfileCategories", x => new { x.EmployerProfileId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_EmployerProfileCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployerProfileCategories_EmployerProfiles_EmployerProfileId",
                        column: x => x.EmployerProfileId,
                        principalTable: "EmployerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopCategories",
                columns: table => new
                {
                    WorkshopId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopCategories", x => new { x.WorkshopId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_WorkshopCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkshopCategories_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployerProfileCategories_CategoryId",
                table: "EmployerProfileCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopCategories_CategoryId",
                table: "WorkshopCategories",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployerProfileCategories");

            migrationBuilder.DropTable(
                name: "WorkshopCategories");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Workshops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "EmployerProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workshops_CategoryId",
                table: "Workshops",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployerProfiles_CategoryId",
                table: "EmployerProfiles",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployerProfiles_Categories_CategoryId",
                table: "EmployerProfiles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workshops_Categories_CategoryId",
                table: "Workshops",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
