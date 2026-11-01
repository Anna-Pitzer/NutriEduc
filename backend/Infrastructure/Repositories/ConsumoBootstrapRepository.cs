using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

public class ConsumoBootstrapRepository : IConsumoBootstrapRepository
{
    private readonly ConnectionContext _contexto;

    public ConsumoBootstrapRepository(ConnectionContext contexto)
    {
        _contexto = contexto;
    }

    public bool UnidadeJaConsumiu(int unidadeId)
    {
        return _contexto.ConsumosBootstrap.Any(consumo => consumo.UnidadeId == unidadeId);
    }

    public void Registrar(ConsumoBootstrap consumo)
    {
        _contexto.ConsumosBootstrap.Add(consumo);
    }
}
