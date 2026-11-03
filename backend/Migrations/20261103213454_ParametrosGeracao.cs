using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ParametrosGeracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametrosGeracao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CaloriasMinimasRefeicao = table.Column<decimal>(type: "TEXT", nullable: false),
                    CaloriasMaximasRefeicao = table.Column<decimal>(type: "TEXT", nullable: false),
                    OpcoesPorRefeicao = table.Column<int>(type: "INTEGER", nullable: false),
                    JanelaRepeticaoDias = table.Column<int>(type: "INTEGER", nullable: false),
                    ExcluirAlimentosIncompativeis = table.Column<bool>(type: "INTEGER", nullable: false),
                    CategoriasObrigatorias = table.Column<string>(type: "TEXT", nullable: false),
                    RefeicoesPadrao = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosGeracao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParametrosGeracao_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParametrosGeracao_UnidadeId",
                table: "ParametrosGeracao",
                column: "UnidadeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametrosGeracao");
        }
    }
}
