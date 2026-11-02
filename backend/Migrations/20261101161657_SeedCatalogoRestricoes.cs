using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <summary>
    /// Semeia o catálogo padrão de restrições e alergias (Spec 010, revisão de 2026-11-01).
    ///
    /// A tela de cadastro do catálogo foi retirada do produto por decisão do proprietário:
    /// as opções oferecidas ao aluno são fixas e vêm daqui. Os nomes são os mesmos que a
    /// interface exibia antes da Spec 010, mantendo o comportamento visível.
    ///
    /// `INSERT OR IGNORE` apoiado no índice único (NomeNormalizado, Categoria) torna a
    /// migração idempotente: rodar duas vezes não duplica nem sobrescreve um item editado
    /// pela API.
    /// </summary>
    public partial class SeedCatalogoRestricoes : Migration
    {
        private const string Inserir = @"
INSERT OR IGNORE INTO RestricoesAlimentares (Nome, NomeNormalizado, Categoria, Ativa)
VALUES
    ('Integral', 'integral', 1, 1),
    ('Intolerância à Lactose', 'intolerancia a lactose', 1, 1),
    ('APVL', 'apvl', 0, 1),
    ('Diabetes', 'diabetes', 1, 1),
    ('Intolerância ao Glúten', 'intolerancia ao gluten', 1, 1),
    ('Outros', 'outros', 1, 1);
";

        private const string Remover = @"
DELETE FROM RestricoesAlimentares
WHERE NomeNormalizado IN (
    'integral',
    'intolerancia a lactose',
    'apvl',
    'diabetes',
    'intolerancia ao gluten',
    'outros'
);
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Inserir);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Remover);
        }
    }
}
