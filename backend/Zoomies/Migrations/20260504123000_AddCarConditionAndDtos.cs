using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoomies.Migrations
{
    /// <inheritdoc />
    public partial class AddCarConditionAndDtos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Used");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Cars");
        }
    }
}
