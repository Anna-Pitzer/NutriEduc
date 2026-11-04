using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class TrilhaDeAcao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrilhaAcoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    AutorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AlvoTipo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    AlvoId = table.Column<int>(type: "INTEGER", nullable: true),
                    ValorAnterior = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ValorNovo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    EmUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrilhaAcoes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrilhaAcoes_AlvoTipo_AlvoId",
                table: "TrilhaAcoes",
                columns: new[] { "AlvoTipo", "AlvoId" });

            migrationBuilder.CreateIndex(
                name: "IX_TrilhaAcoes_EmUtc",
                table: "TrilhaAcoes",
                column: "EmUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrilhaAcoes");
        }
    }
}
