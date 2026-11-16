namespace Ntc.Domain.Entity;

/// <summary>
/// Evento append-only do ciclo de status da ocorrência (Spec 016, RNF2). Guarda o status
/// alcançado, o autor (snapshot) e a observação; não existe edição nem exclusão de evento.
/// </summary>
public class HistoricoOcorrencia
{
    public int Id { get; private set; }

    public int OcorrenciaId { get; private set; }

    public StatusOcorrencia Status { get; private set; }

    public string AutorNome { get; private set; } = string.Empty;

    /// <summary>Observação da transição; "Criação" no evento inicial.</summary>
    public string Observacao { get; private set; } = string.Empty;

    public DateTime CriadoEmUtc { get; private set; }

    private HistoricoOcorrencia()
    {
    }

    internal HistoricoOcorrencia(
        StatusOcorrencia status,
        string autorNome,
        string observacao,
        DateTime criadoEmUtc)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Status de ocorrência inválido.");

        Status = status;
        AutorNome = (autorNome ?? string.Empty).Trim();
        Observacao = (observacao ?? string.Empty).Trim();
        CriadoEmUtc = criadoEmUtc.Kind == DateTimeKind.Utc
            ? criadoEmUtc
            : criadoEmUtc.ToUniversalTime();

        if (AutorNome.Length == 0)
            throw new ArgumentException("O autor do evento de ocorrência é obrigatório.");
    }
}
