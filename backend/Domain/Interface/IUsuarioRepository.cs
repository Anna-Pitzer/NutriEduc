using Ntc.Domain.Entity;
namespace Ntc.Domain.Interface;

public interface IUsuarioRepository
{
    void Add(Usuario usuario);
    Usuario? GetById(int id);
    Usuario? GetByEmail(string email);
    bool ExistsByEmail(string email);
    void Update(Usuario usuario);

    /// <summary>
    /// Lista usuários aplicando o escopo do solicitante e os filtros da RF7. O escopo
    /// vai para a consulta SQL; AdminEscola nunca enxerga outra escola.
    /// </summary>
    /// <summary>
    /// Filtro de negócio já com o recorte do solicitante incorporado pela aplicação
    /// (Spec 024, F5). O porto não conhece sessão nem papel.
    /// </summary>
    List<Usuario> Listar(FiltroUsuarios filtro);

    /// <summary>Exclusão definitiva: o chamador já verificou que não há referências.</summary>
    void Delete(Usuario usuario);

    bool ExistePorEscola(int escolaId);
    bool ExistePorUnidade(int unidadeId);
    int ContarPorPapel(PapelUsuario papel);
}
