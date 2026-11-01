using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta das categorias de alimento (Spec 011, RF1). Categoria não é apenas um rótulo de
/// tela: ela condiciona o cadastro, porque categoria desativada não recebe alimento novo.
/// </summary>
public interface ICategoriaAlimentoRepository
{
    /// <summary>Categorias em ordem de nome; inativas apenas quando pedido.</summary>
    List<CategoriaAlimento> Listar(bool incluirInativas);

    CategoriaAlimento? Obter(int id);

    /// <summary>Detecta a violação do índice único antes de gravar, para devolver 409.</summary>
    bool ExisteNomeNormalizado(string nomeNormalizado, int? ignorarId);

    CategoriaAlimento Add(CategoriaAlimento categoria);

    void Update(CategoriaAlimento categoria);
}
