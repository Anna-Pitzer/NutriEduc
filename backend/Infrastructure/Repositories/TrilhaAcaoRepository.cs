using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

public sealed class TrilhaAcaoRepository : ITrilhaAcaoRepository
{
    private readonly ConnectionContext _context;

    public TrilhaAcaoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public void Add(TrilhaAcao acao)
    {
        _context.TrilhaAcoes.Add(acao);
    }

    public List<TrilhaAcao> Listar()
    {
        return _context.TrilhaAcoes
            .OrderBy(acao => acao.EmUtc)
            .ThenBy(acao => acao.Id)
            .ToList();
    }
}
