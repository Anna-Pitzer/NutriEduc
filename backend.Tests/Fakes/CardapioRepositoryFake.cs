using Backend.Tests.Support;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória de cardápios para os testes de serviço. Reproduz as regras
/// visíveis da consulta — filtro por unidade, situação, data e refeição —, porque é o que
/// o serviço precisa enxergar para decidir escopo.
/// </summary>
public class CardapioRepositoryFake : ICardapioRepository
{
    private readonly List<Cardapio> _cardapios = new();
    private int _proximoId = 1;

    /// <summary>
    /// Resolve o alimento de cada item, como o `Include` do repositório real faz. Sem isso
    /// o serviço montaria DTOs sem nome nem valores — um detalhe que o fake precisa
    /// reproduzir para o teste enxergar o mesmo contrato.
    /// </summary>
    public Func<int, Alimento?>? ResolverAlimento { get; set; }

    public (List<Cardapio> Itens, int Total) Listar(
        FiltroCardapio filtro,
        int pagina,
        int tamanho)
    {
        var filtrados = Filtrar(filtro)
            .OrderByDescending(cardapio => cardapio.Id)
            .ToList();

        return (Paginar(filtrados, pagina, tamanho), filtrados.Count);
    }

    public (List<Cardapio> Itens, int Total) ListarPublicados(
        FiltroCardapio filtro,
        int pagina,
        int tamanho)
    {
        var filtrados = Filtrar(filtro)
            .Where(cardapio =>
                cardapio.Tipo == TipoCardapio.Geral &&
                cardapio.Situacao == SituacaoCardapio.Publicado)
            .OrderBy(cardapio => cardapio.Periodo.Inicio)
            .ToList();

        foreach (var cardapio in filtrados)
            ResolverAlimentos(cardapio);

        return (Paginar(filtrados, pagina, tamanho), filtrados.Count);
    }

    public Cardapio? ObterComRefeicoes(int id)
    {
        var cardapio = _cardapios.FirstOrDefault(item => item.Id == id);

        if (cardapio is not null)
            ResolverAlimentos(cardapio);

        return cardapio;
    }

    public Cardapio? ObterAdaptado(int origemId, int escolaId, string conjuntoChave) =>
        _cardapios
            .Where(cardapio =>
                cardapio.OrigemId == origemId &&
                cardapio.EscolaId == escolaId &&
                cardapio.ConjuntoRestricoesChave == conjuntoChave &&
                cardapio.Situacao != SituacaoCardapio.Substituido)
            .OrderByDescending(cardapio => cardapio.Id)
            .FirstOrDefault();

    public List<Cardapio> ListarAdaptados(int origemId) =>
        _cardapios
            .Where(cardapio =>
                cardapio.OrigemId == origemId &&
                cardapio.Situacao != SituacaoCardapio.Substituido)
            .OrderBy(cardapio => cardapio.EscolaId)
            .ThenBy(cardapio => cardapio.ConjuntoRestricoesChave, StringComparer.Ordinal)
            .ThenBy(cardapio => cardapio.Id)
            .ToList();

    public int ContarAdaptados(int origemId) =>
        _cardapios.Count(cardapio =>
            cardapio.OrigemId == origemId &&
            cardapio.Situacao != SituacaoCardapio.Substituido);

    public Cardapio? ObterGeralPublicado(int unidadeId, DateOnly data)
    {
        var cardapio = _cardapios
            .Where(item =>
                item.Tipo == TipoCardapio.Geral &&
                item.UnidadeId == unidadeId &&
                item.Situacao == SituacaoCardapio.Publicado &&
                item.Periodo.Contem(data))
            .OrderByDescending(item => item.PublicadoEmUtc)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        if (cardapio is not null)
            ResolverAlimentos(cardapio);

        return cardapio;
    }

    public Cardapio? ObterAdaptadoPublicado(
        int origemId,
        int escolaId,
        string conjuntoChave,
        DateOnly data)
    {
        var cardapio = _cardapios
            .Where(item =>
                item.Tipo == TipoCardapio.Adaptado &&
                item.OrigemId == origemId &&
                item.EscolaId == escolaId &&
                item.ConjuntoRestricoesChave == conjuntoChave &&
                item.Situacao == SituacaoCardapio.Publicado &&
                item.Periodo.Contem(data))
            .OrderByDescending(item => item.PublicadoEmUtc)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        if (cardapio is not null)
            ResolverAlimentos(cardapio);

        return cardapio;
    }

    private void ResolverAlimentos(Cardapio cardapio)
    {
        if (ResolverAlimento is null) return;

        foreach (var item in cardapio.Refeicoes.SelectMany(refeicao => refeicao.Itens))
        {
            var alimento = ResolverAlimento(item.AlimentoId);

            if (alimento is not null)
                PropriedadesPrivadas.Definir(item, nameof(ItemRefeicao.Alimento), alimento);
        }
    }

    public bool ExisteCardapioDaUnidade(int unidadeId) =>
        _cardapios.Any(cardapio => cardapio.UnidadeId == unidadeId);

