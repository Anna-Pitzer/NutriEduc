using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de persistência da notificação (Spec 018). A escrita é emitida na mesma unidade de
/// trabalho da mudança de origem; a leitura é sempre restrita ao destinatário (RF10/RF11).
/// </summary>
public interface INotificacaoRepository
{
    Notificacao Add(Notificacao notificacao);

    void Update(Notificacao notificacao);

    /// <summary>Busca a notificação apenas quando pertence ao destinatário (RF11).</summary>
    Notificacao? Obter(int id, int destinatarioId);

    /// <summary>Listagem paginada e ordenada por data decrescente do destinatário.</summary>
    (List<Notificacao> Itens, int Total) Listar(
        int destinatarioId,
        bool? naoLidas,
        int pagina,
        int tamanho);

    int ContarNaoLidas(int destinatarioId);

    /// <summary>True quando o destinatário possui ao menos uma notificação (ADR-014).</summary>
    bool ExistePorDestinatario(int destinatarioId);

    /// <summary>
    /// True quando o destinatário já recebeu a notificação do mesmo evento, identificado por
    /// (tipo, referência). Evita duplicidade quando o mesmo fato é processado mais de uma vez.
    /// </summary>
    bool ExistePorReferencia(
        int destinatarioId,
        TipoNotificacao tipo,
        string referenciaTipo,
        int referenciaId);

    void MarcarTodasComoLidas(int destinatarioId);
}
