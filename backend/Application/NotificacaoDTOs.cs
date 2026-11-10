using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Notificação do destinatário autenticado (Spec 018, RF10/RF12). O conteúdo nunca carrega
/// CPF, e-mail, telefone ou dado clínico além do nome do aluno quando necessário (RF13).
/// </summary>
public class NotificacaoDTO
{
    public int Id { get; set; }

    public TipoNotificacao Tipo { get; set; }

    public string Titulo { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;

    public string? ReferenciaTipo { get; set; }

    public int? ReferenciaId { get; set; }

    public bool Lida { get; set; }

    public DateTime CriadoEm { get; set; }
}
