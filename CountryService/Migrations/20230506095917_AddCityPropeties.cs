using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountryService.Migrations
{
    /// <inheritdoc />
    public partial class AddCityPropeties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCapital",
                table: "Cities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Cities",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Cities",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCapital",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Cities");
        }
    }
}
