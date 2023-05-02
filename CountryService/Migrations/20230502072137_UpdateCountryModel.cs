using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountryService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCountryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OfficialName",
                table: "Countries",
                newName: "Subregion");

            migrationBuilder.AddColumn<double>(
                name: "Area",
                table: "Countries",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "CoatOfArmsPermalink",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCodeA3",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name_Chinese",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name_Native",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name_Official",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "CoatOfArmsPermalink",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "CountryCodeA3",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Name_Chinese",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Name_Native",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Name_Official",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Countries");

            migrationBuilder.RenameColumn(
                name: "Subregion",
                table: "Countries",
                newName: "OfficialName");
        }
    }
}
