using Backend.Tests.Fakes;
using Backend.Tests.Support;
using Ntc.Application;
using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>Regras do caso de uso da geração (Spec 013): parâmetros ausentes (400) e candidatos vazios (422).</summary>
public class GeracaoCardapioServiceTests
{
    private sealed record Cenario(
        GeracaoCardapioService Servico,
        ParametroGeracaoRepositoryFake Parametros,
        AlimentoRepositoryFake Alimentos,
        AlunoRepositoryFake Alunos,
        CardapioRepositoryFake Cardapios);

    /// <summary>Id da nutricionista semeada: a geração exige autoria informada.</summary>
    private const int NutricionistaId = 1;

    private static Cenario Montar()
    {
        var parametros = new ParametroGeracaoRepositoryFake();
        var alimentos = new AlimentoRepositoryFake();
        var alunos = new AlunoRepositoryFake();
        var cardapios = new CardapioRepositoryFake();
        var unidadeDeTrabalho = new UnidadeDeTrabalhoFake();
        var usuarios = new UsuarioRepositoryFake();
        var categorias = new CategoriaAlimentoRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var restricoes = new RestricaoRepositoryFake();

        usuarios.Add(new Usuario(
            "Nutri Teste",
            "12345678901",
            new DateOnly(1990, 1, 1),
            "nutri.teste@nutrieduc.local",
            PapelUsuario.Nutricionista,
            unidadeId: 1,
            crn: "CRN-3 12345"));

        var servico = new GeracaoCardapioService(
            parametros, alimentos, alunos, cardapios, usuarios,
            categorias, escolas, restricoes, unidadeDeTrabalho);

        return new Cenario(servico, parametros, alimentos, alunos, cardapios);
    }

    private static GerarCardapioDTO EntradaGerar(int unidadeId) => new()
    {
        UnidadeId = unidadeId,
        NutricionistaId = NutricionistaId,
        Nome = "Semana 12",
        Periodo = new PeriodoCardapioDTO
        {
            Inicio = new DateOnly(2026, 11, 2),
            Fim = new DateOnly(2026, 11, 6)
        },
        Seed = 42
    };

    private static ParametroGeracao Parametros(int unidadeId) =>
        new(unidadeId, 0, 600, 2, 0, false, null, new[] { "Almoço" });

    [Fact]
    public void Gerar_SemParametros_DevolveInvalido()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Gerar(EntradaGerar(unidadeId: 1));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("Parâmetros", resultado.Erro!);
    }

    [Fact]
    public void Gerar_SemNutricionista_DevolveInvalido()
    {
        var cenario = Montar();
        cenario.Parametros.Add(Parametros(unidadeId: 1));

        var entrada = EntradaGerar(unidadeId: 1);
        entrada.NutricionistaId = 0;

        var resultado = cenario.Servico.Gerar(entrada);

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Contains("nutricionista", resultado.Erro!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gerar_SemAlimentosAtivos_LancaGeracaoImpossivel()
    {
        var cenario = Montar();
        cenario.Parametros.Add(Parametros(unidadeId: 1));

        Assert.Throws<GeracaoImpossivelException>(
            () => cenario.Servico.Gerar(EntradaGerar(unidadeId: 1)));
    }

    private static Alimento CriarAlimento(AlimentoRepositoryFake alimentos, string nome) =>
        alimentos.Add(new Alimento(
            nome,
            1,
            ValoresNutricionais.Criar(130, 2.7m, 28.2m, 0.3m, 0.4m, 1),
            null));

    /// <summary>
    /// Alimento com vínculo a uma restrição do catálogo. O fake não resolve o <c>Include</c>
    /// da navegação <c>Restricao</c>, então ela é definida manualmente para a análise de
    /// compatibilidade enxergar nome e categoria (Spec 013, RF7).
    /// </summary>
    private static Alimento CriarAlimentoComRestricao(
        AlimentoRepositoryFake alimentos,
        string nome,
        string restricao,
        CategoriaRestricao categoria)
    {
        var catalogo = new RestricaoAlimentar(restricao, categoria);
        var alimento = alimentos.Add(new Alimento(
            nome,
            1,
            ValoresNutricionais.Criar(130, 2.7m, 28.2m, 0.3m, 0.4m, 1),
            new[] { 1 }));

        foreach (var vinculo in alimento.Vinculos)
            PropriedadesPrivadas.Definir(vinculo, nameof(AlimentoRestricao.Restricao), catalogo);

        return alimento;
    }

    [Fact]
    public void Gerar_GeralNaoFiltraPorRestricao()
    {
        var cenario = Montar();
        cenario.Parametros.Add(new ParametroGeracao(1, 0, 600, 2, 0, true, null, new[] { "Almoço" }));

        CriarAlimentoComRestricao(
            cenario.Alimentos, "Iogurte", "Lactose", CategoriaRestricao.Alergia);
        CriarAlimento(cenario.Alimentos, "Arroz");
        CriarAlimento(cenario.Alimentos, "Feijão");

        var aluno = new Aluno(
            "Ana",
            new DateOnly(2015, 5, 20),
            new Turma("Escola Municipal", "5 ano"),
            null,
            null,
            new[] { "Lactose" },
            false,
            new Responsavel("Ana Silva", "11988888888"),
            escolaId: 1);
        aluno.DefinirUnidade(1);
        cenario.Alunos.Add(aluno);

        var resultado = cenario.Servico.Gerar(EntradaGerar(unidadeId: 1));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);

        // RF3: o geral não é filtrado por restrições individuais; a exclusão é da adaptação.
        var cardapio = Assert.Single(cenario.Cardapios.GetCardapios());
        Assert.Equal(TipoCardapio.Geral, cardapio.Tipo);
    }

    [Fact]
    public void Gerar_ComJanelaDeRepeticao_ExcluiAlimentoDePublicadoRecente()
    {
        var cenario = Montar();
        cenario.Parametros.Add(new ParametroGeracao(1, 0, 600, 1, 3, false, null, new[] { "Almoço" }));

        var arroz = CriarAlimento(cenario.Alimentos, "Arroz");
        CriarAlimento(cenario.Alimentos, "Feijão");
        CriarAlimento(cenario.Alimentos, "Carne");

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var publicado = new Cardapio(
            "Semana anterior",
            1,
            new PeriodoVigencia(hoje.AddDays(-2), hoje),
            new[] { new Refeicao("Almoço", 1, new[] { new ItemRefeicao(arroz.Id, 100m) }) });
        publicado.Publicar("teste", DateTime.UtcNow);
        cenario.Cardapios.Add(publicado);

        var resultado = cenario.Servico.Gerar(EntradaGerar(unidadeId: 1));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);

        var gerado = cenario.Cardapios.GetCardapios().Single(cardapio => cardapio.Situacao == SituacaoCardapio.Rascunho);
        var ids = gerado.Refeicoes.SelectMany(refeicao => refeicao.Itens).Select(item => item.AlimentoId).ToList();
        Assert.DoesNotContain(arroz.Id, ids);
    }
}
