namespace Ntc.Domain.Entity;

/// <summary>
/// Evento append-only do ciclo de status da solicitação (Spec 017, RNF2).
/// Guarda o status alcançado, o autor (snapshot), a resposta da decisão e a data UTC; não
/// existe edição nem exclusão de evento.
/// </summary>
public class HistoricoSolicitacao
{
    public int Id { get; private set; }

    public int SolicitacaoId { get; private set; }

    public StatusSolicitacao Status { get; private set; }

    public string AutorNome { get; private set; } = string.Empty;

    /// <summary>Resposta da decisão; nula em eventos de análise, cancelamento e conclusão.</summary>
    public string? Resposta { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    private HistoricoSolicitacao()
    {
    }

    internal HistoricoSolicitacao(
        int solicitacaoId,
        StatusSolicitacao status,
        string autorNome,
        string? resposta,
        DateTime criadoEmUtc)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Status de solicitação inválido.");

        SolicitacaoId = solicitacaoId;
        Status = status;
        AutorNome = (autorNome ?? string.Empty).Trim();
        Resposta = string.IsNullOrWhiteSpace(resposta) ? null : resposta.Trim();
        CriadoEmUtc = criadoEmUtc.Kind == DateTimeKind.Utc
            ? criadoEmUtc
            : criadoEmUtc.ToUniversalTime();

        if (AutorNome.Length == 0)
            throw new ArgumentException("O autor do evento de solicitação é obrigatório.");
    }
}
