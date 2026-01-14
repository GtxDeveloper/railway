using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tringelty.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkersLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkedUserId",
                table: "Workers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkedUserId",
                table: "Workers");
        }
    }
}
