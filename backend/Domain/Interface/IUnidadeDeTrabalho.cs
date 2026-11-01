namespace Ntc.Domain.Interface;

/// <summary>
/// Unidade de trabalho (Spec 024, F6). O caso de uso decide quando a operação termina; o
/// repositório apenas prepara o estado. Isso torna atômicos os fluxos que tocam mais de um
/// agregado (bootstrap e vínculo) e elimina a decisão de transação de dentro da
/// infraestrutura.
/// </summary>
public interface IUnidadeDeTrabalho
{
    /// <summary>
    /// Executa a operação numa transação e confirma as mudanças no fim. Qualquer exceção
    /// desfaz tudo: o banco volta ao estado anterior.
    /// </summary>
    T ExecutarEmTransacao<T>(Func<T> operacao);

    /// <summary>Versão sem retorno de <see cref="ExecutarEmTransacao{T}"/>.</summary>
    void ExecutarEmTransacao(Action operacao);
}
