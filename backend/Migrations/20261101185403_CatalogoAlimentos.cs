using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoAlimentos : Migration
    {
        /// <summary>
        /// Semeia as categorias iniciais (Spec 011, RF11). O `INSERT OR IGNORE` apoiado no
        /// índice único em NomeNormalizado torna a execução idempotente: rodar de novo não
        /// duplica nem sobrescreve categoria renomeada pela API.
        ///
        /// Nenhum alimento é semeado: valor nutricional real exige fonte externa, que está
        /// fora do escopo da spec.
        /// </summary>
        private const string InserirCategorias = @"
INSERT OR IGNORE INTO CategoriasAlimento (Nome, NomeNormalizado, Ativa)
VALUES
    ('Cereais', 'cereais', 1),
    ('Leguminosas', 'leguminosas', 1),
    ('Hortaliças', 'hortalicas', 1),
    ('Frutas', 'frutas', 1),
    ('Carnes e ovos', 'carnes e ovos', 1),
    ('Laticínios', 'laticinios', 1);
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasAlimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    NomeNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasAlimento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    NomeNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    Valores_CaloriasKcal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Valores_CarboidratosG = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Valores_FibrasG = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Valores_GordurasG = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Valores_ProteinasG = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Valores_SodioMg = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alimentos_CategoriasAlimento_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriasAlimento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlimentosRestricoes",
                columns: table => new
                {
                    AlimentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RestricaoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlimentosRestricoes", x => new { x.AlimentoId, x.RestricaoId });
                    table.ForeignKey(
                        name: "FK_AlimentosRestricoes_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlimentosRestricoes_RestricoesAlimentares_RestricaoId",
                        column: x => x.RestricaoId,
                        principalTable: "RestricoesAlimentares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_CategoriaId",
                table: "Alimentos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_NomeNormalizado",
                table: "Alimentos",
                column: "NomeNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlimentosRestricoes_RestricaoId",
                table: "AlimentosRestricoes",
                column: "RestricaoId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasAlimento_NomeNormalizado",
                table: "CategoriasAlimento",
                column: "NomeNormalizado",
                unique: true);

            migrationBuilder.Sql(InserirCategorias);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlimentosRestricoes");

            migrationBuilder.DropTable(
                name: "Alimentos");

            migrationBuilder.DropTable(
                name: "CategoriasAlimento");
        }
    }
}
