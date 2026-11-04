using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class HierarquiaEscolaAluno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Escolas_Nome",
                table: "Escolas");

            migrationBuilder.AddColumn<int>(
                name: "UnidadeId",
                table: "Escolas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "Alunos",
                type: "INTEGER",
                nullable: true);

            // Backfill idempotente (Spec 027, design §Persistência):
            // 1) escola assume a unidade legada que a citava como origem;
            // 2) aluno assume a escola quando a sua unidade tem exatamente uma escola.
            // Registros ambíguos permanecem nulos e entram no diagnóstico.
            migrationBuilder.Sql(
                "UPDATE Escolas SET UnidadeId = (" +
                "  SELECT u.Id FROM Unidades u WHERE u.EscolaId = Escolas.Id ORDER BY u.Id LIMIT 1" +
                ") WHERE UnidadeId IS NULL AND EXISTS (" +
                "  SELECT 1 FROM Unidades u WHERE u.EscolaId = Escolas.Id);");

            migrationBuilder.Sql(
                "UPDATE Alunos SET EscolaId = (" +
                "  SELECT e.Id FROM Escolas e WHERE e.UnidadeId = Alunos.UnidadeId ORDER BY e.Id LIMIT 1" +
                ") WHERE EscolaId IS NULL AND UnidadeId IS NOT NULL AND (" +
                "  SELECT COUNT(*) FROM Escolas e WHERE e.UnidadeId = Alunos.UnidadeId" +
                ") = 1;");

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_UnidadeId_Nome",
                table: "Escolas",
                columns: new[] { "UnidadeId", "Nome" },
                unique: true,
                filter: "\"UnidadeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_EscolaId",
                table: "Alunos",
                column: "EscolaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alunos_Escolas_EscolaId",
                table: "Alunos",
                column: "EscolaId",
                principalTable: "Escolas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Escolas_Unidades_UnidadeId",
                table: "Escolas",
                column: "UnidadeId",
                principalTable: "Unidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alunos_Escolas_EscolaId",
                table: "Alunos");

            migrationBuilder.DropForeignKey(
                name: "FK_Escolas_Unidades_UnidadeId",
                table: "Escolas");

            migrationBuilder.DropIndex(
                name: "IX_Escolas_UnidadeId_Nome",
                table: "Escolas");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_EscolaId",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "UnidadeId",
                table: "Escolas");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "Alunos");

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_Nome",
                table: "Escolas",
                column: "Nome",
                unique: true);
        }
    }
}
