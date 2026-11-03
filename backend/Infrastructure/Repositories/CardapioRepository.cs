using Microsoft.EntityFrameworkCore;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Persistência do agregado de cardápio (Spec 012). Compartilha a instância Scoped do
/// <see cref="ConnectionContext"/> com os demais repositórios, para que cardápio,
/// refeições e itens entrem na mesma unidade de persistência.
/// </summary>
public class CardapioRepository : ICardapioRepository
{
    private readonly ConnectionContext _context;

    public CardapioRepository(ConnectionContext context)
    {
        _context = context;
    }

    public (List<Cardapio> Itens, int Total) Listar(
        FiltroCardapio filtro,
        int pagina,
        int tamanho)
    {
        var consulta = Filtrar(filtro);
        var total = consulta.Count();

        var itens = consulta
            .Include(cardapio => cardapio.Refeicoes)
                .ThenInclude(refeicao => refeicao.Itens)
                    .ThenInclude(item => item.Alimento)
            .OrderByDescending(cardapio => cardapio.CriadoEmUtc)
            .ThenByDescending(cardapio => cardapio.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    public (List<Cardapio> Itens, int Total) ListarPublicados(
        FiltroCardapio filtro,
        int pagina,
        int tamanho)
    {
        var consulta = Filtrar(filtro, exigirPublicado: true, exigirUnidade: true);
        var total = consulta.Count();

        var itens = consulta
            .Include(cardapio => cardapio.Refeicoes)
                .ThenInclude(refeicao => refeicao.Itens)
                    .ThenInclude(item => item.Alimento)
            .OrderBy(cardapio => cardapio.Periodo.Inicio)
            .ThenBy(cardapio => cardapio.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    public Cardapio? ObterComRefeicoes(int id) =>
        _context.Cardapios
            .Include(cardapio => cardapio.Refeicoes)
                .ThenInclude(refeicao => refeicao.Itens)
                    .ThenInclude(item => item.Alimento)
            .FirstOrDefault(cardapio => cardapio.Id == id);

    public bool ExisteCardapioDaUnidade(int unidadeId) =>
        _context.Cardapios.Any(cardapio => cardapio.UnidadeId == unidadeId);

    public IReadOnlyCollection<int> ObterAlimentosUsadosDesde(int unidadeId, DateOnly dataLimite) =>
        _context.Cardapios
            .Where(cardapio =>
                cardapio.UnidadeId == unidadeId &&
                cardapio.Situacao == SituacaoCardapio.Publicado &&
                cardapio.Periodo.Fim >= dataLimite)
            .SelectMany(cardapio => cardapio.Refeicoes)
            .SelectMany(refeicao => refeicao.Itens)
            .Select(item => item.AlimentoId)
            .Distinct()
            .ToList();

    public int? ExistePublicadoNoPeriodo(int unidadeId, PeriodoVigencia periodo, int? ignorarId) =>
        _context.Cardapios
            .Where(cardapio =>
                cardapio.UnidadeId == unidadeId &&
                cardapio.Situacao == SituacaoCardapio.Publicado &&
                (ignorarId == null || cardapio.Id != ignorarId) &&
                cardapio.Periodo.Inicio <= periodo.Fim &&
                cardapio.Periodo.Fim >= periodo.Inicio)
            .Select(cardapio => (int?)cardapio.Id)
            .FirstOrDefault();

    public (List<HistoricoCardapio> Itens, int Total) ListarHistorico(
        int cardapioId,
        int pagina,
        int tamanho)
    {
        var consulta = _context.HistoricosCardapios
            .Where(historico => historico.CardapioId == cardapioId);

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(historico => historico.CriadoEmUtc)
            .ThenByDescending(historico => historico.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    public (List<AlteracaoCardapioItem> Itens, int Total) ListarAlteracoes(
        int? unidadeId,
        DateTime desde,
        int pagina,
        int tamanho)
    {
        var consulta = _context.HistoricosCardapios
            .Where(historico => historico.CriadoEmUtc >= desde);

        if (unidadeId is { } unidadeDoUsuario)
        {
            consulta = consulta.Where(historico =>
                _context.Cardapios.Any(cardapio =>
                    cardapio.Id == historico.CardapioId && cardapio.UnidadeId == unidadeDoUsuario));
        }

        var projetado =
            from historico in consulta
            join cardapio in _context.Cardapios on historico.CardapioId equals cardapio.Id
            join unidade in _context.Unidades on cardapio.UnidadeId equals unidade.Id
            orderby historico.CriadoEmUtc descending, historico.Id descending
            select new AlteracaoCardapioItem(
                historico.Id,
                historico.CardapioId,
                cardapio.Nome,
                unidade.Nome,
                historico.Evento,
                historico.Versao,
                historico.AutorNome,
                historico.Motivo,
                historico.CriadoEmUtc);

        var total = projetado.Count();

        var itens = projetado
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    public Cardapio Add(Cardapio cardapio)
    {
        _context.Cardapios.Add(cardapio);
        return cardapio;
    }

    public void Update(Cardapio cardapio)
    {
        _context.Cardapios.Update(cardapio);
    }

    public void Delete(Cardapio cardapio)
    {
        _context.Cardapios.Remove(cardapio);
    }

    /// <summary>
    /// Filtro comum às duas leituras. Na rota pública a unidade e a situação são
    /// obrigatórias (RF9); na listagem interna são opcionais.
    /// </summary>
    private IQueryable<Cardapio> Filtrar(
        FiltroCardapio filtro,
        bool exigirPublicado = false,
        bool exigirUnidade = false)
    {
        var consulta = _context.Cardapios.AsQueryable();

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(cardapio => cardapio.UnidadeId == unidadeId);
        }
        else if (exigirUnidade)
        {
            return consulta.Where(_ => false);
        }

        if (filtro.Situacao is { } situacao)
        {
            consulta = consulta.Where(cardapio => cardapio.Situacao == situacao);
        }
        else if (exigirPublicado)
        {
            consulta = consulta.Where(cardapio => cardapio.Situacao == SituacaoCardapio.Publicado);
        }

        // O período é um tipo complexo: o intervalo vira comparação das duas colunas, que
        // o SQLite resolve como texto ISO (Spec 012, RNF6).
        if (filtro.Data is { } data)
        {
            consulta = consulta.Where(cardapio =>
                cardapio.Periodo.Inicio <= data && cardapio.Periodo.Fim >= data);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Refeicao))
        {
            var nome = filtro.Refeicao.Trim().ToLowerInvariant();

            consulta = consulta.Where(cardapio =>
                cardapio.Refeicoes.Any(refeicao => refeicao.Nome.ToLower() == nome));
        }

        return consulta;
    }
}
