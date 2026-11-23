using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplineCompetitionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Format",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxTeamsPerSide",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MinTeamsPerSide",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ScoringMode",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SidesPerEvent",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disciplines_CompetitionRules",
                table: "Disciplines",
                sql: "(\r\n            (\r\n                ([Format] = 0 AND [SidesPerEvent] = 1)\r\n                OR\r\n                ([Format] = 1 AND [SidesPerEvent] BETWEEN 2 AND 4)\r\n            )\r\n            AND\r\n            (\r\n                (\r\n                    [ScoringMode] = 0\r\n                    AND [MinTeamsPerSide] = 1\r\n                    AND [MaxTeamsPerSide] = 1\r\n                )\r\n                OR\r\n                (\r\n                    [ScoringMode] = 1\r\n                    AND [MinTeamsPerSide] BETWEEN 2 AND 4\r\n                    AND [MaxTeamsPerSide]\r\n                        BETWEEN [MinTeamsPerSide] AND 4\r\n                )\r\n            )\r\n        )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Disciplines_CompetitionRules",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "MaxTeamsPerSide",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "MinTeamsPerSide",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "ScoringMode",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "SidesPerEvent",
                table: "Disciplines");
        }
    }
}
