using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentOrganizers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentOrganizers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentOrganizers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentOrganizers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentOrganizers_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentOrganizers_TournamentId_UserId",
                table: "TournamentOrganizers",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentOrganizers_UserId",
                table: "TournamentOrganizers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentOrganizers");
        }
    }
}
