using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoboReglament.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicUserId : Migration
    {
        /// <inheritdoc />

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            // Генератор публичных ID
            migrationBuilder.CreateSequence<long>(
                name: "UserPublicIdSequence",
                schema: "dbo",
                startValue: 10000000L,
                incrementBy: 1);

            // Сначала допускаем NULL для существующих аккаунтов
            migrationBuilder.AddColumn<long>(
                name: "PublicId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            // Назначаем каждому существующему аккаунту свой ID
            migrationBuilder.Sql(@"
        UPDATE [dbo].[AspNetUsers]
        SET [PublicId] =
            NEXT VALUE FOR [dbo].[UserPublicIdSequence]
        WHERE [PublicId] IS NULL;
    ");

            // Делаем поле обязательным и добавляем генерацию
            // номера для новых пользователей
            migrationBuilder.AlterColumn<long>(
                name: "PublicId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                defaultValueSql:
                    "NEXT VALUE FOR [dbo].[UserPublicIdSequence]",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PublicId",
                table: "AspNetUsers",
                column: "PublicId",
                unique: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PublicId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "AspNetUsers");

            migrationBuilder.DropSequence(
                name: "UserPublicIdSequence",
                schema: "dbo");
        }
    }
}
