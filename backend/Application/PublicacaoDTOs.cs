using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Entrada de cancelamento (Spec 015, RF8): motivo obrigatório entre 2 e 500 caracteres.</summary>
public class CancelarCardapioDTO
{
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>Evento da linha do tempo do cardápio (Spec 015, RF10).</summary>
public class HistoricoCardapioDTO
{
    public EventoCardapio Evento { get; set; }
    public int Versao { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>Linha do feed de alterações (Spec 015, RF11), com nome do cardápio e da unidade.</summary>
public class AlteracaoCardapioDTO
{
    public int Id { get; set; }
    public int CardapioId { get; set; }
    public string CardapioNome { get; set; } = string.Empty;
    public string UnidadeNome { get; set; } = string.Empty;
    public EventoCardapio Evento { get; set; }
    public int Versao { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; }
}
