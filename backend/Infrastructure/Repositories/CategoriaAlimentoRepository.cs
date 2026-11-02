using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Categorias do catálogo de alimentos (Spec 011, RF1). Mesmo padrão do
/// <see cref="RestricaoRepository"/>: recebe o <see cref="ConnectionContext"/> por
/// construtor (L4) e não abre conexão própria.
/// </summary>
public class CategoriaAlimentoRepository : ICategoriaAlimentoRepository
{
    private readonly ConnectionContext _context;

    public CategoriaAlimentoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public List<CategoriaAlimento> Listar(bool incluirInativas)
    {
        var consulta = _context.CategoriasAlimento.AsQueryable();

        if (!incluirInativas)
        {
            consulta = consulta.Where(categoria => categoria.Ativa);
        }

        return consulta
            .OrderBy(categoria => categoria.Nome)
            .ToList();
    }

    public CategoriaAlimento? Obter(int id) =>
        _context.CategoriasAlimento.FirstOrDefault(categoria => categoria.Id == id);

    public bool ExisteNomeNormalizado(string nomeNormalizado, int? ignorarId) =>
        _context.CategoriasAlimento.Any(categoria =>
            categoria.NomeNormalizado == nomeNormalizado &&
            (ignorarId == null || categoria.Id != ignorarId));

    public CategoriaAlimento Add(CategoriaAlimento categoria)
    {
        _context.CategoriasAlimento.Add(categoria);
        return categoria;
    }

    public void Update(CategoriaAlimento categoria)
    {
        _context.CategoriasAlimento.Update(categoria);
    }
}
