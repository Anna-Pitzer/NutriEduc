using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackNotificacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Assunto = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Mensagem = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PapelAutor = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Resposta = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RespondidoPor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_AutorId_CriadoEmUtc",
                table: "Feedbacks",
                columns: new[] { "AutorId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_EscolaId_CriadoEmUtc",
                table: "Feedbacks",
                columns: new[] { "EscolaId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_UnidadeId_CriadoEmUtc",
                table: "Feedbacks",
                columns: new[] { "UnidadeId", "CriadoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");
        }
    }
}
