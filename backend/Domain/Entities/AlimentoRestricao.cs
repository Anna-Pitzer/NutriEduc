namespace Ntc.Domain.Entity;

/// <summary>
/// Vínculo entre um alimento e uma restrição do catálogo da Spec 010 (Spec 011, RF4).
///
/// O vínculo é apenas o par <c>(AlimentoId, RestricaoId)</c>: não existe motivo próprio.
/// A severidade é sempre a <see cref="RestricaoAlimentar.Categoria"/> — <c>Alergia</c>
/// bloqueia, <c>Restricao</c> alerta —, de modo que reclassificar a restrição muda a
/// severidade de todos os alimentos vinculados sem reescrever vínculo algum.
/// </summary>
public class AlimentoRestricao
{
    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private AlimentoRestricao()
    {
    }

    public AlimentoRestricao(int restricaoId)
    {
        if (restricaoId <= 0)
            throw new ArgumentException("Restrição inválida para vínculo com alimento.");

        RestricaoId = restricaoId;
    }

    public int AlimentoId { get; private set; }

    public int RestricaoId { get; private set; }

    /// <summary>
    /// Restrição do vínculo, carregada nas consultas de leitura. É referência de outro
    /// agregado: o vínculo guarda o id, e a navegação existe apenas para não repetir
    /// consulta na montagem da resposta.
    /// </summary>
    public RestricaoAlimentar? Restricao { get; private set; }
}
