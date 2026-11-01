namespace Ntc.Domain.Entity;

/// <summary>
/// Registra que uma unidade já consumiu o token de criação de ambiente (Spec 022 v2).
/// Existe para garantir o "uma vez por unidade": o token de configuração continua o
/// mesmo, mas cada unidade só pode originar um administrador por ele.
/// </summary>
public class ConsumoBootstrap
{
    public int Id { get; private set; }

    public int UnidadeId { get; private set; }

    public DateTime ConsumidoEm { get; private set; }

    /// <summary>Somente para o EF Core.</summary>
    private ConsumoBootstrap()
    {
    }

    public ConsumoBootstrap(int unidadeId, DateTime consumidoEm)
    {
        if (unidadeId <= 0)
        {
            throw new ArgumentException("Consumo de bootstrap exige unidade válida.");
        }

        UnidadeId = unidadeId;
        ConsumidoEm = consumidoEm;
    }
}
