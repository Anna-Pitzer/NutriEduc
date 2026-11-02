using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class HashSenhaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sem alteração de schema: a coluna Senha já é TEXT e o valor formatado
            // do hash PBKDF2 (prefixo + iterações + salt + derivado) cabe nela.
            // Esta migration existe para registrar o marco da Spec 002 no histórico.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migration documental: nada a reverter.
        }
    }
}
