using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Categorias em memória para os testes de serviço (Spec 011, RF1). O construtor recebe
/// os nomes já ativos, porque o cadastro de alimento depende de categoria existente.
/// </summary>
public class CategoriaAlimentoRepositoryFake : ICategoriaAlimentoRepository
{
    private readonly List<CategoriaAlimento> _categorias = new();
    private int _proximoId = 1;

    public CategoriaAlimentoRepositoryFake(params string[] nomesAtivos)
    {
        foreach (var nome in nomesAtivos)
        {
            Add(new CategoriaAlimento(nome));
        }
    }

    public List<CategoriaAlimento> Listar(bool incluirInativas) =>
        _categorias
            .Where(categoria => incluirInativas || categoria.Ativa)
            .OrderBy(categoria => categoria.Nome)
            .ToList();

    public CategoriaAlimento? Obter(int id) =>
        _categorias.FirstOrDefault(categoria => categoria.Id == id);

    public bool ExisteNomeNormalizado(string nomeNormalizado, int? ignorarId) =>
        _categorias.Any(categoria =>
            categoria.NomeNormalizado == nomeNormalizado &&
            (ignorarId == null || categoria.Id != ignorarId));

    public CategoriaAlimento Add(CategoriaAlimento categoria)
    {
        if (categoria.Id == 0)
        {
            PropriedadesPrivadas.Definir(categoria, nameof(CategoriaAlimento.Id), _proximoId++);
        }

        _categorias.Add(categoria);

        return categoria;
    }

    public void Update(CategoriaAlimento categoria)
    {
        var indice = _categorias.FindIndex(item => item.Id == categoria.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Categoria {categoria.Id} não existe no catálogo fake.");

        _categorias[indice] = categoria;
    }

    /// <summary>Somente para asserções dos testes: desativa sem passar pelo serviço.</summary>
    public CategoriaAlimento Desativar(string nome)
    {
        var categoria = _categorias.Single(entrada =>
            string.Equals(entrada.Nome, nome, StringComparison.OrdinalIgnoreCase));

        categoria.Desativar();

        return categoria;
    }
}
