namespace Ntc.Domain.Entity;

/// <summary>
/// Evento da trilha append-only do cardápio (Spec 015, RF9). Cada transição e cada edição de
/// rascunho geram exatamente um evento, com autor e data em snapshot (ADR-009).
/// </summary>
public enum EventoCardapio
{
    Criado,
    Editado,
    Duplicado,
    RevisaoCriada,
    Publicado,
    Cancelado,
    Substituido
}
