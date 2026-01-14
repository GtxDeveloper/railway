using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tringelty.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkersLinking2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkerInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerInvitations_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workers_LinkedUserId",
                table: "Workers",
                column: "LinkedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerInvitations_Token",
                table: "WorkerInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerInvitations_WorkerId",
                table: "WorkerInvitations",
                column: "WorkerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkerInvitations");

            migrationBuilder.DropIndex(
                name: "IX_Workers_LinkedUserId",
                table: "Workers");
        }
    }
}
