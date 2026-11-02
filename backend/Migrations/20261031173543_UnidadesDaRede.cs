using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class UnidadesDaRede : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Unidades_Escolas_EscolaId",
                table: "Unidades");

            migrationBuilder.DropIndex(
                name: "IX_Unidades_EscolaId_Nome",
                table: "Unidades");

            migrationBuilder.AlterColumn<int>(
                name: "EscolaId",
                table: "Unidades",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_EscolaId",
                table: "Unidades",
                column: "EscolaId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_Nome",
                table: "Unidades",
                column: "Nome",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Unidades_Escolas_EscolaId",
                table: "Unidades",
                column: "EscolaId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Unidades_Escolas_EscolaId",
                table: "Unidades");

            migrationBuilder.DropIndex(
                name: "IX_Unidades_EscolaId",
                table: "Unidades");

            migrationBuilder.DropIndex(
                name: "IX_Unidades_Nome",
                table: "Unidades");

            migrationBuilder.AlterColumn<int>(
                name: "EscolaId",
                table: "Unidades",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_EscolaId_Nome",
                table: "Unidades",
                columns: new[] { "EscolaId", "Nome" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Unidades_Escolas_EscolaId",
                table: "Unidades",
                column: "EscolaId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
