using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alunos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Nascimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Escola = table.Column<string>(type: "TEXT", nullable: false),
                    Serie = table.Column<string>(type: "TEXT", nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", nullable: false),
                    Restricoes = table.Column<string>(type: "TEXT", nullable: false),
                    Anafilaxia = table.Column<bool>(type: "INTEGER", nullable: false),
                    NomeResponsavel = table.Column<string>(type: "TEXT", nullable: false),
                    TelefoneResponsavel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alunos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alunos");
        }
    }
}
