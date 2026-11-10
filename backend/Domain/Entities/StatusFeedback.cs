namespace Ntc.Domain.Entity;

/// <summary>
/// Ciclo de vida do feedback (Spec 018, RF1): aberto, respondido ou encerrado pelo autor.
/// </summary>
public enum StatusFeedback
{
    Aberto,
    Respondido,
    Encerrado
}
