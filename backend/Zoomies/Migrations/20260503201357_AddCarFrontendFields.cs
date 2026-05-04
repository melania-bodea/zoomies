using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoomies.Migrations
{
    /// <inheritdoc />
    public partial class AddCarFrontendFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Cars",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "Description", "ImageUrl" },
                values: new object[] { "Sports", "Starter demo listing owned by the admin user.", "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=900" });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "Description", "ImageUrl" },
                values: new object[] { "Sports", "Demo seller listing used for buyer-to-seller contact tests.", "https://images.unsplash.com/photo-1625231334168-35067f8853ed?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=900" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Cars");

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "...");

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "...");
        }
    }
}
