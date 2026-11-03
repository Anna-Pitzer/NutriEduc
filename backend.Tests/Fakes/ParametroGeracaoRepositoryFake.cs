using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>Repositório em memória dos parâmetros de geração para os testes de serviço (Spec 013).</summary>
public class ParametroGeracaoRepositoryFake : IParametroGeracaoRepository
{
    private readonly List<ParametroGeracao> _parametros = new();
    private int _proximoId = 1;

    public ParametroGeracao? ObterPorUnidade(int unidadeId) =>
        _parametros.FirstOrDefault(parametro => parametro.UnidadeId == unidadeId);

    public ParametroGeracao Add(ParametroGeracao parametro)
    {
        if (parametro.Id == 0)
            PropriedadesPrivadas.Definir(parametro, nameof(ParametroGeracao.Id), _proximoId++);

        _parametros.Add(parametro);

        return parametro;
    }

    public void Update(ParametroGeracao parametro)
    {
        var indice = _parametros.FindIndex(item => item.Id == parametro.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Parâmetro {parametro.Id} não existe no fake.");

        _parametros[indice] = parametro;
    }

    public List<ParametroGeracao> GetParametros() => _parametros.ToList();
}
