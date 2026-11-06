using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Unidade de trabalho em memória (Spec 024, F6). Os repositórios fake já atribuem o Id ao
/// inserir, então a transação não precisa confirmar nada: basta executar a operação. O que
/// os testes verificam aqui é a orquestração, não a persistência.
/// </summary>
public class UnidadeDeTrabalhoFake : IUnidadeDeTrabalho
{
    public int Transacoes { get; private set; }

    public int Salvamentos { get; private set; }

    public T ExecutarEmTransacao<T>(Func<T> operacao)
    {
        Transacoes++;

        return operacao();
    }

    public void ExecutarEmTransacao(Action operacao)
    {
        Transacoes++;

        operacao();
    }

    public void Salvar() => Salvamentos++;
}
