using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Persistência da notificação no contexto Scoped (Spec 018). A escrita acontece dentro da
/// transação do serviço de origem; a leitura é sempre filtrada pelo destinatário.
/// </summary>
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

    public void Update(Notificacao notificacao) =>
        _context.Notificacoes.Update(notificacao);

    public Notificacao? Obter(int id, int destinatarioId) =>
        _context.Notificacoes.FirstOrDefault(notificacao =>
            notificacao.Id == id && notificacao.DestinatarioId == destinatarioId);

    public (List<Notificacao> Itens, int Total) Listar(
        int destinatarioId,
        bool? naoLidas,
        int pagina,
        int tamanho)
    {
        var consulta = _context.Notificacoes
            .Where(notificacao => notificacao.DestinatarioId == destinatarioId);

        if (naoLidas is { } apenasNaoLidas)
        {
            consulta = consulta.Where(notificacao => notificacao.Lida == !apenasNaoLidas);
        }

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(notificacao => notificacao.CriadoEmUtc)
            .ThenByDescending(notificacao => notificacao.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    public int ContarNaoLidas(int destinatarioId) =>
        _context.Notificacoes.Count(notificacao =>
            notificacao.DestinatarioId == destinatarioId && !notificacao.Lida);

    public bool ExistePorDestinatario(int destinatarioId) =>
        _context.Notificacoes.Any(notificacao => notificacao.DestinatarioId == destinatarioId);

    public void MarcarTodasComoLidas(int destinatarioId)
    {
        var naoLidas = _context.Notificacoes
            .Where(notificacao => notificacao.DestinatarioId == destinatarioId && !notificacao.Lida)
            .ToList();

        foreach (var notificacao in naoLidas)
        {
            notificacao.MarcarComoLida();
        }
    }
}
