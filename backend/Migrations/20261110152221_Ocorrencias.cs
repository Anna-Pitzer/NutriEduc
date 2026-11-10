using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Ocorrencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ocorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CardapioId = table.Column<int>(type: "INTEGER", nullable: true),
                    RefeicaoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlimentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    AlunoNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CardapioNome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    RefeicaoNome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    AlimentoNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ocorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Refeicoes_RefeicaoId",
                        column: x => x.RefeicaoId,
                        principalTable: "Refeicoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Ocorrencias_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoricoOcorrencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OcorrenciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoOcorrencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoOcorrencias_Ocorrencias_OcorrenciaId",
                        column: x => x.OcorrenciaId,
                        principalTable: "Ocorrencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoOcorrencias_OcorrenciaId_CriadoEmUtc",
                table: "HistoricoOcorrencias",
                columns: new[] { "OcorrenciaId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_AlimentoId",
                table: "Ocorrencias",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_AlunoId_CriadoEmUtc",
                table: "Ocorrencias",
                columns: new[] { "AlunoId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_CardapioId",
                table: "Ocorrencias",
                column: "CardapioId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_EscolaId_CriadoEmUtc",
                table: "Ocorrencias",
                columns: new[] { "EscolaId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_RefeicaoId",
                table: "Ocorrencias",
                column: "RefeicaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ocorrencias_UnidadeId_CriadoEmUtc",
                table: "Ocorrencias",
                columns: new[] { "UnidadeId", "CriadoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoOcorrencias");

            migrationBuilder.DropTable(
                name: "Ocorrencias");
        }
    }
}
