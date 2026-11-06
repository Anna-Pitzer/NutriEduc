using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Regras da versão adaptada (Spec 029): chave canônica do conjunto, invariantes do
/// agregado, substituição apenas de item bloqueante, tolerância nutricional e pendência
/// explícita quando não há candidato. Função pura, sem banco.
/// </summary>
public class AdaptacaoDeCardapioTests
{
    private static readonly DateOnly Inicio = new(2026, 11, 2);
    private static readonly DateOnly Fim = new(2026, 11, 6);

    private static ValoresNutricionais Valores(
        decimal kcal = 130m,
        decimal proteina = 2.7m,
        decimal gordura = 0.3m,
        decimal fibra = 0.4m) =>
        ValoresNutricionais.Criar(kcal, proteina, 28.2m, gordura, fibra, 1m);

    private static Alimento CriarAlimento(
        string nome,
        int categoriaId,
        ValoresNutricionais? valores = null,
        IEnumerable<int>? restricaoIds = null,
        int? id = null)
    {
        var alimento = new Alimento(
            nome,
            categoriaId,
            valores ?? Valores(),
            restricaoIds);

        if (id is { } definido)
        {
            PropriedadesPrivadas.Definir(alimento, nameof(Alimento.Id), definido);
        }

        return alimento;
    }

    private static Cardapio CriarGeral(params Refeicao[] refeicoes) =>
        new("Geral da unidade", 1, new PeriodoVigencia(Inicio, Fim), refeicoes);

    private static readonly IReadOnlyList<RestricaoDoCatalogo> CatalogoDeRestricoes =
        new[] { new RestricaoDoCatalogo(1, "Lactose", CategoriaRestricao.Alergia) };

    [Fact]
    public void Conjunto_ChaveCanonica_IgnoraOrdemEDuplicata()
    {
        var primeiro = ConjuntoDeRestricoes.DeIds(new[] { 7, 3, 7, 12 });
        var segundo = ConjuntoDeRestricoes.DeIds(new[] { 12, 3, 7 });

        Assert.Equal("3,7,12", primeiro.Chave);
        Assert.Equal(primeiro, segundo);
        Assert.True(primeiro.Contem(7));
        Assert.False(primeiro.Contem(5));
        Assert.Equal("10:20:3,7,12", primeiro.ChaveDeVersao(10, 20));
    }

