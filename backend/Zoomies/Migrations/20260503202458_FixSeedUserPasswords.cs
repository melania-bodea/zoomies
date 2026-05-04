using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoomies.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedUserPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$gqfJFPQzPs3HykWpu3P6z.fpG74WvoI5SBpYjKLyMvRxH57ESHQtq");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$gqfJFPQzPs3HykWpu3P6z.fpG74WvoI5SBpYjKLyMvRxH57ESHQtq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$10$darOjZodJy21dQK2lira5eVywMpxr3Xb4cXBf7kL7DVfXdouQwiS.");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$10$darOjZodJy21dQK2lira5eVywMpxr3Xb4cXBf7kL7DVfXdouQwiS.");
        }
    }
}
