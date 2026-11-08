using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class CrnUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes de criar o índice único, verifica duplicidade (Spec 031, T4). Se houver
            // CRN repetido, a migração aborta com o relatório dos valores repetidos e não
            // altera dados; a correção é manual pelo administrador.
            migrationBuilder.Sql(@"
                CREATE TEMP TRIGGER __verifica_crn_duplicado
                AFTER UPDATE OF ""Crn"" ON ""Usuarios""
                WHEN EXISTS (
                    SELECT 1 FROM ""Usuarios""
                    WHERE ""Crn"" IS NOT NULL
                    GROUP BY ""Crn"" HAVING COUNT(*) > 1
                )
                BEGIN
                    SELECT RAISE(ABORT,
                        'CRNs duplicados em Usuarios.Crn: ' ||
                        (SELECT GROUP_CONCAT(""Crn"", ', ')
                         FROM (SELECT ""Crn"" FROM ""Usuarios""
                               WHERE ""Crn"" IS NOT NULL
                               GROUP BY ""Crn"" HAVING COUNT(*) > 1)));
                END;

                UPDATE ""Usuarios"" SET ""Crn"" = ""Crn"" WHERE ""Crn"" IS NOT NULL;

                DROP TRIGGER __verifica_crn_duplicado;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Crn",
                table: "Usuarios",
                column: "Crn",
                unique: true,
                filter: "\"Crn\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Crn",
                table: "Usuarios");
        }
    }
}
