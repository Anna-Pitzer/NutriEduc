namespace Ntc.Domain.Entity;

/// <summary>Ciclo de vida da ocorrência (Spec 016, RF4).</summary>
public enum StatusOcorrencia
{
    Aberta,
    EmAnalise,
    Resolvida,
    Arquivada,
    Cancelada
}
