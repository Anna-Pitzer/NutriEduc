using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Entrada de criação de feedback (Spec 018, RF1/RF2).</summary>
public class CriarFeedbackDTO
{
    public string? Assunto { get; set; }

    public string? Mensagem { get; set; }

    /// <summary>Quando informado, a escola do feedback é derivada do aluno (RF1).</summary>
    public int? AlunoId { get; set; }
}

/// <summary>Entrada de resposta do feedback (Spec 018, RF3).</summary>
public class ResponderFeedbackDTO
{
    public string? Resposta { get; set; }
}

/// <summary>
/// Linha da listagem de feedbacks. Nunca expõe o id do autor (Spec 028, isolamento). As
/// permissões são calculadas pelo serviço para a sessão corrente.
/// </summary>
public class FeedbackResumoDTO
{
    public int Id { get; set; }

    public string Assunto { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;

    public string Autor { get; set; } = string.Empty;

    public PapelUsuario PapelAutor { get; set; }

    public int UnidadeId { get; set; }

    public int? EscolaId { get; set; }

    public StatusFeedback Status { get; set; }

    public DateTime CriadoEm { get; set; }

    /// <summary>True quando a sessão pode responder este feedback (Nutricionista/Escola).</summary>
    public bool PodeResponder { get; set; }

    /// <summary>True quando a sessão é o autor e o feedback está respondido (RF4).</summary>
    public bool PodeEncerrar { get; set; }
}

/// <summary>Detalhe do feedback com a resposta e as permissões da sessão (RF3/RF4).</summary>
public class FeedbackDTO : FeedbackResumoDTO
{
    public string? Resposta { get; set; }

    public string? RespondidoPor { get; set; }

    public DateTime AtualizadoEm { get; set; }
}
