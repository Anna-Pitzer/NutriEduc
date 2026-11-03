namespace Ntc.Domain.Entity;

/// <summary>
/// Trilha append-only de eventos do cardápio (Spec 015, RF9 e RNF3). `AutorNome` e `Motivo`
/// são snapshots — renomear ou desativar o autor não reescreve o passado, e nenhuma FK liga
/// o evento a eles (ADR-009). Não existe endpoint de edição ou exclusão: a única remoção é o
/// cascade de `DELETE` de um rascunho (Spec 012, RF7).
/// </summary>
public class HistoricoCardapio
{
    private HistoricoCardapio()
    {
    }

    public int Id { get; private set; }

    public int CardapioId { get; private set; }

    public EventoCardapio Evento { get; private set; }

    public int Versao { get; private set; }

    public string AutorNome { get; private set; } = string.Empty;

    public string? Motivo { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    internal HistoricoCardapio(
        int cardapioId,
        EventoCardapio evento,
        int versao,
        string autorNome,
        string? motivo,
        DateTime criadoEmUtc)
    {
        CardapioId = cardapioId;
        Evento = evento;
        Versao = versao;
        AutorNome = string.IsNullOrWhiteSpace(autorNome) ? "sistema" : autorNome.Trim();
        Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        CriadoEmUtc = criadoEmUtc.Kind == DateTimeKind.Utc
            ? criadoEmUtc
            : criadoEmUtc.ToUniversalTime();
    }
}

/// <summary>
/// Linha do feed de alterações (Spec 015, RF11): o evento já resolvido com o nome do cardápio
/// e da unidade, para o serviço montar o DTO sem consulta extra por linha.
/// </summary>
public sealed record AlteracaoCardapioItem(
    int Id,
    int CardapioId,
    string CardapioNome,
    string UnidadeNome,
    EventoCardapio Evento,
    int Versao,
    string AutorNome,
    string? Motivo,
    DateTime CriadoEmUtc);
