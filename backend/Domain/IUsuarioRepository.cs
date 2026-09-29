namespace Ntc.Domain.Entity;

public interface IUsuarioRepository
{
    bool RegisterUser(Usuario user);
    bool LoginUser (string email, string senha);
    string? GetName(string email);
    int GetIdByEmail(string email);
    Usuario GetById(int id);
    void ChangePassword(int id, string senha);
}