using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentApplicationParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentApplicationParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentApplicationId = table.Column<int>(type: "int", nullable: false),
                    ParticipantId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentApplicationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TournamentApplicationParticipants_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TournamentApplicationParticipants_TournamentApplications_TournamentApplicationId",
                        column: x => x.TournamentApplicationId,
                        principalTable: "TournamentApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentApplicationParticipants_ParticipantId",
                table: "TournamentApplicationParticipants",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentApplicationParticipants_TournamentApplicationId_ParticipantId",
                table: "TournamentApplicationParticipants",
                columns: new[] { "TournamentApplicationId", "ParticipantId" },
                unique: true,
                filter: "[ParticipantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentApplicationParticipants");
        }
    }
}
