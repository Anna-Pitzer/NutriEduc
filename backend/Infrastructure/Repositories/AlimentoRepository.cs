using Microsoft.EntityFrameworkCore;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Catálogo de alimentos (Spec 011). Recebe o <see cref="ConnectionContext"/> por
/// construtor (L4) e compartilha a instância Scoped com os demais repositórios, para que
/// alimento e vínculos sejam gravados na mesma unidade de persistência.
/// </summary>
public class AlimentoRepository : IAlimentoRepository
{
    private readonly ConnectionContext _context;

    public AlimentoRepository(ConnectionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Busca por prefixo e, apenas quando o prefixo não devolve nada, por "contém"
    /// (RF7). O fallback é decidido pelo total da consulta inteira — não pelo tamanho da
    /// página —, então a página 2 de um resultado vazio também cai na segunda etapa.
    /// </summary>
    public (List<Alimento> Itens, int Total) Listar(
        FiltroAlimento filtro,
        int pagina,
        int tamanho)
    {
        var consulta = Filtrar(filtro, prefixo: true);
        var total = consulta.Count();

        if (total == 0 && !string.IsNullOrEmpty(filtro.NomeNormalizado))
        {
            consulta = Filtrar(filtro, prefixo: false);
            total = consulta.Count();
        }

        var itens = Ordenar(
                consulta
                    .Include(alimento => alimento.Categoria)
                    .Include(alimento => alimento.Vinculos)
                        .ThenInclude(vinculo => vinculo.Restricao),
                filtro.Ordenacao)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    /// <summary>
    /// Ordenação da página (RF7). As três ordenações por texto repetem o desempate por
    /// id para que a paginação seja estável; "recentes" usa o id, que é crescente na
    /// inserção.
    /// </summary>
    private static IOrderedQueryable<Alimento> Ordenar(
        IQueryable<Alimento> consulta,
        OrdenacaoAlimento ordenacao)
    {
        return ordenacao switch
        {
            OrdenacaoAlimento.NomeDescendente =>
                consulta.OrderByDescending(alimento => alimento.Nome)
                    .ThenBy(alimento => alimento.Id),

            OrdenacaoAlimento.Categoria =>
                consulta.OrderBy(alimento => alimento.Categoria!.Nome)
                    .ThenBy(alimento => alimento.Nome)
                    .ThenBy(alimento => alimento.Id),

            OrdenacaoAlimento.Recentes =>
                consulta.OrderByDescending(alimento => alimento.Id),

            _ =>
                consulta.OrderBy(alimento => alimento.Nome)
                    .ThenBy(alimento => alimento.Id)
        };
    }

    public Alimento? Obter(int id) =>
        _context.Alimentos.FirstOrDefault(alimento => alimento.Id == id);

    public Alimento? ObterComVinculos(int id) =>
        _context.Alimentos
            .Include(alimento => alimento.Categoria)
            .Include(alimento => alimento.Vinculos)
                .ThenInclude(vinculo => vinculo.Restricao)
            .FirstOrDefault(alimento => alimento.Id == id);

    /// <summary>
    /// Alimentos de um cardápio em uma única consulta (Spec 012). A composição por 100 g
    /// é coluna do próprio alimento (tipo complexo), então vem junto sem Include.
    /// </summary>
    public List<Alimento> ObterPorIds(IEnumerable<int> ids)
    {
        var solicitados = ids.Distinct().ToList();

        return solicitados.Count == 0
            ? new List<Alimento>()
            : _context.Alimentos
                .Where(alimento => solicitados.Contains(alimento.Id))
                .ToList();
    }

    public List<Alimento> ObterComVinculosPorIds(IEnumerable<int> ids)
    {
        var solicitados = ids.Distinct().ToList();

        return solicitados.Count == 0
            ? new List<Alimento>()
            : _context.Alimentos
                .Include(alimento => alimento.Vinculos)
                    .ThenInclude(vinculo => vinculo.Restricao)
                .Where(alimento => solicitados.Contains(alimento.Id))
                .ToList();
    }

    public List<Alimento> ListarAtivosComVinculos() =>
        _context.Alimentos
            .Include(alimento => alimento.Vinculos)
                .ThenInclude(vinculo => vinculo.Restricao)
            .Where(alimento => alimento.Ativo)
            .OrderBy(alimento => alimento.Id)
            .ToList();

    public bool ExisteNomeNormalizado(string nomeNormalizado, int? ignorarId) =>
        _context.Alimentos.Any(alimento =>
            alimento.NomeNormalizado == nomeNormalizado &&
            (ignorarId == null || alimento.Id != ignorarId));

    public Alimento Add(Alimento alimento)
    {
        _context.Alimentos.Add(alimento);
        return alimento;
    }

    public void Update(Alimento alimento)
    {
        _context.Alimentos.Update(alimento);
    }

    private IQueryable<Alimento> Filtrar(FiltroAlimento filtro, bool prefixo)
    {
        var consulta = _context.Alimentos.AsQueryable();

        if (!filtro.IncluirInativos)
        {
            consulta = consulta.Where(alimento => alimento.Ativo);
        }

        if (filtro.CategoriaId is { } categoriaId)
        {
            consulta = consulta.Where(alimento => alimento.CategoriaId == categoriaId);
        }

        if (!string.IsNullOrEmpty(filtro.NomeNormalizado))
        {
            var padrao = MontarPadrao(filtro.NomeNormalizado, prefixo);
            consulta = consulta.Where(alimento =>
                EF.Functions.Like(alimento.NomeNormalizado, padrao));
        }

        return consulta;
    }

    /// <summary>
    /// Monta o padrão do <c>LIKE</c> escapando os curingas: o texto do usuário é termo de
    /// busca, nunca expressão. A normalização (caixa e acento) já veio do serviço.
    /// </summary>
    private static string MontarPadrao(string termo, bool prefixo)
    {
        var limpo = termo
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return prefixo ? $"{limpo}%" : $"%{limpo}%";
    }
}