    public IReadOnlyCollection<int> ObterAlimentosUsadosDesde(int unidadeId, DateOnly dataLimite) =>
        _cardapios
            .Where(cardapio =>
                cardapio.UnidadeId == unidadeId &&
                cardapio.Situacao == SituacaoCardapio.Publicado &&
                cardapio.Periodo.Fim >= dataLimite)
            .SelectMany(cardapio => cardapio.Refeicoes)
            .SelectMany(refeicao => refeicao.Itens)
            .Select(item => item.AlimentoId)
            .Distinct()
            .ToList();

    public int? ExistePublicadoNoPeriodo(
        int unidadeId,
        PeriodoVigencia periodo,
        int? ignorarId,
        TipoCardapio? tipo = null,
        int? escolaId = null,
        string? conjuntoChave = null)
    {
        var consulta = _cardapios.Where(cardapio =>
            cardapio.UnidadeId == unidadeId &&
            cardapio.Situacao == SituacaoCardapio.Publicado &&
            (ignorarId == null || cardapio.Id != ignorarId) &&
            cardapio.Periodo.Inicio <= periodo.Fim &&
            cardapio.Periodo.Fim >= periodo.Inicio);

        if (tipo is { } tipoDoCardapio)
        {
            consulta = consulta.Where(cardapio => cardapio.Tipo == tipoDoCardapio);
        }

        if (tipo == TipoCardapio.Adaptado)
        {
            consulta = consulta.Where(cardapio =>
                cardapio.EscolaId == escolaId &&
                cardapio.ConjuntoRestricoesChave == conjuntoChave);
        }

        return consulta
            .Select(cardapio => (int?)cardapio.Id)
            .FirstOrDefault();
    }

    public (List<HistoricoCardapio> Itens, int Total) ListarHistorico(
        int cardapioId,
        int pagina,
        int tamanho)
    {
        var eventos = _cardapios
            .Where(cardapio => cardapio.Id == cardapioId)
            .SelectMany(cardapio => cardapio.Historico)
            .OrderByDescending(historico => historico.CriadoEmUtc)
            .ThenByDescending(historico => historico.Id)
            .ToList();

        return (Paginar(eventos, pagina, tamanho), eventos.Count);
    }

    public (List<AlteracaoCardapioItem> Itens, int Total) ListarAlteracoes(
        int? unidadeId,
        DateTime desde,
        int pagina,
        int tamanho)
    {
        var linhas = _cardapios
            .Where(cardapio => unidadeId == null || cardapio.UnidadeId == unidadeId)
            .SelectMany(cardapio => cardapio.Historico
                .Where(historico => historico.CriadoEmUtc >= desde)
                .Select(historico => new AlteracaoCardapioItem(
                    historico.Id,
                    cardapio.Id,
                    cardapio.Nome,
                    "Unidade",
                    historico.Evento,
                    historico.Versao,
                    historico.AutorNome,
                    historico.Motivo,
                    historico.CriadoEmUtc)))
            .OrderByDescending(linha => linha.CriadoEmUtc)
            .ThenByDescending(linha => linha.Id)
            .ToList();

        return (Paginar(linhas, pagina, tamanho), linhas.Count);
    }

    public Cardapio Add(Cardapio cardapio)
    {
        if (cardapio.Id == 0)
        {
            PropriedadesPrivadas.Definir(cardapio, nameof(Cardapio.Id), _proximoId++);
        }

        _cardapios.Add(cardapio);

        return cardapio;
    }

    public void Update(Cardapio cardapio)
    {
        var indice = _cardapios.FindIndex(item => item.Id == cardapio.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Cardápio {cardapio.Id} não existe no fake.");

        _cardapios[indice] = cardapio;
    }

    public void Delete(Cardapio cardapio)
    {
        _cardapios.RemoveAll(item => item.Id == cardapio.Id);
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<Cardapio> GetCardapios() => _cardapios.ToList();

    private IEnumerable<Cardapio> Filtrar(FiltroCardapio filtro)
    {
        var consulta = _cardapios.AsEnumerable();

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(cardapio => cardapio.UnidadeId == unidadeId);
        }

        if (filtro.Situacao is { } situacao)
        {
            consulta = consulta.Where(cardapio => cardapio.Situacao == situacao);
        }

        if (filtro.Data is { } data)
        {
            consulta = consulta.Where(cardapio => cardapio.Periodo.Contem(data));
        }

        if (filtro.EscolaVisivelId is { } escolaVisivel)
        {
            consulta = consulta.Where(cardapio =>
                cardapio.Tipo == TipoCardapio.Geral || cardapio.EscolaId == escolaVisivel);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Refeicao))
        {
            consulta = consulta.Where(cardapio => cardapio.Refeicoes.Any(refeicao =>
                string.Equals(refeicao.Nome, filtro.Refeicao.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        return consulta;
    }

    private static List<T> Paginar<T>(List<T> itens, int pagina, int tamanho) =>
        itens.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
}
