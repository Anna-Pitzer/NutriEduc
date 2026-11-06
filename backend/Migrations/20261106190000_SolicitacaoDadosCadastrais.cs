using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <summary>
    /// Payload de dados cadastrais da solicitação (Spec 030, design §Persistência).
    ///
    /// Cria a tabela append-only de alterações e acrescenta a origem ao evento da trilha do
    /// aluno, para que a aplicação da solicitação seja rastreável sem uma segunda trilha.
    /// </summary>
    public partial class SolicitacaoDadosCadastrais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Origem",
                table: "HistoricosAlunos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SolicitacaoAlteracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitacaoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Campo = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorSolicitado = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ValorNoPedido = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoAlteracoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoAlteracoes_Solicitacoes_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "Solicitacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoAlteracoes_SolicitacaoId",
                table: "SolicitacaoAlteracoes",
                column: "SolicitacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitacaoAlteracoes");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "HistoricosAlunos");
        }
    }
}
