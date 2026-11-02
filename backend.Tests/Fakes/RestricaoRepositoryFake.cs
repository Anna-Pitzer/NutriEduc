using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Catálogo em memória para os testes de serviço (Spec 010). O construtor recebe os nomes
/// já ativos, porque a validação do caso de uso precisa que eles existam no catálogo.
///
/// Limite declarado: o histórico vive na coleção do agregado <c>Aluno</c>, que este fake
/// não modela — <see cref="ListarHistorico"/> serve apenas para satisfazer o contrato. A
/// leitura paginada é verificada nos testes HTTP, com SQLite real.
/// </summary>
public class RestricaoRepositoryFake : IRestricaoRepository
{
    private readonly List<RestricaoAlimentar> _catalogo = new();
    private int _proximoId = 1;

    public RestricaoRepositoryFake(params string[] nomesAtivos)
    {
        foreach (var nome in nomesAtivos)
        {
            Add(new RestricaoAlimentar(nome, CategoriaRestricao.Restricao));
        }
    }

    public List<RestricaoAlimentar> ObterAtivasPorNomes(IEnumerable<string> nomesNormalizados)
    {
        var nomes = nomesNormalizados.ToList();

        return _catalogo
            .Where(item => item.Ativa && nomes.Contains(item.NomeNormalizado))
            .ToList();
    }

    public List<RestricaoAlimentar> Listar(CategoriaRestricao? categoria, bool incluirInativas)
    {
        var consulta = _catalogo.AsEnumerable();

        if (categoria is { } valor)
        {
            consulta = consulta.Where(item => item.Categoria == valor);
        }

        if (!incluirInativas)
        {
            consulta = consulta.Where(item => item.Ativa);
        }

        return consulta
            .OrderBy(item => item.Categoria)
            .ThenBy(item => item.Nome)
            .ToList();
    }

    public RestricaoAlimentar? Obter(int id) =>
        _catalogo.FirstOrDefault(item => item.Id == id);

    public bool ExisteNomeNormalizado(
        string nomeNormalizado,
        CategoriaRestricao categoria,
        int? ignorarId)
    {
        return _catalogo.Any(item =>
            item.NomeNormalizado == nomeNormalizado &&
            item.Categoria == categoria &&
            (ignorarId == null || item.Id != ignorarId));
    }

    public RestricaoAlimentar Add(RestricaoAlimentar restricao)
    {
        if (restricao.Id == 0)
        {
            PropriedadesPrivadas.Definir(restricao, nameof(RestricaoAlimentar.Id), _proximoId++);
        }

        _catalogo.Add(restricao);

        return restricao;
    }

    public void Update(RestricaoAlimentar restricao)
    {
        var indice = _catalogo.FindIndex(item => item.Id == restricao.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Restrição {restricao.Id} não existe no catálogo fake.");

        _catalogo[indice] = restricao;
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<RestricaoAlimentar> GetCatalogo() => _catalogo.ToList();

    /// <summary>Somente para asserções dos testes: desativa sem passar pelo serviço.</summary>
    public RestricaoAlimentar Desativar(string nome)
    {
        var item = _catalogo.Single(entrada =>
            string.Equals(entrada.Nome, nome, StringComparison.OrdinalIgnoreCase));

        item.Desativar();

        return item;
    }
}
