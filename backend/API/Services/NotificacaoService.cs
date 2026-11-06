using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Ponto único de emissão de notificações (Spec 018, RF8). A emissão é síncrona e acontece
/// na mesma unidade de trabalho da mudança de origem: ou a mudança e o aviso são gravados,
/// ou nenhum dos dois.
///
/// Nesta fatia, consumida pela Spec 017/030, o conteúdo é apenas o tipo e o status da
/// solicitação — nunca dado clínico, CPF, e-mail ou telefone (RF13).
/// </summary>
public sealed class NotificacaoService
{
    private readonly INotificacaoRepository _notificacoes;

    public NotificacaoService(INotificacaoRepository notificacoes)
    {
        _notificacoes = notificacoes;
    }

    public void Notificar(
        int destinatarioId,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string? referenciaTipo,
        int? referenciaId,
        DateTime utc) =>
        _notificacoes.Add(new Notificacao(
            destinatarioId,
            tipo,
            titulo,
            mensagem,
            referenciaTipo,
            referenciaId,
            utc));

    /// <summary>
    /// Notifica o solicitante e o decisor sobre a mudança de status de uma solicitação.
    /// Não notifica duas vezes a mesma pessoa.
    /// </summary>
    public void NotificarSolicitacao(
        int solicitanteId,
        int decisorId,
        int solicitacaoId,
        TipoSolicitacao tipoSolicitacao,
        StatusSolicitacao status,
        DateTime utc)
    {
        var titulo = $"Solicitação #{solicitacaoId}: {status}";

        Notificar(
            solicitanteId,
            TipoNotificacao.SolicitacaoStatus,
            titulo,
            $"Seu pedido de {tipoSolicitacao} foi atualizado para {status}.",
            "Solicitacao",
            solicitacaoId,
            utc);

        if (decisorId <= 0 || decisorId == solicitanteId)
        {
            return;
        }

        Notificar(
            decisorId,
            TipoNotificacao.SolicitacaoStatus,
            titulo,
            $"A solicitação #{solicitacaoId} está em {status}.",
            "Solicitacao",
            solicitacaoId,
            utc);
    }
}