    [Fact]
    public void Adaptado_SemOrigem_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Cardapio(
            "Adaptado sem origem",
            1,
            new PeriodoVigencia(Inicio, Fim),
            Array.Empty<Refeicao>(),
            tipo: TipoCardapio.Adaptado,
            escolaId: 10,
            conjunto: ConjuntoDeRestricoes.DeIds(new[] { 1 }),
            statusAdaptacao: StatusAdaptacao.Atualizado));
    }

    [Fact]
    public void Geral_ComEscola_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Cardapio(
            "Geral com escola",
            1,
            new PeriodoVigencia(Inicio, Fim),
            Array.Empty<Refeicao>(),
            escolaId: 10));
    }

    [Fact]
    public void CriarAdaptado_ComOrigemAdaptada_Lanca()
    {
        var geral = CriarGeral();
        PropriedadesPrivadas.Definir(geral, nameof(Cardapio.Id), 1);

        var adaptado = Cardapio.CriarAdaptado(
            geral,
            10,
            ConjuntoDeRestricoes.DeIds(new[] { 1 }),
            Array.Empty<Refeicao>(),
            null,
            null,
            StatusAdaptacao.Atualizado);

        Assert.Throws<ArgumentException>(() => Cardapio.CriarAdaptado(
            adaptado,
            11,
            ConjuntoDeRestricoes.DeIds(new[] { 1 }),
            Array.Empty<Refeicao>(),
            null,
            null,
            StatusAdaptacao.Atualizado));
    }

    [Fact]
    public void Adaptar_SubstituiSoItensBloqueantes()
    {
        var leite = CriarAlimento("Leite", 1, restricaoIds: new[] { 1 }, id: 1);
        var bebida = CriarAlimento("Bebida vegetal", 1, id: 2);
        var arroz = CriarAlimento("Arroz", 2, id: 3);

        var geral = CriarGeral(new Refeicao(
            "Almoço",
            1,
            new[]
            {
                new ItemRefeicao(leite.Id, 100m),
                new ItemRefeicao(arroz.Id, 100m)
            }));

        var resultado = AdaptacaoDeCardapio.Adaptar(
            geral,
            10,
            ConjuntoDeRestricoes.DeIds(new[] { 1 }),
            new[] { leite, bebida, arroz },
            CatalogoDeRestricoes);

        Assert.Equal(StatusAdaptacao.Atualizado, resultado.Status);
        Assert.Single(resultado.Substituicoes);
        Assert.Equal(leite.Id, resultado.Substituicoes[0].AlimentoOriginalId);
        Assert.Equal(bebida.Id, resultado.Substituicoes[0].AlimentoSubstitutoId);
        Assert.Contains("Lactose", resultado.Substituicoes[0].RestricoesBloqueantes);

        var itens = resultado.Refeicoes.Single().Itens;
        Assert.Equal(2, itens.Count);
        Assert.Contains(itens, item => item.AlimentoId == bebida.Id);
        Assert.Contains(itens, item => item.AlimentoId == arroz.Id);
    }

    [Fact]
    public void Adaptar_SemCandidato_MarcaPendenteERemoveItemBloqueante()
    {
        var leite = CriarAlimento("Leite", 1, restricaoIds: new[] { 1 }, id: 1);

        var geral = CriarGeral(new Refeicao(
            "Almoço",
            1,
            new[] { new ItemRefeicao(leite.Id, 100m) }));

        var resultado = AdaptacaoDeCardapio.Adaptar(
            geral,
            10,
            ConjuntoDeRestricoes.DeIds(new[] { 1 }),
            new[] { leite },
            CatalogoDeRestricoes);

        Assert.Equal(StatusAdaptacao.PendenteDeRevisao, resultado.Status);
        Assert.Empty(resultado.Substituicoes);
        Assert.Contains(resultado.Pendencias, pendencia => pendencia.AlimentoId == leite.Id);
        Assert.Empty(resultado.Refeicoes.Single().Itens);
    }

    [Fact]
    public void Adaptar_RefeicaoForaDaTolerancia_MarcaPendente()
    {
        // Cada item tem substituto 19% acima em kcal (dentro dos 20% do item), mas os dois
        // juntos levam a refeição a 19% acima — fora dos 10% permitidos (RF7).
        var originalA = CriarAlimento("Original A", 1, restricaoIds: new[] { 1 }, id: 1);
        var substitutoA = CriarAlimento(
            "Substituto A", 1,
            Valores(154.7m, 3.21m, 0.36m, 0.48m),
            id: 2);

        var originalB = CriarAlimento("Original B", 2, restricaoIds: new[] { 1 }, id: 3);
        var substitutoB = CriarAlimento(
            "Substituto B", 2,
            Valores(154.7m, 3.21m, 0.36m, 0.48m),
            id: 4);

        var geral = CriarGeral(new Refeicao(
            "Almoço",
            1,
            new[]
            {
                new ItemRefeicao(originalA.Id, 100m),
                new ItemRefeicao(originalB.Id, 100m)
            }));

        var resultado = AdaptacaoDeCardapio.Adaptar(
            geral,
            10,
            ConjuntoDeRestricoes.DeIds(new[] { 1 }),
            new[] { originalA, substitutoA, originalB, substitutoB },
            CatalogoDeRestricoes);

        Assert.Equal(StatusAdaptacao.PendenteDeRevisao, resultado.Status);
        Assert.Equal(2, resultado.Substituicoes.Count);
        Assert.Contains(resultado.Pendencias, pendencia => pendencia.AlimentoId is null);
    }

    [Fact]
    public void VerificarAdaptabilidade_AlimentoSemSubstituto_BloqueiaMontagem()
    {
        var leite = CriarAlimento("Leite", 1, restricaoIds: new[] { 1 }, id: 1);
        var bebida = CriarAlimento("Bebida vegetal", 1, id: 2);

        var geral = CriarGeral(new Refeicao(
            "Almoço",
            1,
            new[] { new ItemRefeicao(leite.Id, 100m) }));

        var grupo = new[]
        {
            new GrupoDeAdaptacao(10, "Escola A", ConjuntoDeRestricoes.DeIds(new[] { 1 }))
        };

        var semSubstituto = AdaptacaoDeCardapio.VerificarAdaptabilidade(
            geral,
            grupo,
            new[] { leite },
            CatalogoDeRestricoes);

        var comSubstituto = AdaptacaoDeCardapio.VerificarAdaptabilidade(
            geral,
            grupo,
            new[] { leite, bebida },
            CatalogoDeRestricoes);

        Assert.False(Assert.Single(semSubstituto).TemSubstitutoSeguro);
        Assert.True(Assert.Single(comSubstituto).TemSubstitutoSeguro);
    }
}
