using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de leitura e escrita dos parâmetros de geração (Spec 013). Recebe o
/// <c>ConnectionContext</c> por construtor (L4) e nunca abre conexão própria.
/// </summary>
public interface IParametroGeracaoRepository
{
    ParametroGeracao? ObterPorUnidade(int unidadeId);

    ParametroGeracao Add(ParametroGeracao parametro);

    void Update(ParametroGeracao parametro);
}
