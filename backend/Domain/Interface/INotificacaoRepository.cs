using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>Porta de escrita da notificação (Spec 018).</summary>
public interface INotificacaoRepository
{
    Notificacao Add(Notificacao notificacao);
}
