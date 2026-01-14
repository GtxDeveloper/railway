using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tringelty.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Workers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Businesses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Businesses");
        }
    }
}
