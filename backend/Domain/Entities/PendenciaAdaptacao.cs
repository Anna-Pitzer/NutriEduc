namespace Ntc.Domain.Entity;

/// <summary>
/// Motivo pelo qual uma versão adaptada ficou <c>PendenteDeRevisao</c> (Spec 029, RF7 e RF8).
///
/// A pendência é por item — ou por refeição, quando o total sai da tolerância — e permite
/// que o detalhe mostre exatamente o que impediu a publicação, em vez de um aviso genérico.
/// </summary>
public class PendenciaAdaptacao
{
    public int Id { get; private set; }

    public int CardapioId { get; private set; }

    public int RefeicaoOrdem { get; private set; }

    public string RefeicaoNome { get; private set; } = string.Empty;

    /// <summary>Alimento que não pôde ser substituído; nulo quando a pendência é da refeição.</summary>
    public int? AlimentoId { get; private set; }

    public string? AlimentoNome { get; private set; }

    public string Motivo { get; private set; } = string.Empty;

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private PendenciaAdaptacao()
    {
    }

    internal PendenciaAdaptacao(
        int refeicaoOrdem,
        string refeicaoNome,
        int? alimentoId,
        string? alimentoNome,
        string motivo)
    {
        if (refeicaoOrdem <= 0)
            throw new ArgumentException("A ordem da refeição deve ser positiva.");

        if (alimentoId is <= 0)
            throw new ArgumentException("O alimento da pendência deve ser válido.");

        RefeicaoOrdem = refeicaoOrdem;
        RefeicaoNome = ValidacaoDeCampo.Texto(refeicaoNome, "nome da refeição", 2, 60);
        AlimentoId = alimentoId;
        AlimentoNome = string.IsNullOrWhiteSpace(alimentoNome) ? null : alimentoNome.Trim();
        Motivo = ValidacaoDeCampo.Texto(motivo, "motivo da pendência", 2, 500);
    }
}
