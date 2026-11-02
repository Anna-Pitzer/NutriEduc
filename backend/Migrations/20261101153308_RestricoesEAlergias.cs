using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RestricoesEAlergias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoricosRestricoesAlunos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RestricaoNome = table.Column<string>(type: "TEXT", nullable: false),
                    Acao = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "RestricoesAlimentares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    NomeNormalizado = table.Column<string>(type: "TEXT", nullable: false),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestricoesAlimentares", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosRestricoesAlunos_AlunoId_CriadoEmUtc",
                table: "HistoricosRestricoesAlunos",
                columns: new[] { "AlunoId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RestricoesAlimentares_NomeNormalizado_Categoria",
                table: "RestricoesAlimentares",
                columns: new[] { "NomeNormalizado", "Categoria" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricosRestricoesAlunos");

            migrationBuilder.DropTable(
                name: "RestricoesAlimentares");
        }
    }
}
