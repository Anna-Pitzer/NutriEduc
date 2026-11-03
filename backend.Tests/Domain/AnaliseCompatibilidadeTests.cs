using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

public class AnaliseCompatibilidadeTests
{
    private static ResultadoCompatibilidade Analisar(
        ItemAnalisado[] itens,
        Dictionary<int, IReadOnlyList<VinculoAnalisado>>? vinculos = null,
        AlunoAnalisado[]? alunos = null) =>
        AnaliseCompatibilidade.Analisar(
            itens,
            vinculos ?? new Dictionary<int, IReadOnlyList<VinculoAnalisado>>(),
            alunos ?? Array.Empty<AlunoAnalisado>());

    [Fact]
    public void Alergia_DeAluno_GeraBloqueante()
    {
        var itens = new[] { new ItemAnalisado(1, "Almoço", 7, "Iogurte") };
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>
        {
            [7] = new[] { new VinculoAnalisado(1, "Lactose", CategoriaRestricao.Alergia) }
        };
        var alunos = new[] { new AlunoAnalisado(3, "Ana", new[] { "Lactose" }) };

        var resultado = Analisar(itens, vinculos, alunos);

        Assert.False(resultado.Compativel);
        var achado = Assert.Single(resultado.Bloqueantes);
        Assert.Equal(SeveridadeCompatibilidade.Bloqueante, achado.Severidade);
        Assert.Equal("Lactose", achado.RestricaoNome);
        Assert.Equal("Almoço", achado.RefeicaoNome);
        Assert.Empty(resultado.Alertas);
    }

    [Fact]
    public void Restricao_DeAluno_GeraAlertaNaoBloqueante()
    {
        var itens = new[] { new ItemAnalisado(1, "Almoço", 7, "Pão") };
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>
        {
            [7] = new[] { new VinculoAnalisado(2, "Gluten", CategoriaRestricao.Restricao) }
        };
        var alunos = new[] { new AlunoAnalisado(3, "Ana", new[] { "Gluten" }) };

        var resultado = Analisar(itens, vinculos, alunos);

        Assert.True(resultado.Compativel);
        var achado = Assert.Single(resultado.Alertas);
        Assert.Equal(SeveridadeCompatibilidade.Alerta, achado.Severidade);
        Assert.Empty(resultado.Bloqueantes);
    }

    [Fact]
    public void AlimentoSemVinculo_EhPermitido()
    {
        var itens = new[] { new ItemAnalisado(1, "Almoço", 7, "Arroz") };
        var alunos = new[] { new AlunoAnalisado(3, "Ana", new[] { "Lactose" }) };

        var resultado = Analisar(itens, vinculos: null, alunos);

        Assert.True(resultado.Compativel);
        Assert.Equal(new[] { "Arroz" }, resultado.AlimentosPermitidos);
        Assert.Empty(resultado.AlimentosNaoPermitidos);
    }

    [Fact]
    public void MesmoAlimentoEmDuasRefeicoes_GeraDoisAchados()
    {
        var itens = new[]
        {
            new ItemAnalisado(1, "Almoço", 7, "Iogurte"),
            new ItemAnalisado(2, "Jantar", 7, "Iogurte")
        };
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>
        {
            [7] = new[] { new VinculoAnalisado(1, "Lactose", CategoriaRestricao.Alergia) }
        };
        var alunos = new[] { new AlunoAnalisado(3, "Ana", new[] { "Lactose" }) };

        var resultado = Analisar(itens, vinculos, alunos);

        Assert.Equal(2, resultado.Bloqueantes.Count);
        Assert.Contains(resultado.Bloqueantes, achado => achado.RefeicaoNome == "Almoço");
        Assert.Contains(resultado.Bloqueantes, achado => achado.RefeicaoNome == "Jantar");
    }

    [Fact]
    public void DoisAlunosComMesmaRestricao_GeramUmAchadoComDoisAlunos()
    {
        var itens = new[] { new ItemAnalisado(1, "Almoço", 7, "Iogurte") };
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>
        {
            [7] = new[] { new VinculoAnalisado(1, "Lactose", CategoriaRestricao.Alergia) }
        };
        var alunos = new[]
        {
            new AlunoAnalisado(3, "Ana", new[] { "Lactose" }),
            new AlunoAnalisado(9, "Beto", new[] { "Lactose" })
        };

        var resultado = Analisar(itens, vinculos, alunos);

        var achado = Assert.Single(resultado.Bloqueantes);
        Assert.Equal(2, achado.AlunosAfetados.Count);
        Assert.Equal("Ana", achado.AlunosAfetados[0].Nome);
        Assert.Equal("Beto", achado.AlunosAfetados[1].Nome);
    }

    [Fact]
    public void UnidadeSemAlunos_DevolveCompativelComAviso()
    {
        var itens = new[] { new ItemAnalisado(1, "Almoço", 7, "Iogurte") };
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>
        {
            [7] = new[] { new VinculoAnalisado(1, "Lactose", CategoriaRestricao.Alergia) }
        };

        var resultado = Analisar(itens, vinculos, Array.Empty<AlunoAnalisado>());

        Assert.True(resultado.Compativel);
        Assert.Equal(0, resultado.TotalAlunosAnalisados);
        Assert.Contains("Nenhum aluno", Assert.Single(resultado.Avisos));
    }

    [Fact]
    public void DuasExecucoes_ProduzemMesmaOrdenacao()
    {
        var itens = new[]
        {
            new ItemAnalisado(1, "Almoço", 8, "Leite"),
            new ItemAnalisado(1, "Almoço", 7, "Iogurte")
        };
        var vinculos = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>
        {
            [7] = new[] { new VinculoAnalisado(1, "Lactose", CategoriaRestricao.Alergia) },
            [8] = new[] { new VinculoAnalisado(1, "Lactose", CategoriaRestricao.Alergia) }
        };
        var alunos = new[]
        {
            new AlunoAnalisado(9, "Beto", new[] { "Lactose" }),
            new AlunoAnalisado(3, "Ana", new[] { "Lactose" })
        };

        var primeiro = Analisar(itens, vinculos, alunos);
        var segundo = Analisar(itens, vinculos, alunos);

        Assert.Equal(
            primeiro.Bloqueantes.Select(achado => (achado.RefeicaoOrdem, achado.AlimentoNome, achado.AlunosAfetados[0].Nome)),
            segundo.Bloqueantes.Select(achado => (achado.RefeicaoOrdem, achado.AlimentoNome, achado.AlunosAfetados[0].Nome)));
    }
}
