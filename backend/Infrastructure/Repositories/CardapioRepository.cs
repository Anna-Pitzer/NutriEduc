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
