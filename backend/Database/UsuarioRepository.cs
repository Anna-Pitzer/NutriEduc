using Microsoft.Data.Sqlite;
using Ntc.Model;
namespace Ntc.Database;
public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConnectionContext _context = new();
    public void RegisterUser(Usuario user) {
        _context.Usuarios.Add(user);
        _context.SaveChanges();

    }
    
    public int LoginUser(Usuario user)
    {
        if(_context.Usuarios.Any(u => u.Email == user.Email && u.Senha == user.Senha))
        {
            return 1;
        }
        return 0;

    }

    public string GetName(string email)
    {
        var usuario = (_context.Usuarios.FirstOrDefault(u => u.Email == email));
        
        return usuario.Nome;
        
    }
    public List<Usuario> ListUsers()
    {
        return _context.Usuarios.ToList();
    }


}