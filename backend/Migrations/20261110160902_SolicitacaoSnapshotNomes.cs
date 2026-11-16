using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SolicitacaoSnapshotNomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlunoNome",
                table: "Solicitacoes",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardapioNome",
                table: "Solicitacoes",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            // Backfill (RNF3): recupera o nome atual para registros antigos cujo vínculo ainda
            // existe. Vínculos já excluídos (FK nula) permanecem com snapshot nulo — não
            // inventamos nome de vínculo excluído.
            migrationBuilder.Sql(@"
                UPDATE ""Solicitacoes""
                SET ""AlunoNome"" = (SELECT ""Nome"" FROM ""Alunos"" WHERE ""Alunos"".""Id"" = ""Solicitacoes"".""AlunoId"")
                WHERE ""AlunoId"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Solicitacoes""
                SET ""CardapioNome"" = (SELECT ""Nome"" FROM ""Cardapios"" WHERE ""Cardapios"".""Id"" = ""Solicitacoes"".""CardapioId"")
                WHERE ""CardapioId"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlunoNome",
                table: "Solicitacoes");

            migrationBuilder.DropColumn(
                name: "CardapioNome",
                table: "Solicitacoes");
        }
    }
}
