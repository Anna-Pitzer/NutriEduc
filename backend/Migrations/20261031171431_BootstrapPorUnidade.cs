using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class BootstrapPorUnidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesBootstrap");

            migrationBuilder.CreateTable(
                name: "ConsumosBootstrap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsumidoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumosBootstrap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumosBootstrap_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokensDeCadastro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnidadeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    CriadoPorUsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensDeCadastro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensDeCadastro_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokensDeCadastro_Usuarios_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosBootstrap_UnidadeId",
                table: "ConsumosBootstrap",
                column: "UnidadeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensDeCadastro_CriadoPorUsuarioId",
                table: "TokensDeCadastro",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensDeCadastro_Hash",
                table: "TokensDeCadastro",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensDeCadastro_UnidadeId_ExpiraEm",
                table: "TokensDeCadastro",
                columns: new[] { "UnidadeId", "ExpiraEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumosBootstrap");

            migrationBuilder.DropTable(
                name: "TokensDeCadastro");

            migrationBuilder.CreateTable(
                name: "ConfiguracoesBootstrap",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CriacaoPermitida = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesBootstrap", x => x.Id);
                });
        }
    }
}
