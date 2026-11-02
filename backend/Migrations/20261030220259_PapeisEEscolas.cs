using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class PapeisEEscolas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Usuarios",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "Usuarios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Papel",
                table: "Usuarios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnidadeId",
                table: "Usuarios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnidadeId",
                table: "Alunos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Escolas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escolas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vinculos",
                columns: table => new
                {
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponsavelUsuarioId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vinculos", x => new { x.AlunoId, x.ResponsavelUsuarioId });
                    table.ForeignKey(
                        name: "FK_Vinculos_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vinculos_Usuarios_ResponsavelUsuarioId",
                        column: x => x.ResponsavelUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unidades_Escolas_EscolaId",
                        column: x => x.EscolaId,
                        principalTable: "Escolas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EscolaId",
                table: "Usuarios",
                column: "EscolaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UnidadeId",
                table: "Usuarios",
                column: "UnidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_UnidadeId",
                table: "Alunos",
                column: "UnidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_Nome",
                table: "Escolas",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_EscolaId_Nome",
                table: "Unidades",
                columns: new[] { "EscolaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vinculos_ResponsavelUsuarioId",
                table: "Vinculos",
                column: "ResponsavelUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alunos_Unidades_UnidadeId",
                table: "Alunos",
                column: "UnidadeId",
                principalTable: "Unidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Escolas_EscolaId",
                table: "Usuarios",
                column: "EscolaId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Unidades_UnidadeId",
                table: "Usuarios",
                column: "UnidadeId",
                principalTable: "Unidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alunos_Unidades_UnidadeId",
                table: "Alunos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Escolas_EscolaId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Unidades_UnidadeId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Unidades");

            migrationBuilder.DropTable(
                name: "Vinculos");

            migrationBuilder.DropTable(
                name: "Escolas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EscolaId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UnidadeId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_UnidadeId",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Papel",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UnidadeId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "UnidadeId",
                table: "Alunos");
        }
    }
}
