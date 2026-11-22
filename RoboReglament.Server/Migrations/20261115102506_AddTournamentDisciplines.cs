using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentDisciplines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TournamentApplications_TournamentId_TeamId",
                table: "TournamentApplications");

            migrationBuilder.DropIndex(
                name: "IX_ProtocolTemplates_TournamentId",
                table: "ProtocolTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TournamentId_Number",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "DisciplineId",
                table: "TournamentApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisciplineId",
                table: "ProtocolTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisciplineId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Disciplines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disciplines", x => x.Id);
                    table.UniqueConstraint("AK_Disciplines_TournamentId_Id", x => new { x.TournamentId, x.Id });
                    table.ForeignKey(
                        name: "FK_Disciplines_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TournamentApplications_TournamentId_DisciplineId_TeamId",
                table: "TournamentApplications",
                columns: new[] { "TournamentId", "DisciplineId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolTemplates_TournamentId_DisciplineId",
                table: "ProtocolTemplates",
                columns: new[] { "TournamentId", "DisciplineId" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId_DisciplineId_Number",
                table: "Matches",
                columns: new[] { "TournamentId", "DisciplineId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disciplines_TournamentId_Name",
                table: "Disciplines",
                columns: new[] { "TournamentId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Disciplines_TournamentId_DisciplineId",
                table: "Matches",
                columns: new[] { "TournamentId", "DisciplineId" },
                principalTable: "Disciplines",
                principalColumns: new[] { "TournamentId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProtocolTemplates_Disciplines_TournamentId_DisciplineId",
                table: "ProtocolTemplates",
                columns: new[] { "TournamentId", "DisciplineId" },
                principalTable: "Disciplines",
                principalColumns: new[] { "TournamentId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
                INSERT INTO [dbo].[Disciplines]
                    ([TournamentId], [Name])
                SELECT
                    [Id],
                    N'Основная дисциплина'
                FROM [dbo].[Tournaments];

                UPDATE a
                SET a.[DisciplineId] = d.[Id]
                FROM [dbo].[TournamentApplications] a
                INNER JOIN [dbo].[Disciplines] d
                    ON a.[TournamentId] = d.[TournamentId];

                UPDATE m
                SET m.[DisciplineId] = d.[Id]
                FROM [dbo].[Matches] m
                INNER JOIN [dbo].[Disciplines] d
                    ON m.[TournamentId] = d.[TournamentId];

                UPDATE p
                SET p.[DisciplineId] = d.[Id]
                FROM [dbo].[ProtocolTemplates] p
                INNER JOIN [dbo].[Disciplines] d
                    ON p.[TournamentId] = d.[TournamentId];
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentApplications_Disciplines_TournamentId_DisciplineId",
                table: "TournamentApplications",
                columns: new[] { "TournamentId", "DisciplineId" },
                principalTable: "Disciplines",
                principalColumns: new[] { "TournamentId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Disciplines_TournamentId_DisciplineId",
                table: "Matches");

            migrationBuilder.DropForeignKey(
                name: "FK_ProtocolTemplates_Disciplines_TournamentId_DisciplineId",
                table: "ProtocolTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentApplications_Disciplines_TournamentId_DisciplineId",
                table: "TournamentApplications");

            migrationBuilder.DropTable(
                name: "Disciplines");

            migrationBuilder.DropIndex(
                name: "IX_TournamentApplications_TournamentId_DisciplineId_TeamId",
                table: "TournamentApplications");

            migrationBuilder.DropIndex(
                name: "IX_ProtocolTemplates_TournamentId_DisciplineId",
                table: "ProtocolTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Matches_TournamentId_DisciplineId_Number",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DisciplineId",
                table: "TournamentApplications");

            migrationBuilder.DropColumn(
                name: "DisciplineId",
                table: "ProtocolTemplates");

            migrationBuilder.DropColumn(
                name: "DisciplineId",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentApplications_TournamentId_TeamId",
                table: "TournamentApplications",
                columns: new[] { "TournamentId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolTemplates_TournamentId",
                table: "ProtocolTemplates",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId_Number",
                table: "Matches",
                columns: new[] { "TournamentId", "Number" },
                unique: true);
        }
    }
}
