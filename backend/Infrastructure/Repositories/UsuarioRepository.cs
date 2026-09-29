using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;
public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConnectionContext _context = new();
    public void Add(Usuario usuario) {
        _context.Usuarios.Add(usuario);
        _context.SaveChanges();  
    }

    public Usuario? GetById(int id)
    {
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == id);
        return usuario;
    }

    public Usuario? GetByEmail(string email)
    {
        return _context.Usuarios.FirstOrDefault(u => u.Email == email);   
    }

    public bool ExistsByEmail(string email)
    {
        return _context.Usuarios.Any(u => u.Email == email);
    }

    public void Update(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        _context.SaveChanges();
    }
}