using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

public class ParametroGeracaoTests
{
    private static ParametroGeracao Criar(
        decimal min = 0,
        decimal max = 600,
        int opcoes = 2,
        int janela = 0,
        bool excluir = false,
        int[]? categorias = null,
        string[]? refeicoes = null) =>
        new(1, min, max, opcoes, janela, excluir, categorias, refeicoes ?? new[] { "Almoço" });

    [Fact]
    public void Construtor_ComDadosValidos_CriaParametros()
    {
        var parametro = Criar();

        Assert.Equal(1, parametro.UnidadeId);
        Assert.Equal(2, parametro.OpcoesPorRefeicao);
        Assert.Equal(new[] { "Almoço" }, parametro.RefeicoesPadrao);
    }

    [Fact]
    public void Construtor_ComMinimoMaiorQueMaximo_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(min: 600, max: 300));
    }

    [Fact]
    public void Construtor_ComCaloriasNegativas_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(min: -1));
    }

    [Fact]
    public void Construtor_ComOpcoesForaDaFaixa_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(opcoes: 0));
        Assert.Throws<ArgumentException>(() => Criar(opcoes: 11));
    }

    [Fact]
    public void Construtor_ComJanelaAcimaDe30_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(janela: 31));
    }

    [Fact]
    public void Construtor_SemRefeicoesPadrao_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(refeicoes: Array.Empty<string>()));
    }

    [Fact]
    public void Construtor_ComCategoriaRepetida_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(categorias: new[] { 1, 1 }));
    }

    [Fact]
    public void Atualizar_RedefineCamposEMantemUnidade()
    {
        var parametro = Criar(min: 0, max: 600);

        parametro.Atualizar(100, 500, 3, 7, false, new[] { 2 }, new[] { "Almoço", "Jantar" });

        Assert.Equal(1, parametro.UnidadeId);
        Assert.Equal(100, parametro.CaloriasMinimasRefeicao);
        Assert.Equal(500, parametro.CaloriasMaximasRefeicao);
        Assert.Equal(3, parametro.OpcoesPorRefeicao);
        Assert.Equal(7, parametro.JanelaRepeticaoDias);
        Assert.Equal(new[] { 2 }, parametro.CategoriasObrigatorias);
        Assert.Equal(new[] { "Almoço", "Jantar" }, parametro.RefeicoesPadrao);
    }
}
