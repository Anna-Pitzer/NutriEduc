namespace Ntc.Domain.Entity;

/// <summary>
/// Registro append-only de uma substituição feita em uma versão adaptada (Spec 029, RF5 e
/// T5). Guarda o item original, o substituto escolhido e as restrições que bloquearam o
/// original, para que o detalhe explique o que mudou e por quê sem recalcular a adaptação.
/// </summary>
public class SubstituicaoDeItem
{
    public int Id { get; private set; }

    public int CardapioId { get; private set; }

    public int RefeicaoOrdem { get; private set; }

    public string RefeicaoNome { get; private set; } = string.Empty;

    public int AlimentoOriginalId { get; private set; }

    public string AlimentoOriginalNome { get; private set; } = string.Empty;

    public int AlimentoSubstitutoId { get; private set; }

    public string AlimentoSubstitutoNome { get; private set; } = string.Empty;

    public decimal QuantidadeGramas { get; private set; }

    /// <summary>Nomes das restrições do conjunto que bloqueiam o alimento original.</summary>
    public string RestricoesBloqueantes { get; private set; } = string.Empty;

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private SubstituicaoDeItem()
    {
    }

    internal SubstituicaoDeItem(
        int refeicaoOrdem,
        string refeicaoNome,
        int alimentoOriginalId,
        string alimentoOriginalNome,
        int alimentoSubstitutoId,
        string alimentoSubstitutoNome,
        decimal quantidadeGramas,
        string restricoesBloqueantes)
    {
        if (refeicaoOrdem <= 0)
            throw new ArgumentException("A ordem da refeição deve ser positiva.");

        if (alimentoOriginalId <= 0 || alimentoSubstitutoId <= 0)
            throw new ArgumentException("A substituição exige os dois alimentos.");

        if (alimentoOriginalId == alimentoSubstitutoId)
            throw new ArgumentException("O substituto não pode ser o próprio alimento original.");

        if (quantidadeGramas <= 0)
            throw new ArgumentException("A quantidade substituída deve ser positiva.");

        RefeicaoOrdem = refeicaoOrdem;
        RefeicaoNome = ValidacaoDeCampo.Texto(refeicaoNome, "nome da refeição", 2, 60);
        AlimentoOriginalId = alimentoOriginalId;
        AlimentoOriginalNome = ValidacaoDeCampo.Texto(alimentoOriginalNome, "alimento original", 2, 100);
        AlimentoSubstitutoId = alimentoSubstitutoId;
        AlimentoSubstitutoNome = ValidacaoDeCampo.Texto(alimentoSubstitutoNome, "alimento substituto", 2, 100);
        QuantidadeGramas = quantidadeGramas;
        RestricoesBloqueantes = (restricoesBloqueantes ?? string.Empty).Trim();
    }
}
