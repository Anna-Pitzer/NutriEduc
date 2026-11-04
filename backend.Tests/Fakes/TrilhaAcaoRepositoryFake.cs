using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>Trilha em memória para os testes de serviço (Spec 028, T7).</summary>
public class TrilhaAcaoRepositoryFake : ITrilhaAcaoRepository
{
    private readonly List<TrilhaAcao> _acoes = new();

    public void Add(TrilhaAcao acao) => _acoes.Add(acao);

    public List<TrilhaAcao> Listar() => _acoes.ToList();

    public List<TrilhaAcao> GetAcoes() => _acoes.ToList();
}
