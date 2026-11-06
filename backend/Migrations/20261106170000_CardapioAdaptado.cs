using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <summary>
    /// Versão geral e adaptada do cardápio (Spec 029, design §Persistência).
    ///
    /// A migration é aditiva: nenhuma coluna existente é removida. Cardápios anteriores
    /// recebem <c>Tipo = Geral</c>, e as colunas de adaptação ficam nulas neles.
    ///
    /// O arquivo não tem Designer porque o snapshot do modelo é regenerado pelo
    /// `dotnet ef` quando a ferramenta estiver disponível; em runtime a migration é aplicada
    /// pelo próprio `Up`, que é o contrato lido por `Database.Migrate()`.
    /// </summary>
    public partial class CardapioAdaptado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Cardapios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrigemId",
                table: "Cardapios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "Cardapios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjuntoRestricoesChave",
                table: "Cardapios",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusAdaptacao",
                table: "Cardapios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubstituicoesDeItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CardapioId = table.Column<int>(type: "INTEGER", nullable: false),
                    RefeicaoOrdem = table.Column<int>(type: "INTEGER", nullable: false),
                    RefeicaoNome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    AlimentoOriginalId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlimentoOriginalNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AlimentoSubstitutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlimentoSubstitutoNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    QuantidadeGramas = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    RestricoesBloqueantes = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstituicoesDeItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubstituicoesDeItem_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendenciasAdaptacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CardapioId = table.Column<int>(type: "INTEGER", nullable: false),
                    RefeicaoOrdem = table.Column<int>(type: "INTEGER", nullable: false),
                    RefeicaoNome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    AlimentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlimentoNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendenciasAdaptacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendenciasAdaptacao_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfirmacoesDeIncompatibilidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CardapioId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AlimentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlimentoNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Escolas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Conjuntos = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfirmacoesDeIncompatibilidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfirmacoesDeIncompatibilidade_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cardapios_OrigemId",
                table: "Cardapios",
                column: "OrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Cardapios_EscolaId",
                table: "Cardapios",
                column: "EscolaId");

            // Uma versão adaptada vigente por (origem, escola, conjunto). Versões
            // substituídas são históricas e não bloqueiam a readaptação (RF3).
            migrationBuilder.CreateIndex(
                name: "IX_Cardapios_OrigemId_EscolaId_ConjuntoRestricoesChave",
                table: "Cardapios",
                columns: new[] { "OrigemId", "EscolaId", "ConjuntoRestricoesChave" },
                unique: true,
                filter: "\"Tipo\" = 1 AND \"Situacao\" <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_SubstituicoesDeItem_CardapioId",
                table: "SubstituicoesDeItem",
                column: "CardapioId");

            migrationBuilder.CreateIndex(
                name: "IX_PendenciasAdaptacao_CardapioId",
                table: "PendenciasAdaptacao",
                column: "CardapioId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacoesDeIncompatibilidade_CardapioId",
                table: "ConfirmacoesDeIncompatibilidade",
                column: "CardapioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cardapios_Cardapios_OrigemId",
                table: "Cardapios",
                column: "OrigemId",
                principalTable: "Cardapios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cardapios_Escolas_EscolaId",
                table: "Cardapios",
                column: "EscolaId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cardapios_Cardapios_OrigemId",
                table: "Cardapios");

            migrationBuilder.DropForeignKey(
                name: "FK_Cardapios_Escolas_EscolaId",
                table: "Cardapios");

            migrationBuilder.DropTable(
                name: "ConfirmacoesDeIncompatibilidade");

            migrationBuilder.DropTable(
                name: "PendenciasAdaptacao");

            migrationBuilder.DropTable(
                name: "SubstituicoesDeItem");

            migrationBuilder.DropIndex(
                name: "IX_Cardapios_OrigemId",
                table: "Cardapios");

            migrationBuilder.DropIndex(
                name: "IX_Cardapios_EscolaId",
                table: "Cardapios");

            migrationBuilder.DropIndex(
                name: "IX_Cardapios_OrigemId_EscolaId_ConjuntoRestricoesChave",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "OrigemId",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "ConjuntoRestricoesChave",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "StatusAdaptacao",
                table: "Cardapios");
        }
    }
}
