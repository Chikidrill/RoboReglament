using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplineMatchPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DrawPoints",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LossPoints",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WinPoints",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disciplines_MatchPoints",
                table: "Disciplines",
                sql: "[WinPoints] BETWEEN 0 AND 100\r\n          AND [DrawPoints] BETWEEN 0 AND 100\r\n          AND [LossPoints] BETWEEN 0 AND 100\r\n          AND [WinPoints] > [DrawPoints]\r\n          AND [DrawPoints] >= [LossPoints]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Disciplines_MatchPoints",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "DrawPoints",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "LossPoints",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "WinPoints",
                table: "Disciplines");
        }
    }
}
