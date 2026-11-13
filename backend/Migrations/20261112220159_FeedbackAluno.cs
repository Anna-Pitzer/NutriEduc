using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FeedbackAluno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlunoId",
                table: "Feedbacks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlunoNome",
                table: "Feedbacks",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_AlunoId",
                table: "Feedbacks",
                column: "AlunoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Feedbacks_Alunos_AlunoId",
                table: "Feedbacks",
                column: "AlunoId",
                principalTable: "Alunos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Feedbacks_Alunos_AlunoId",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_AlunoId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AlunoId",
                table: "Feedbacks");

            migrationBuilder.DropColumn(
                name: "AlunoNome",
                table: "Feedbacks");
        }
    }
}
