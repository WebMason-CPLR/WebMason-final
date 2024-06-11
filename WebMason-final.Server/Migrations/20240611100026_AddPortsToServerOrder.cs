using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebMason_final.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPortsToServerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MySQLPort",
                table: "ServerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WordPressPort",
                table: "ServerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MySQLPort",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "WordPressPort",
                table: "ServerOrders");
        }
    }
}
