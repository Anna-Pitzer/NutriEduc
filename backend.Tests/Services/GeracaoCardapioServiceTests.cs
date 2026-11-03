using Backend.Tests.Fakes;
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
        AlimentoRepositoryFake Alimentos);

    private static Cenario Montar()
    {
        var parametros = new ParametroGeracaoRepositoryFake();
        var alimentos = new AlimentoRepositoryFake();
        var alunos = new AlunoRepositoryFake();
        var cardapios = new CardapioRepositoryFake();
        var unidadeDeTrabalho = new UnidadeDeTrabalhoFake();

        var servico = new GeracaoCardapioService(
            parametros, alimentos, alunos, cardapios, unidadeDeTrabalho);

        return new Cenario(servico, parametros, alimentos);
    }

    private static GerarCardapioDTO EntradaGerar(int unidadeId) => new()
    {
        UnidadeId = unidadeId,
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
    public void Gerar_SemAlimentosAtivos_LancaGeracaoImpossivel()
    {
        var cenario = Montar();
        cenario.Parametros.Add(Parametros(unidadeId: 1));

        Assert.Throws<GeracaoImpossivelException>(
            () => cenario.Servico.Gerar(EntradaGerar(unidadeId: 1)));
    }
}
