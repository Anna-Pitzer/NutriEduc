namespace Ntc.Domain.Entity;

/// <summary>
/// Tipos mínimos de notificação central (Spec 018, RF7). A emissão é feita por um único
/// serviço; as specs consumidoras não criam notificações diretamente.
/// </summary>
public enum TipoNotificacao
{
    CardapioPublicado,
    CardapioAlterado,
    SolicitacaoStatus,
    OcorrenciaStatus,
    AlunoAtualizado,
    FeedbackRespondido,

    /// <summary>Nova solicitação de acesso criada e aguardando decisão do ADM da unidade.</summary>
    SolicitacaoAcesso,

    /// <summary>Nova solicitação (alergia, cardápio, ingrediente ou dado cadastral) criada.</summary>
    NovaSolicitacao,

    /// <summary>Nova ocorrência registrada, aguardando análise da equipe responsável.</summary>
    OcorrenciaRegistrada
}
