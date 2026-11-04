using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class NutricionistaResponsavelECrn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Crn",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutricionistaCpf",
                table: "Cardapios",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NutricionistaCrn",
                table: "Cardapios",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutricionistaEmail",
                table: "Cardapios",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NutricionistaId",
                table: "Cardapios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutricionistaNome",
                table: "Cardapios",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Crn",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "NutricionistaCpf",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "NutricionistaCrn",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "NutricionistaEmail",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "NutricionistaId",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "NutricionistaNome",
                table: "Cardapios");
        }
    }
}
