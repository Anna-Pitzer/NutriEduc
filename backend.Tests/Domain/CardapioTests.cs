using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Invariantes do período de vigência (Spec 012, RF1): o fim nunca antecede o início e
/// o limite é de 366 dias inclusive.
/// </summary>
public class PeriodoVigenciaTests
{
    [Fact]
    public void Fim_AnteriorAoInicio_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            new PeriodoVigencia(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 2)));

        Assert.Contains("anterior ao início", erro.Message);
    }

    [Fact]
    public void UmAnoBissextoInteiro_EhAceito()
    {
        var periodo = new PeriodoVigencia(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.Equal(366, periodo.Fim.DayNumber - periodo.Inicio.DayNumber + 1);
    }

    [Fact]
    public void PeriodoDe367Dias_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            new PeriodoVigencia(new DateOnly(2024, 1, 1), new DateOnly(2025, 1, 1)));

        Assert.Contains("366", erro.Message);
    }

    [Fact]
    public void Contem_IncluiAsBordas()
    {
        var periodo = new PeriodoVigencia(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6));

        Assert.True(periodo.Contem(new DateOnly(2026, 11, 2)));
        Assert.True(periodo.Contem(new DateOnly(2026, 11, 6)));
        Assert.False(periodo.Contem(new DateOnly(2026, 11, 7)));
    }
}

/// <summary>
/// Quantidade do item de refeição (Spec 012, RF3): acima de zero, no máximo 5000 g e
/// com duas casas decimais.
/// </summary>
public class ItemRefeicaoTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("5001")]
    [InlineData("100.125")]
    public void QuantidadeInvalida_Lanca(string gramas)
    {
        var valor = decimal.Parse(gramas, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentException>(() => new ItemRefeicao(1, valor));
    }

    [Fact]
    public void QuantidadeLimite_EhAceita()
    {
        var item = new ItemRefeicao(7, 5000);

        Assert.Equal(7, item.AlimentoId);
        Assert.Equal(5000m, item.QuantidadeGramas);
    }

    [Fact]
    public void AlimentoInvalido_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new ItemRefeicao(0, 100));
    }
}

/// <summary>
/// Estrutura da refeição (Spec 012, RF2): nome, ordem, limite de itens e alimento único.
/// </summary>
public class RefeicaoTests
{
    [Fact]
    public void NomeMuitoCurto_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Refeicao("A", 1, null));
    }

    [Fact]
    public void OrdemNaoPositiva_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Refeicao("Almoço", 0, null));
    }

    [Fact]
    public void AlimentoRepetido_Lanca()
    {
        var itens = new[] { new ItemRefeicao(1, 100), new ItemRefeicao(1, 50) };

        var erro = Assert.Throws<ArgumentException>(() => new Refeicao("Almoço", 1, itens));

        Assert.Contains("mesmo alimento", erro.Message);
    }

    [Fact]
    public void MaisDe50Itens_Lanca()
    {
        var itens = Enumerable.Range(1, 51).Select(id => new ItemRefeicao(id, 10));

        Assert.Throws<ArgumentException>(() => new Refeicao("Almoço", 1, itens));
    }

    [Fact]
    public void RefeicaoVazia_EhAceita()
    {
        var refeicao = new Refeicao("Lanche", 2, null);

        Assert.Empty(refeicao.Itens);
        Assert.Equal(2, refeicao.Ordem);
    }
}

