using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória de escolas e unidades. Só existe para os testes de serviço que
/// precisam validar existência de escola; a unicidade real é garantida pelo índice do
/// banco e testada nos testes HTTP.
/// </summary>
public class EscolaRepositoryFake : IEscolaRepository
{
    private readonly List<Escola> _escolas = new();
    private readonly List<UnidadeEscolar> _unidades = new();
    private int _proximaEscola = 1;
    private int _proximaUnidade = 1;

    public Escola Add(Escola escola)
    {
        DefinirId(escola, _proximaEscola++);
        _escolas.Add(escola);
        return escola;
    }

    public Escola? GetEscola(int id) => _escolas.FirstOrDefault(escola => escola.Id == id);

    public List<Escola> Listar() => _escolas.ToList();

    public bool ExisteNome(string nome, int? ignorarId = null)
    {
        var alvo = (nome ?? string.Empty).Trim();

        return _escolas.Any(escola =>
            string.Equals(escola.Nome, alvo, StringComparison.OrdinalIgnoreCase) &&
            (ignorarId is null || escola.Id != ignorarId));
    }

    public void Update(Escola escola)
    {
        var indice = _escolas.FindIndex(item => item.Id == escola.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Escola {escola.Id} não existe no repositório fake.");

        _escolas[indice] = escola;
    }

    public UnidadeEscolar AddUnidade(UnidadeEscolar unidade)
    {
        DefinirId(unidade, _proximaUnidade++);
        _unidades.Add(unidade);
        return unidade;
    }

    public UnidadeEscolar? GetUnidade(int id) =>
        _unidades.FirstOrDefault(unidade => unidade.Id == id);

    public List<UnidadeEscolar> ListarUnidades() =>
        _unidades.OrderBy(unidade => unidade.Nome).ToList();

    public List<UnidadeEscolar> BuscarUnidadesPorNome(string nome)
    {
        var alvo = (nome ?? string.Empty).Trim();

        return _unidades
            .Where(unidade => string.Equals(unidade.Nome, alvo, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public bool ExisteNomeUnidade(string nome, int? ignorarId = null)
    {
        var alvo = (nome ?? string.Empty).Trim();

        return _unidades.Any(unidade =>
            string.Equals(unidade.Nome, alvo, StringComparison.OrdinalIgnoreCase) &&
            (ignorarId is null || unidade.Id != ignorarId));
    }

    public void UpdateUnidade(UnidadeEscolar unidade)
    {
        var indice = _unidades.FindIndex(item => item.Id == unidade.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Unidade {unidade.Id} não existe no repositório fake.");

        _unidades[indice] = unidade;
    }

    public void Delete(Escola escola)
    {
        _escolas.RemoveAll(item => item.Id == escola.Id);
    }

    public void DeleteUnidade(UnidadeEscolar unidade)
    {
        _unidades.RemoveAll(item => item.Id == unidade.Id);
    }

    public bool ExisteUnidadeDaEscola(int escolaId) =>
        _unidades.Any(unidade => unidade.EscolaId == escolaId);

    private static void DefinirId(object entidade, int id)
    {
        PropriedadesPrivadas.Definir(entidade, "Id", id);
    }
}
