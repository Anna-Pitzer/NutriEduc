using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <summary>
    /// Substitui o histórico exclusivo de restrições pela trilha única do aluno
    /// (Spec 023, ADR-016).
    ///
    /// A tabela antiga não é apenas descartada: cada evento existente é copiado para
    /// `HistoricosAlunos` preservando aluno, autor e data. `Acao` (0 = Adicionada,
    /// 1 = Removida) vira `Tipo` (2 = RestricaoAdicionada, 3 = RestricaoRemovida) e o nome
    /// da restrição passa a ocupar o lado do snapshot que corresponde à ação — o mesmo
    /// contrato que a rota antiga lê.
    /// </summary>
    public partial class HistoricoDeAlteracoesDoAluno : Migration
    {
        private const string CopiarEventosAntigos = @"
INSERT INTO HistoricosAlunos
    (AlunoId, Tipo, Campo, ValorAnterior, ValorNovo, AutorNome, CriadoEmUtc)
SELECT
    AlunoId,
    CASE Acao WHEN 0 THEN 2 ELSE 3 END,
    'Restrições',
    CASE Acao WHEN 0 THEN '' ELSE RestricaoNome END,
    CASE Acao WHEN 0 THEN RestricaoNome ELSE '' END,
    AutorNome,
    CriadoEmUtc
FROM HistoricosRestricoesAlunos;
";

        private const string RestaurarEventosDeRestricao = @"
INSERT INTO HistoricosRestricoesAlunos (AlunoId, Acao, AutorNome, CriadoEmUtc, RestricaoNome)
SELECT
    AlunoId,
    CASE Tipo WHEN 2 THEN 0 ELSE 1 END,
    AutorNome,
    CriadoEmUtc,
    CASE Tipo WHEN 2 THEN ValorNovo ELSE ValorAnterior END
FROM HistoricosAlunos
WHERE Tipo IN (2, 3);
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricosAlunos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Campo = table.Column<string>(type: "TEXT", nullable: true),
                    ValorAnterior = table.Column<string>(type: "TEXT", nullable: true),
                    ValorNovo = table.Column<string>(type: "TEXT", nullable: true),
                    AutorNome = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosAlunos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosAlunos_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAlunos_AlunoId_CriadoEmUtc",
                table: "HistoricosAlunos",
                columns: new[] { "AlunoId", "CriadoEmUtc" });

            // Cópia dos eventos existentes antes de descartar a tabela de origem.
            migrationBuilder.Sql(CopiarEventosAntigos);

            migrationBuilder.DropTable(
                name: "HistoricosRestricoesAlunos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricosRestricoesAlunos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Acao = table.Column<int>(type: "INTEGER", nullable: false),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RestricaoNome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosRestricoesAlunos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosRestricoesAlunos_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosRestricoesAlunos_AlunoId_CriadoEmUtc",
                table: "HistoricosRestricoesAlunos",
                columns: new[] { "AlunoId", "CriadoEmUtc" });

            // Reversão: apenas os eventos de restrição voltam para a tabela antiga.
            migrationBuilder.Sql(RestaurarEventosDeRestricao);

            migrationBuilder.DropTable(
                name: "HistoricosAlunos");
        }
    }
}
