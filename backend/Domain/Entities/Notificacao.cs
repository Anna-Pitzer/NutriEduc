namespace Ntc.Domain.Entity;

/// <summary>
/// Notificação de um destinatário (Spec 018, RF6). O conteúdo nunca inclui CPF, e-mail,
/// telefone ou dado clínico além do necessário; a referência aponta para o recurso de
/// origem sem duplicar o dado.
/// </summary>
public class Notificacao
{
    public const int TituloMin = 5;
    public const int TituloMax = 120;
    public const int MensagemMin = 5;
    public const int MensagemMax = 500;
    public const int ReferenciaTipoMax = 60;

    public int Id { get; private set; }

    public int DestinatarioId { get; private set; }

    public TipoNotificacao Tipo { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Mensagem { get; private set; } = string.Empty;

    public string? ReferenciaTipo { get; private set; }

    public int? ReferenciaId { get; private set; }

    public bool Lida { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    private Notificacao()
    {
    }

    public Notificacao(
        int destinatarioId,
        TipoNotificacao tipo,
        string? titulo,
        string? mensagem,
        string? referenciaTipo,
        int? referenciaId,
        DateTime criadoEmUtc)
    {
        if (destinatarioId <= 0)
            throw new ArgumentException("A notificação exige um destinatário válido.");

        if (!Enum.IsDefined(tipo))
            throw new ArgumentException("Tipo de notificação inválido.");

        DestinatarioId = destinatarioId;
        Tipo = tipo;
        Titulo = ValidacaoDeCampo.Texto(titulo, "título", TituloMin, TituloMax);
        Mensagem = ValidacaoDeCampo.Texto(mensagem, "mensagem", MensagemMin, MensagemMax);
        ReferenciaTipo = string.IsNullOrWhiteSpace(referenciaTipo)
            ? null
            : referenciaTipo.Trim();

        if (ReferenciaTipo is { Length: > ReferenciaTipoMax })
            throw new ArgumentException(
                $"O tipo da referência deve ter no máximo {ReferenciaTipoMax} caracteres.");

        ReferenciaId = referenciaId;
        Lida = false;
        CriadoEmUtc = criadoEmUtc.Kind == DateTimeKind.Utc
            ? criadoEmUtc
            : criadoEmUtc.ToUniversalTime();
    }

    public void MarcarComoLida() => Lida = true;
}
