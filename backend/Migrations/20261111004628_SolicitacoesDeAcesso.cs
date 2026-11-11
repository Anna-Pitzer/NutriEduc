using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SolicitacoesDeAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitacoesAcesso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoSolicitado = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CrnSolicitado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SolicitadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecididoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecididoPorId = table.Column<int>(type: "INTEGER", nullable: true),
                    DecididoPorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PapelConcedido = table.Column<int>(type: "INTEGER", nullable: true),
                    EscolaConcedidaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    JustificativaCorrecaoPapel = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesAcesso_Escolas_EscolaConcedidaId",
                        column: x => x.EscolaConcedidaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacoesAcesso_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacoesAcesso_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAcesso_EscolaConcedidaId",
                table: "SolicitacoesAcesso",
                column: "EscolaConcedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAcesso_UnidadeId_Status_SolicitadoEmUtc",
                table: "SolicitacoesAcesso",
                columns: new[] { "UnidadeId", "Status", "SolicitadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAcesso_UsuarioId",
                table: "SolicitacoesAcesso",
                column: "UsuarioId",
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitacoesAcesso");
        }
    }
}
