using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "IconUrl", "IsActive", "Name", "ParentId", "Slug", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, true, "Seramik", null, "seramik", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, true, "Resim", null, "resim", 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, true, "Fotoğrafçılık", null, "fotografcilik", 3 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, true, "Yazılım", null, "yazilim", 4 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, true, "Müzik", null, "muzik", 5 },
                    { new Guid("66666666-6666-6666-6666-666666666666"), null, true, "Dans", null, "dans", 6 },
                    { new Guid("77777777-7777-7777-7777-777777777777"), null, true, "Yemek", null, "yemek", 7 },
                    { new Guid("88888888-8888-8888-8888-888888888888"), null, true, "El Sanatları", null, "el-sanatlari", 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));
        }
    }
}
