using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
namespace Ntc.Database;
public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConnectionContext _context = new();
    public bool RegisterUser(Usuario user) {
        if (!_context.Usuarios.Any(u => u.Email == user.Email))
        {
            _context.Usuarios.Add(user);
            _context.SaveChanges();  
            return true;
        }
        return false;
    }
    
    public bool LoginUser(string email, string senha)
    {
        if(_context.Usuarios.Any(u => u.Email == email && u.Senha == senha))
        {
            return true;
        }
        return false;

    }

    public Usuario GetById(int id)
    {
        var user = _context.Usuarios.FirstOrDefault(u => u.Id == id);
        return user;
    }

    public string? GetName(string email)
    {
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Email == email);
        
        return usuario?.Nome;
        
    }

    public int GetIdByEmail(string email)
    {
        return _context.Usuarios.Where(u => u.Email == email).Select(u => (int)u.Id).SingleOrDefault();
        
    }
        
    public void ChangePassword(int id, string senha)
    {
        _context.Usuarios.Where(u => u.Id == id).ExecuteUpdate(setters => setters.SetProperty(u => u.Senha, senha));
    }

    
}