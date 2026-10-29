using Microsoft.EntityFrameworkCore;
using Ntc.Database;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Valida a construção do modelo do EF Core sem abrir conexão. É o gate que pega erro de
/// mapeamento — por exemplo, um objeto de valor que exigiria coluna nova no banco — antes
/// de subir a aplicação.
/// </summary>
public class ConnectionContextTests
{
    [Fact]
    public void Modelo_MapeiaTurmaEResponsavelNasColunasExistentes()
    {
        using var contexto = new ConnectionContext();

        var aluno = contexto.Model.FindEntityType(typeof(Aluno));

        Assert.NotNull(aluno);
        Assert.Equal("Alunos", aluno!.GetTableName());

        var turma = aluno.FindComplexProperty(nameof(Aluno.Turma))?.ComplexType;

        Assert.NotNull(turma);
        Assert.Equal("Escola", turma!.FindProperty(nameof(Turma.Escola))?.GetColumnName());
        Assert.Equal("Serie", turma.FindProperty(nameof(Turma.Serie))?.GetColumnName());

        var responsavel = aluno.FindComplexProperty(nameof(Aluno.Responsavel))?.ComplexType;

        Assert.NotNull(responsavel);
        Assert.Equal("NomeResponsavel", responsavel!.FindProperty(nameof(Responsavel.Nome))?.GetColumnName());
        Assert.Equal("TelefoneResponsavel", responsavel.FindProperty(nameof(Responsavel.Telefone))?.GetColumnName());
    }

    [Fact]
    public void Modelo_NaoCriaColunaNovaParaOsObjetosDeValor()
    {
        using var contexto = new ConnectionContext();

        var aluno = contexto.Model.FindEntityType(typeof(Aluno))!;

        var colunas = aluno.GetProperties()
            .Select(propriedade => propriedade.GetColumnName())
            .Concat(aluno.GetComplexProperties()
                .SelectMany(complexa => complexa.ComplexType.GetProperties())
                .Select(propriedade => propriedade.GetColumnName()))
            .OrderBy(coluna => coluna, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Anafilaxia",
                "Escola",
                "Id",
                "Nascimento",
                "Nome",
                "NomeResponsavel",
                "Observacao",
                "Restricoes",
                "Serie",
                "Telefone",
                "TelefoneResponsavel"
            },
            colunas);
    }
}
