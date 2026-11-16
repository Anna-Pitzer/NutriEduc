using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Notificações do fluxo de solicitação de acesso (Spec 033 + Spec 018): toda solicitação
/// criada avisa os <see cref="PapelUsuario.AdminSecretaria"/> ativos da unidade, que são os
/// únicos com autorização para decidir (matriz 028). O ADM autor da criação manual não é
/// notificado. A emissão acontece na mesma unidade de trabalho da criação (RF8 da Spec 018).
/// </summary>
public sealed class NotificacaoDeAcessoService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly NotificacaoService _notificacoes;

    public NotificacaoDeAcessoService(
        IUsuarioRepository usuarios,
        NotificacaoService notificacoes)
    {
        _usuarios = usuarios;
        _notificacoes = notificacoes;
    }

    /// <summary>
    /// Emite a notificação de nova solicitação para os ADMs ativos da unidade. Deve ser
    /// chamado dentro da transação que criou a solicitação, depois de o id estar gravado.
    /// </summary>
    public void NovaSolicitacao(SolicitacaoDeAcesso solicitacao, int? autorId, DateTime utc)
    {
        var destinatarios = _usuarios
            .Listar(new FiltroUsuarios(
                PapelUsuario.AdminSecretaria,
                null,
                solicitacao.UnidadeId,
                true,
                null))
            .Select(usuario => usuario.Id)
            .Where(id => id != autorId);

        _notificacoes.NotificarVariosUmaVez(
            destinatarios,
            TipoNotificacao.SolicitacaoAcesso,
            $"Nova solicitação de acesso #{solicitacao.Id}",
            $"Solicitação de {solicitacao.TipoSolicitado} aguardando análise.",
            "SolicitacaoAcesso",
            solicitacao.Id,
            utc);
    }
}
