namespace Ntc.Domain.Entity;

/// <summary>
/// Trilha append-only de ação privilegiada (Spec 028, RF11; ADR-009). Guarda autor,
/// alvo e valores anterior/novo. A trilha do aluno (Spec 023) continua dona do histórico
/// de cadastro; esta cobre papel, vínculo, transferência de escola e desativação.
/// </summary>
public class TrilhaAcao
{
    public int Id { get; private set; }

    public string Tipo { get; private set; } = string.Empty;

    public int AutorId { get; private set; }

    /// <summary>Snapshot do nome do autor; não é FK para não perder o histórico.</summary>
    public string AutorNome { get; private set; } = string.Empty;

    public string AlvoTipo { get; private set; } = string.Empty;

    public int? AlvoId { get; private set; }

    public string? ValorAnterior { get; private set; }

    public string? ValorNovo { get; private set; }

    public DateTime EmUtc { get; private set; }

    /// <summary>Somente para o EF Core.</summary>
    private TrilhaAcao()
    {
    }

    public TrilhaAcao(
        string tipo,
        int autorId,
        string autorNome,
        string alvoTipo,
        int? alvoId,
        string? valorAnterior,
        string? valorNovo,
        DateTime emUtc)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            throw new ArgumentException("O tipo da ação é obrigatório.");
        }

        if (autorId <= 0)
        {
            throw new ArgumentException("O autor da ação é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(autorNome))
        {
            throw new ArgumentException("O nome do autor é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(alvoTipo))
        {
            throw new ArgumentException("O tipo do alvo é obrigatório.");
        }

        if (emUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A data da trilha deve estar em UTC.");
        }

        Tipo = tipo.Trim();
        AutorId = autorId;
        AutorNome = autorNome.Trim();
        AlvoTipo = alvoTipo.Trim();
        AlvoId = alvoId;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
        EmUtc = emUtc;
    }
}
