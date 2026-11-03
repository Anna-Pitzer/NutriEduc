using Backend.Tests.Fakes;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

public class CompatibilidadeServiceTests
{
    private static readonly EscopoUsuario Nutricao = new(1, PapelUsuario.Nutricionista, null, null);

    private sealed record Cenario(
        CompatibilidadeService Servico,
        CardapioRepositoryFake Cardapios,
        int UnidadeA,
        int UnidadeB);

    private static Cenario Montar()
    {
        var cardapios = new CardapioRepositoryFake();
        var alimentos = new AlimentoRepositoryFake();
        var alunos = new AlunoRepositoryFake();
        var servico = new CompatibilidadeService(cardapios, alimentos, alunos);

        return new Cenario(servico, cardapios, 1, 2);
    }

    private static Cardapio CriarCardapio(int unidadeId) =>
        new(
            "Semana 12",
            unidadeId,
            new PeriodoVigencia(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6)),
            Array.Empty<Refeicao>());

    [Fact]
    public void Analisar_CardapioInexistente_DevolveNaoEncontrado()
    {
        var cenario = Montar();

        var resultado = cenario.Servico.Analisar(999, Nutricao);

        Assert.Equal(ResultadoStatus.NaoEncontrado, resultado.Status);
    }

    [Fact]
    public void Analisar_ComoMerendeiraDeOutraUnidade_DevolveProibido()
    {
        var cenario = Montar();
        var cardapio = CriarCardapio(cenario.UnidadeA);
        cenario.Cardapios.Add(cardapio);

        var resultado = cenario.Servico.Analisar(
            cardapio.Id,
            new EscopoUsuario(2, PapelUsuario.Merendeira, null, cenario.UnidadeB));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Analisar_ComoResponsavel_DevolveProibido()
    {
        var cenario = Montar();
        var cardapio = CriarCardapio(cenario.UnidadeA);
        cenario.Cardapios.Add(cardapio);

        var resultado = cenario.Servico.Analisar(
            cardapio.Id,
            new EscopoUsuario(3, PapelUsuario.Responsavel, null, null));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }
}
