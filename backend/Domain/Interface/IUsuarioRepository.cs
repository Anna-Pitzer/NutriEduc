using Ntc.Domain.Entity;
namespace Ntc.Domain.Interface;

public interface IUsuarioRepository
{
    void Add(Usuario usuario);
    Usuario? GetById(int id);
    Usuario? GetByEmail(string email);
    bool ExistsByEmail(string email);
    void Update(Usuario usuario);

    
}