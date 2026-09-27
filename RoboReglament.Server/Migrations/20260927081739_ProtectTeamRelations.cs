using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class ProtectTeamRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchTeams_Teams_TeamId",
                table: "MatchTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentApplications_Teams_TeamId",
                table: "TournamentApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchTeams_Teams_TeamId",
                table: "MatchTeams",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentApplications_Teams_TeamId",
                table: "TournamentApplications",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchTeams_Teams_TeamId",
                table: "MatchTeams");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentApplications_Teams_TeamId",
                table: "TournamentApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_MatchTeams_Teams_TeamId",
                table: "MatchTeams",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentApplications_Teams_TeamId",
                table: "TournamentApplications",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
