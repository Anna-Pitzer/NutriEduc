using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <summary>
    /// Fluxo de solicitações (Spec 017, design §Persistência).
    ///
    /// Cria a solicitação e a trilha append-only de status. O tipo é inteiro: o valor
    /// <c>DadosCadastrais</c> entra sem migração de enum, porque o payload é da Spec 030.
    /// </summary>
    public partial class Solicitacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Solicitacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CardapioId = table.Column<int>(type: "INTEGER", nullable: true),
                    RestricaoNome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Ingrediente = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SolicitanteId = table.Column<int>(type: "INTEGER", nullable: false),
                    SolicitanteNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Resposta = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RespondidoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitacoes_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoSolicitacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitacaoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Resposta = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoSolicitacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoSolicitacoes_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_AlunoId",
                table: "Solicitacoes",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_CardapioId",
                table: "Solicitacoes",
                column: "CardapioId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_EscolaId_CriadoEmUtc",
                table: "Solicitacoes",
                columns: new[] { "EscolaId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_Status_Tipo",
                table: "Solicitacoes",
                columns: new[] { "Status", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_Solicitacoes_UnidadeId_CriadoEmUtc",
                table: "Solicitacoes",
                columns: new[] { "UnidadeId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoSolicitacoes_SolicitacaoId_CriadoEmUtc",
                table: "HistoricoSolicitacoes",
                columns: new[] { "SolicitacaoId", "CriadoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoSolicitacoes");

            migrationBuilder.DropTable(
                name: "Solicitacoes");
        }
    }
}
