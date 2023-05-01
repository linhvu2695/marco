using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountryService.Migrations
{
    /// <inheritdoc />
    public partial class AddFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagDescription",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagPermalink",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlagDescription",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "FlagPermalink",
                table: "Countries");
        }
    }
}
