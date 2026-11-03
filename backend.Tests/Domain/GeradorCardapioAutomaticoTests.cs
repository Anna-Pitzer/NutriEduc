using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

public class GeradorCardapioAutomaticoTests
{
    private static CandidatoAlimento Alimento(int id, int categoriaId, decimal calorias) =>
        new(id, categoriaId, ValoresNutricionais.Criar(calorias, 0, 0, 0, 0, 0));

    private static ParametroGeracao Parametros(
        int opcoes,
        decimal min = 0,
        decimal max = 600,
        int[]? categorias = null,
        string[]? refeicoes = null) =>
        new(1, min, max, opcoes, 0, false, categorias, refeicoes ?? new[] { "Almoço" });

    private static EntradaGeracao Entrada(
        CandidatoAlimento[] candidatos,
        ParametroGeracao parametros,
        int seed,
        int[]? bloqueados = null,
        string[]? refeicoes = null) =>
        new(
            (refeicoes ?? new[] { "Almoço" }).Select((nome, i) => new RefeicaoPadrao(nome, i + 1)).ToList(),
            candidatos,
            parametros,
            new HashSet<int>(bloqueados ?? Array.Empty<int>()),
            seed);

    private static List<int> Ids(ResultadoGeracao resultado) =>
        resultado.Refeicoes
            .SelectMany(refeicao => refeicao.Itens.Select(item => item.AlimentoId))
            .ToList();

    [Fact]
    public void MesmaSemente_ProduzMesmoResultado()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150),
            Alimento(3, 1, 200)
        };
        var parametros = Parametros(opcoes: 2);

        var primeiro = GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 42));
        var segundo = GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 42));

        Assert.Equal(Ids(primeiro), Ids(segundo));
    }

    [Fact]
    public void SementesDiferentes_ProduzemResultadosDiferentes()
    {
        var candidatos = Enumerable.Range(1, 10)
            .Select(i => Alimento(i, 1, 100))
            .ToArray();
        var parametros = Parametros(opcoes: 10, max: 2000);

        var primeiro = GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 1));
        var segundo = GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 2));

        Assert.NotEqual(Ids(primeiro), Ids(segundo));
    }

    [Fact]
    public void FaixaCalorica_RespeitadaEmTodasAsRefeicoes()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150),
            Alimento(3, 1, 200),
            Alimento(4, 1, 120),
            Alimento(5, 1, 180)
        };
        var parametros = Parametros(opcoes: 2, min: 0, max: 500);

        var resultado = GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 7));

        foreach (var refeicao in resultado.Refeicoes)
        {
            var total = refeicao.Itens.Sum(item => item.Valores.CaloriasKcal);
            Assert.InRange(total, parametros.CaloriasMinimasRefeicao, parametros.CaloriasMaximasRefeicao);
        }
    }

    [Fact]
    public void AlimentoBloqueado_NuncaEhEscolhido()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150),
            Alimento(3, 1, 200)
        };
        var parametros = Parametros(opcoes: 2);

        var resultado = GeradorCardapioAutomatico.Gerar(
            Entrada(candidatos, parametros, seed: 3, bloqueados: new[] { 1 }));

        Assert.DoesNotContain(1, Ids(resultado));
    }

    [Fact]
    public void CategoriaObrigatoriaSemCandidato_Lanca()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150)
        };
        var parametros = Parametros(opcoes: 1, categorias: new[] { 99 });

        Assert.Throws<GeracaoImpossivelException>(
            () => GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 1)));
    }

    [Fact]
    public void CategoriaObrigatoria_Atendida()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 2, 150),
            Alimento(3, 1, 200)
        };
        var parametros = Parametros(opcoes: 2, categorias: new[] { 2 });

        var resultado = GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 1));

        var ids = Ids(resultado);
        Assert.Contains(2, ids);
        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public void FaixaCaloricaImpossivel_Lanca()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150),
            Alimento(3, 1, 200)
        };
        var parametros = Parametros(opcoes: 2, min: 400, max: 500);

        Assert.Throws<GeracaoImpossivelException>(
            () => GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 1)));
    }

    [Fact]
    public void CandidatosInsuficientes_Lanca()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150)
        };
        var parametros = Parametros(opcoes: 3);

        Assert.Throws<GeracaoImpossivelException>(
            () => GeradorCardapioAutomatico.Gerar(Entrada(candidatos, parametros, seed: 1)));
    }

    [Fact]
    public void AlimentoNaoRepeteEntreRefeicoes()
    {
        var candidatos = new[]
        {
            Alimento(1, 1, 100),
            Alimento(2, 1, 150),
            Alimento(3, 1, 200),
            Alimento(4, 1, 120)
        };
        var parametros = new ParametroGeracao(
            1, 0, 600, 2, 0, false, null, new[] { "Almoço", "Jantar" });

        var resultado = GeradorCardapioAutomatico.Gerar(
            new EntradaGeracao(
                new[] { new RefeicaoPadrao("Almoço", 1), new RefeicaoPadrao("Jantar", 2) },
                candidatos,
                parametros,
                new HashSet<int>(),
                Seed: 5));

        var ids = Ids(resultado);

        Assert.Equal(4, ids.Count);
        Assert.Equal(4, ids.Distinct().Count());
    }
}
