using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Registro de unidades que já usaram o token de criação de ambiente (Spec 022 v2).
/// O índice único por unidade é a garantia final; este contrato é a checagem prévia
/// que permite devolver 409 com mensagem em vez de erro de banco.
/// </summary>
public interface IConsumoBootstrapRepository
{
    bool UnidadeJaConsumiu(int unidadeId);

    void Registrar(ConsumoBootstrap consumo);
}
