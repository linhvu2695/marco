using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountryService.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name_Native",
                table: "Countries",
                newName: "Languages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Languages",
                table: "Countries",
                newName: "Name_Native");
        }
    }
}
