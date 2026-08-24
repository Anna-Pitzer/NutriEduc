namespace Ntc.Model;

public interface IUsuarioRepository
{
    void RegisterUser(Usuario user);
    int LoginUser (Usuario user);
    
    string GetName(string email);
    List<Usuario> ListUsers();
}