using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkshopTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_TicketCode",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "TicketCode",
                table: "Enrollments");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "Enrollments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateTable(
                name: "workshop_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    Nonce = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Signature = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workshop_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workshop_tickets_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workshop_tickets_enrollment_id",
                table: "workshop_tickets",
                column: "EnrollmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workshop_tickets");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Enrollments");

            migrationBuilder.AddColumn<string>(
                name: "TicketCode",
                table: "Enrollments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_TicketCode",
                table: "Enrollments",
                column: "TicketCode",
                unique: true);
        }
    }
}
