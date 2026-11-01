using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

public class EscolaRepository : IEscolaRepository
{
    private readonly ConnectionContext _context;

    public EscolaRepository(ConnectionContext context)
    {
        _context = context;
    }

    public Escola Add(Escola escola)
    {
        _context.Escolas.Add(escola);
        _context.SaveChanges();
        return escola;
    }

    public Escola? GetEscola(int id)
    {
        return _context.Escolas.FirstOrDefault(escola => escola.Id == id);
    }

    public List<Escola> Listar()
    {
        return _context.Escolas.OrderBy(escola => escola.Nome).ToList();
    }

    /// <summary>
    /// Comparação sem diferenciar caixa, coerente com o índice NOCASE. O serviço usa
    /// esta checagem para responder 409 antes de o banco recusar a gravação.
    /// </summary>
    public bool ExisteNome(string nome, int? ignorarId = null)
    {
        var alvo = (nome ?? string.Empty).Trim().ToLowerInvariant();

        return _context.Escolas.Any(escola =>
            escola.Nome.ToLower() == alvo &&
            (ignorarId == null || escola.Id != ignorarId));
    }

    public void Update(Escola escola)
    {
        _context.Escolas.Update(escola);
        _context.SaveChanges();
    }

    public UnidadeEscolar AddUnidade(UnidadeEscolar unidade)
    {
        _context.Unidades.Add(unidade);
        _context.SaveChanges();
        return unidade;
    }

    public UnidadeEscolar? GetUnidade(int id)
    {
        return _context.Unidades.FirstOrDefault(unidade => unidade.Id == id);
    }

    public List<UnidadeEscolar> ListarUnidades()
    {
        return _context.Unidades
            .OrderBy(unidade => unidade.Nome)
            .ToList();
    }

    public List<UnidadeEscolar> BuscarUnidadesPorNome(string nome)
    {
        var alvo = (nome ?? string.Empty).Trim().ToLowerInvariant();

        if (alvo.Length == 0)
        {
            return new List<UnidadeEscolar>();
        }

        return _context.Unidades
            .Where(unidade => unidade.Nome.ToLower() == alvo)
            .ToList();
    }

    public bool ExisteNomeUnidade(string nome, int? ignorarId = null)
    {
        var alvo = (nome ?? string.Empty).Trim().ToLowerInvariant();

        return _context.Unidades.Any(unidade =>
            unidade.Nome.ToLower() == alvo &&
            (ignorarId == null || unidade.Id != ignorarId));
    }

    public void UpdateUnidade(UnidadeEscolar unidade)
    {
        _context.Unidades.Update(unidade);
        _context.SaveChanges();
    }

    public void Delete(Escola escola)
    {
        _context.Escolas.Remove(escola);
        _context.SaveChanges();
    }

    public void DeleteUnidade(UnidadeEscolar unidade)
    {
        _context.Unidades.Remove(unidade);
        _context.SaveChanges();
    }

    public bool ExisteUnidadeDaEscola(int escolaId) =>
        _context.Unidades.Any(unidade => unidade.EscolaId == escolaId);
}
