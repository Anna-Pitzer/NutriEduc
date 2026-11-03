using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class PublicacaoHistoricoCardapio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PublicadoEmUtc",
                table: "Cardapios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicadoPor",
                table: "Cardapios",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RevisaoDeId",
                table: "Cardapios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HistoricosCardapios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CardapioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Evento = table.Column<int>(type: "INTEGER", nullable: false),
                    Versao = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosCardapios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricosCardapios_Cardapios_CardapioId",
                        column: x => x.CardapioId,
                        principalTable: "Cardapios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cardapios_RevisaoDeId",
                table: "Cardapios",
                column: "RevisaoDeId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosCardapios_CardapioId_CriadoEmUtc",
                table: "HistoricosCardapios",
                columns: new[] { "CardapioId", "CriadoEmUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Cardapios_Cardapios_RevisaoDeId",
                table: "Cardapios",
                column: "RevisaoDeId",
                principalTable: "Cardapios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cardapios_Cardapios_RevisaoDeId",
                table: "Cardapios");

            migrationBuilder.DropTable(
                name: "HistoricosCardapios");

            migrationBuilder.DropIndex(
                name: "IX_Cardapios_RevisaoDeId",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "PublicadoEmUtc",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "PublicadoPor",
                table: "Cardapios");

            migrationBuilder.DropColumn(
                name: "RevisaoDeId",
                table: "Cardapios");
        }
    }
}
