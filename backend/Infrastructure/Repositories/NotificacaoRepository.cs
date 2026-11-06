using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>Persistência da notificação no contexto Scoped da operação de origem.</summary>
public class NotificacaoRepository : INotificacaoRepository
{
    private readonly ConnectionContext _context;

    public NotificacaoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public Notificacao Add(Notificacao notificacao)
    {
        _context.Notificacoes.Add(notificacao);
        return notificacao;
    }
}
