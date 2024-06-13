using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebMason_final.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePortsToServerOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OdooContainerId",
                table: "ServerOrders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OdooPort",
                table: "ServerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OdooPostgreSQLContainerId",
                table: "ServerOrders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OdooPostgreSQLPort",
                table: "ServerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RedmineContainerId",
                table: "ServerOrders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RedmineMySQLContainerId",
                table: "ServerOrders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RedmineMySQLPort",
                table: "ServerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RedminePort",
                table: "ServerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OdooContainerId",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "OdooPort",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "OdooPostgreSQLContainerId",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "OdooPostgreSQLPort",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "RedmineContainerId",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "RedmineMySQLContainerId",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "RedmineMySQLPort",
                table: "ServerOrders");

            migrationBuilder.DropColumn(
                name: "RedminePort",
                table: "ServerOrders");
        }
    }
}
