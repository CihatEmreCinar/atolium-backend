using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerBioAndProfileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "EmployerProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "EmployerProfiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                table: "EmployerProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "EmployerProfiles");
        }
    }
}
