using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tringelty.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLinked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLinked",
                table: "Workers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLinked",
                table: "Workers");
        }
    }
}
