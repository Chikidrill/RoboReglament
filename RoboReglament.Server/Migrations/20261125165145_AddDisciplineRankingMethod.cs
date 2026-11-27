using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplineRankingMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RankingMethod",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"UPDATE [Disciplines]
                  SET [RankingMethod] = 3
                  WHERE [Format] = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disciplines_RankingMethod",
                table: "Disciplines",
                sql: "(\r\n            ([Format] = 0 AND [RankingMethod] IN (0, 1, 2))\r\n            OR\r\n            ([Format] = 1 AND [RankingMethod] = 3)\r\n        )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Disciplines_RankingMethod",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "RankingMethod",
                table: "Disciplines");
        }
    }
}
