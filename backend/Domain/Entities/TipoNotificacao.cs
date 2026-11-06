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
    FeedbackRespondido
}
