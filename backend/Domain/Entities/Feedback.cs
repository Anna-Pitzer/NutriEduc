using Ntc.Domain.Services;

namespace Ntc.Domain.Entity;

/// <summary>
/// Canal de retorno bidirecional (Spec 018, RF1–RF5). O agregado é dono do ciclo de status
/// (<see cref="StatusFeedback"/>) e das transições <see cref="Responder"/> e
/// <see cref="Encerrar"/>; o serviço decide quem pode responder/encerrar por papel e escopo.
/// </summary>
public class Feedback
{
    public const int AssuntoMin = 5;
    public const int AssuntoMax = 120;
    public const int MensagemMin = 5;
    public const int MensagemMax = 1000;
    public const int RespostaMin = 5;
    public const int RespostaMax = 1000;

    public int Id { get; private set; }

    public string Assunto { get; private set; } = string.Empty;

    public string Mensagem { get; private set; } = string.Empty;

    public int AutorId { get; private set; }

    public string AutorNome { get; private set; } = string.Empty;

    public PapelUsuario PapelAutor { get; private set; }

    public int UnidadeId { get; private set; }

    public int? EscolaId { get; private set; }

    public StatusFeedback Status { get; private set; }

    public string? Resposta { get; private set; }

    public string? RespondidoPor { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Feedback()
    {
    }

    public Feedback(
        string? assunto,
        string? mensagem,
        int autorId,
        string? autorNome,
        PapelUsuario papel,
        int unidadeId,
        int? escolaId,
        DateTime utc)
    {
        if (autorId <= 0)
            throw new ArgumentException("O feedback exige um autor válido.");

        if (!Enum.IsDefined(papel))
            throw new ArgumentException("Papel do autor inválido.");

        if (unidadeId <= 0)
            throw new ArgumentException("O feedback exige uma unidade válida.");

        Assunto = ValidacaoDeCampo.Texto(assunto, "assunto", AssuntoMin, AssuntoMax);
        Mensagem = ValidacaoDeCampo.Texto(mensagem, "mensagem", MensagemMin, MensagemMax);
        AutorId = autorId;
        AutorNome = ValidacaoDeCampo.Texto(autorNome, "autor", 2, 100);
        PapelAutor = papel;
        UnidadeId = unidadeId;
        EscolaId = escolaId is > 0 ? escolaId : null;
        Status = StatusFeedback.Aberto;
        CriadoEmUtc = AtualizadoEmUtc = NormalizarUtc(utc);
    }

    public void Responder(string? resposta, string? autorNome, DateTime utc)
    {
        if (Status != StatusFeedback.Aberto)
            throw new TransicaoInvalidaException("Este feedback já foi respondido.");

        Resposta = ValidacaoDeCampo.Texto(resposta, "resposta", RespostaMin, RespostaMax);
        RespondidoPor = ValidacaoDeCampo.Texto(autorNome, "autor", 2, 100);
        Status = StatusFeedback.Respondido;
        AtualizadoEmUtc = NormalizarUtc(utc);
    }

    public void Encerrar(string? autorNome, DateTime utc)
    {
        if (Status != StatusFeedback.Respondido)
            throw new TransicaoInvalidaException("Somente feedback respondido pode ser encerrado.");

        Status = StatusFeedback.Encerrado;
        AtualizadoEmUtc = NormalizarUtc(utc);
    }

    private static DateTime NormalizarUtc(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
}
