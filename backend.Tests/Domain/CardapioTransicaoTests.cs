using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

public class CardapioTransicaoTests
{
    private static Cardapio CriarRascunho() =>
        new(
            "Semana 12",
            1,
            new PeriodoVigencia(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6)),
            Array.Empty<Refeicao>());

    private static void DefinirSituacao(Cardapio cardapio, SituacaoCardapio situacao) =>
        PropriedadesPrivadas.Definir(cardapio, nameof(Cardapio.Situacao), situacao);

    [Fact]
    public void Publicar_Rascunho_MarcaPublicadoComEvento()
    {
        var cardapio = CriarRascunho();
        var utc = DateTime.UtcNow;

        cardapio.Publicar("Maria", utc);

        Assert.Equal(SituacaoCardapio.Publicado, cardapio.Situacao);
        Assert.Equal(utc, cardapio.PublicadoEmUtc);
        Assert.Equal("Maria", cardapio.PublicadoPor);

        var evento = Assert.Single(cardapio.Historico);
        Assert.Equal(EventoCardapio.Publicado, evento.Evento);
        Assert.Equal("Maria", evento.AutorNome);
    }

    [Fact]
    public void Publicar_JaPublicado_LancaTransicaoInvalida()
    {
        var cardapio = CriarRascunho();
        DefinirSituacao(cardapio, SituacaoCardapio.Publicado);

        Assert.Throws<TransicaoInvalidaException>(() => cardapio.Publicar("Maria", DateTime.UtcNow));
    }

    [Fact]
    public void Cancelar_ComMotivo_MarcaCanceladoComEvento()
    {
        var cardapio = CriarRascunho();

        cardapio.Cancelar("Produto descontinuado", "Maria", DateTime.UtcNow);

        Assert.Equal(SituacaoCardapio.Cancelado, cardapio.Situacao);

        var evento = Assert.Single(cardapio.Historico);
        Assert.Equal(EventoCardapio.Cancelado, evento.Evento);
        Assert.Equal("Produto descontinuado", evento.Motivo);
    }

    [Fact]
    public void Cancelar_MotivoCurto_LancaArgumento()
    {
        var cardapio = CriarRascunho();

        Assert.Throws<ArgumentException>(() => cardapio.Cancelar("x", "Maria", DateTime.UtcNow));
    }

    [Fact]
    public void Cancelar_JaEncerrado_LancaTransicaoInvalida()
    {
        var cardapio = CriarRascunho();
        DefinirSituacao(cardapio, SituacaoCardapio.Cancelado);

        Assert.Throws<TransicaoInvalidaException>(
            () => cardapio.Cancelar("Motivo válido", "Maria", DateTime.UtcNow));
    }

    [Fact]
    public void CriarRevisao_IncrementaVersaoEMantemOriginal()
    {
        var original = CriarRascunho();
        PropriedadesPrivadas.Definir(original, nameof(Cardapio.Id), 10);
        DefinirSituacao(original, SituacaoCardapio.Publicado);

        var revisao = original.CriarRevisao("Maria", DateTime.UtcNow);

        Assert.Equal(2, revisao.Versao);
        Assert.Equal(10, revisao.RevisaoDeId);
        Assert.Equal(SituacaoCardapio.Rascunho, revisao.Situacao);
        Assert.Equal(SituacaoCardapio.Publicado, original.Situacao);

        var evento = Assert.Single(revisao.Historico);
        Assert.Equal(EventoCardapio.RevisaoCriada, evento.Evento);
    }

    [Fact]
    public void MarcarSubstituido_Publicado_GeraEvento()
    {
        var cardapio = CriarRascunho();
        DefinirSituacao(cardapio, SituacaoCardapio.Publicado);

        cardapio.MarcarSubstituido(99, "Maria", DateTime.UtcNow);

        Assert.Equal(SituacaoCardapio.Substituido, cardapio.Situacao);

        var evento = Assert.Single(cardapio.Historico);
        Assert.Equal(EventoCardapio.Substituido, evento.Evento);
    }
}
