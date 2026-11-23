using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchJudgeAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JudgeProtocols_MatchGroupId",
                table: "JudgeProtocols");

            migrationBuilder.CreateTable(
                name: "MatchJudges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    JudgeId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchJudges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchJudges_AspNetUsers_JudgeId",
                        column: x => x.JudgeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchJudges_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JudgeProtocols_MatchGroupId_JudgeId",
                table: "JudgeProtocols",
                columns: new[] { "MatchGroupId", "JudgeId" },
                unique: true,
                filter: "[JudgeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MatchJudges_JudgeId",
                table: "MatchJudges",
                column: "JudgeId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchJudges_MatchId_JudgeId",
                table: "MatchJudges",
                columns: new[] { "MatchId", "JudgeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchJudges");

            migrationBuilder.DropIndex(
                name: "IX_JudgeProtocols_MatchGroupId_JudgeId",
                table: "JudgeProtocols");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeProtocols_MatchGroupId",
                table: "JudgeProtocols",
                column: "MatchGroupId");
        }
    }
}
