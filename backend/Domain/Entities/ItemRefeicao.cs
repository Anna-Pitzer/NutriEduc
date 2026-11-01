namespace Ntc.Domain.Entity;

/// <summary>
/// Alimento dentro de uma refeição, com a quantidade servida (Spec 012, RF3).
///
/// Não tem identidade própria: a chave é o par <c>(RefeicaoId, AlimentoId)</c>, que é
/// também a garantia de que o mesmo alimento não aparece duas vezes na refeição — em
/// refeições diferentes, pode.
///
/// A quantidade usa a regra de medida do projeto (<see cref="ValidacaoDeCampo.Medida"/>)
/// e acrescenta o limite de 5000 g, que é específico da refeição.
/// </summary>
public class ItemRefeicao
{
    public const decimal GramasMaximas = 5000m;

    public int RefeicaoId { get; private set; }

    public int AlimentoId { get; private set; }

    public decimal QuantidadeGramas { get; private set; }

    /// <summary>Carregado nas consultas de detalhe para nome e composição (Spec 011).</summary>
    public Alimento? Alimento { get; private set; }

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private ItemRefeicao()
    {
    }

    internal ItemRefeicao(int alimentoId, decimal gramas)
    {
        if (alimentoId <= 0)
        {
            throw new ArgumentException("O item exige um alimento válido.");
        }

        var quantidade = ValidacaoDeCampo.Medida(gramas, "quantidade em gramas");

        if (quantidade <= 0 || quantidade > GramasMaximas)
        {
            throw new ArgumentException(
                $"A quantidade deve ser maior que 0 e no máximo {GramasMaximas} gramas.");
        }

        AlimentoId = alimentoId;
        QuantidadeGramas = quantidade;
    }
}
