using System.Text;
using Ntc.Domain.Relatorios;
using Ntc.Exportacao;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Exportador CSV (ADR-012): segunda implementação de <c>IExportadorDeRelatorio</c>. Garante
/// o contrato que o serviço consome — extensão, tipo de conteúdo, cabeçalho, escape RFC 4180
/// e determinismo — sem depender do banco.
/// </summary>
public class ExportadorCsvTests
{
    private static readonly byte[] Bom = Encoding.UTF8.GetPreamble();

    private static TabelaDeRelatorio Tabela() =>
        new(
            "Alunos e restrições",
            new[] { "Aluno", "Restrição" },
            new[]
            {
                new[] { "Ana", "Lactose" },
                new[] { "Bruno; Silva", "Ovo" }
            },
            "2 linhas");

    private static string Texto(byte[] arquivo)
    {
        Assert.Equal(Bom, arquivo.Take(Bom.Length).ToArray());

        return Encoding.UTF8.GetString(arquivo, Bom.Length, arquivo.Length - Bom.Length);
    }

    private static string[] Linhas(byte[] arquivo) =>
        Texto(arquivo).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void IdentificaOFormatoPeloContrato()
    {
        var exportador = new ExportadorCsv();

        Assert.Equal("csv", exportador.Extensao);
        Assert.Equal("text/csv; charset=utf-8", exportador.TipoDeConteudo);
    }

    [Fact]
    public void Gerar_EscreveCabecalhoELinhasComPontoEVirgula()
    {
        var linhas = Linhas(new ExportadorCsv().Gerar(Tabela()));

        Assert.Equal(3, linhas.Length);
        Assert.Equal("Aluno;Restrição", linhas[0]);
        Assert.Equal("Ana;Lactose", linhas[1]);
    }

    [Fact]
    public void Gerar_EscapaCampoComSeparador()
    {
        var texto = Texto(new ExportadorCsv().Gerar(Tabela()));

        Assert.Contains("\"Bruno; Silva\";Ovo", texto);
    }

    [Fact]
    public void Gerar_EscapaAspasEDuplicaOInterno()
    {
        var tabela = new TabelaDeRelatorio(
            "t",
            new[] { "Nome" },
            new[] { new[] { "Ana \"Nutri\" Souza" } });

        var texto = Texto(new ExportadorCsv().Gerar(tabela));

        Assert.Contains("\"Ana \"\"Nutri\"\" Souza\"", texto);
    }

    [Fact]
    public void Gerar_EscapaCampoComQuebraDeLinha()
    {
        var tabela = new TabelaDeRelatorio(
            "t",
            new[] { "Observação" },
            new[] { new[] { "linha 1\nlinha 2" } });

        var texto = Texto(new ExportadorCsv().Gerar(tabela));

        Assert.Contains("\"linha 1\nlinha 2\"", texto);
    }

    [Fact]
    public void Gerar_SemColunas_UsaCabecalhoPadrao()
    {
        var tabela = new TabelaDeRelatorio(
            "t",
            Array.Empty<string>(),
            new[] { new[] { "42" } });

        var linhas = Linhas(new ExportadorCsv().Gerar(tabela));

        Assert.Equal("Valor", linhas[0]);
        Assert.Equal("42", linhas[1]);
    }

    [Fact]
    public void Gerar_NaoEscreveTituloNemRodape()
    {
        var texto = Texto(new ExportadorCsv().Gerar(Tabela()));

        Assert.DoesNotContain("Alunos e restrições", texto);
        Assert.DoesNotContain("2 linhas", texto);
    }

    [Fact]
    public void Gerar_MesmaTabela_ProduzOsMesmosBytes()
    {
        var exportador = new ExportadorCsv();

        Assert.Equal(exportador.Gerar(Tabela()), exportador.Gerar(Tabela()));
    }
}
