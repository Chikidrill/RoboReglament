using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDisciplineJudgeAggregationMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JudgeAggregationMethod",
                table: "Disciplines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Disciplines_JudgeAggregationMethod",
                table: "Disciplines",
                sql: "[JudgeAggregationMethod] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Disciplines_JudgeAggregationMethod",
                table: "Disciplines");

            migrationBuilder.DropColumn(
                name: "JudgeAggregationMethod",
                table: "Disciplines");
        }
    }
}