/// <summary>
/// Agregado do cardápio (Spec 012, RF1, RF2, RF6 e RF12): criação em rascunho, limites,
/// edição atômica e cópia independente.
/// </summary>
public class CardapioTests
{
    private static readonly PeriodoVigencia PeriodoSemana =
        new(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 6));

    private static Refeicao Refeicao(string nome, int ordem, params (int AlimentoId, decimal Gramas)[] itens) =>
        new(nome, ordem, itens.Select(item => new ItemRefeicao(item.AlimentoId, item.Gramas)));

    private static Cardapio Criar(params Refeicao[] refeicoes) =>
        new("Semana 12", 3, PeriodoSemana, refeicoes);

    [Fact]
    public void Nasce_EmRascunho()
    {
        var cardapio = Criar(Refeicao("Almoço", 1, (7, 100m)));

        Assert.Equal(SituacaoCardapio.Rascunho, cardapio.Situacao);
        Assert.Equal(3, cardapio.UnidadeId);
        Assert.Equal(1, cardapio.Versao);
        Assert.Null(cardapio.CardapioDuplicadoDeId);
    }

    [Fact]
    public void SemRefeicoes_EhAceito_ETemTotalZero()
    {
        var cardapio = new Cardapio("Semana vazia", 3, PeriodoSemana, null);

        Assert.Empty(cardapio.Refeicoes);
        Assert.Equal(0, cardapio.TotalDeItens);
    }

    [Fact]
    public void MaisDe10Refeicoes_Lanca()
    {
        var refeicoes = Enumerable.Range(1, 11)
            .Select(ordem => Refeicao($"Refeição {ordem}", ordem))
            .ToArray();

        Assert.Throws<ArgumentException>(() => Criar(refeicoes));
    }

    [Fact]
    public void OrdemRepetida_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            Criar(
                Refeicao("Almoço", 1),
                Refeicao("Jantar", 1)));

        Assert.Contains("mesma ordem", erro.Message);
    }

    [Fact]
    public void NomeRepetidoSemDiferenciarCaixa_Lanca()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            Criar(
                Refeicao("Almoço", 1),
                Refeicao("almoço", 2)));

        Assert.Contains("mesmo nome", erro.Message);
    }

    [Fact]
    public void Atualizar_ComPayloadInvalido_NaoAlteraNada()
    {
        var cardapio = Criar(Refeicao("Almoço", 1, (7, 100m)));
        var periodoNovo = new PeriodoVigencia(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5));

        Assert.Throws<ArgumentException>(() =>
            cardapio.Atualizar("Semana 13", periodoNovo, new[] { Refeicao("A", 1) }));

        Assert.Equal("Semana 12", cardapio.Nome);
        Assert.Equal(PeriodoSemana.Inicio, cardapio.Periodo.Inicio);
        Assert.Single(cardapio.Refeicoes);
        Assert.Equal("Almoço", cardapio.Refeicoes[0].Nome);
    }

    [Fact]
    public void Atualizar_EmCardapioQueNaoEhRascunho_Lanca()
    {
        var cardapio = Criar(Refeicao("Almoço", 1, (7, 100m)));
        PropriedadesPrivadas.Definir(cardapio, nameof(Cardapio.Situacao), SituacaoCardapio.Publicado);

        var erro = Assert.Throws<InvalidOperationException>(() =>
            cardapio.Atualizar("Semana 13", PeriodoSemana, null));

        Assert.Contains("rascunho", erro.Message);
        Assert.Single(cardapio.Refeicoes);
    }

    [Fact]
    public void Atualizar_Valido_SubstituiRefeicoes()
    {
        var cardapio = Criar(Refeicao("Almoço", 1, (7, 100m)));
        var periodoNovo = new PeriodoVigencia(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5));

        cardapio.Atualizar("Semana 13", periodoNovo, new[] { Refeicao("Jantar", 1, (9, 80m)) });

        Assert.Equal("Semana 13", cardapio.Nome);
        Assert.Equal(periodoNovo.Inicio, cardapio.Periodo.Inicio);
        Assert.Equal("Jantar", Assert.Single(cardapio.Refeicoes).Nome);
        Assert.Equal(1, cardapio.TotalDeItens);
    }

    [Fact]
    public void Copiar_CriaComposicaoIgualComInstanciasNovas()
    {
        var origem = Criar(Refeicao("Almoço", 1, (7, 100m), (8, 50m)));
        var periodoNovo = new PeriodoVigencia(new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 13));

        var copia = Cardapio.Copiar(origem, "Semana 13", periodoNovo, duplicadoDeId: origem.Id);

        Assert.NotSame(origem, copia);
        Assert.Equal(origem.TotalDeItens, copia.TotalDeItens);
        Assert.NotSame(origem.Refeicoes[0], copia.Refeicoes[0]);
        Assert.Equal(origem.Refeicoes[0].Nome, copia.Refeicoes[0].Nome);
        Assert.Equal(origem.Refeicoes[0].Itens[0].AlimentoId, copia.Refeicoes[0].Itens[0].AlimentoId);
        Assert.NotSame(origem.Refeicoes[0].Itens[0], copia.Refeicoes[0].Itens[0]);
        Assert.Equal(SituacaoCardapio.Rascunho, copia.Situacao);
    }

    [Fact]
    public void Copiar_SemMarcadorDeOrigem_NaoRegistraDuplicacao()
    {
        var origem = Criar(Refeicao("Almoço", 1, (7, 100m)));

        var copia = Cardapio.Copiar(origem, "Revisão", PeriodoSemana);

        Assert.Null(copia.CardapioDuplicadoDeId);
    }
}
