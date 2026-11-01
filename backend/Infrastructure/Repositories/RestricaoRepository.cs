using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Catálogo e histórico de restrições (Spec 010). Recebe o <see cref="ConnectionContext"/>
/// por construtor (L4) e compartilha a instância Scoped com os demais repositórios, de modo
/// que o histórico é gravado na mesma unidade de persistência do aluno (RNF2).
/// </summary>
public class RestricaoRepository : IRestricaoRepository
{
    private readonly ConnectionContext _context;

    public RestricaoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public List<RestricaoAlimentar> ObterAtivasPorNomes(IEnumerable<string> nomesNormalizados)
    {
        var nomes = nomesNormalizados.ToList();

        if (nomes.Count == 0)
        {
            return new List<RestricaoAlimentar>();
        }

        return _context.RestricoesAlimentares
            .Where(restricao => restricao.Ativa && nomes.Contains(restricao.NomeNormalizado))
            .ToList();
    }

    public List<RestricaoAlimentar> Listar(CategoriaRestricao? categoria, bool incluirInativas)
    {
        var consulta = _context.RestricoesAlimentares.AsQueryable();

        if (categoria is { } valor)
        {
            consulta = consulta.Where(restricao => restricao.Categoria == valor);
        }

        if (!incluirInativas)
        {
            consulta = consulta.Where(restricao => restricao.Ativa);
        }

        return consulta
            .OrderBy(restricao => restricao.Categoria)
            .ThenBy(restricao => restricao.Nome)
            .ToList();
    }

    public RestricaoAlimentar? Obter(int id) =>
        _context.RestricoesAlimentares.FirstOrDefault(restricao => restricao.Id == id);

    public bool ExisteNomeNormalizado(
        string nomeNormalizado,
        CategoriaRestricao categoria,
        int? ignorarId)
    {
        return _context.RestricoesAlimentares.Any(restricao =>
            restricao.NomeNormalizado == nomeNormalizado &&
            restricao.Categoria == categoria &&
            (ignorarId == null || restricao.Id != ignorarId));
    }

    public RestricaoAlimentar Add(RestricaoAlimentar restricao)
    {
        _context.RestricoesAlimentares.Add(restricao);
        return restricao;
    }

    public void Update(RestricaoAlimentar restricao)
    {
        _context.RestricoesAlimentares.Update(restricao);
    }
}
