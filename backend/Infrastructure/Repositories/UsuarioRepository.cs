using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain;
using Ntc.Domain.Interface;

namespace Ntc.Database;
public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConnectionContext _context;

    public UsuarioRepository(ConnectionContext context)
    {
        _context = context;
    }

    public void Add(Usuario usuario) {
        _context.Usuarios.Add(usuario);
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

    public Usuario? GetByCrn(string crn)
    {
        return _context.Usuarios.FirstOrDefault(u => u.Crn == crn);
    }

    public Usuario? GetByCpf(string cpf)
    {
        return _context.Usuarios.FirstOrDefault(u => u.Cpf == cpf);
    }

    public void Update(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
    }

    /// <summary>
    /// O recorte do solicitante já vem dentro de <paramref name="filtro"/> (Spec 024, F5);
    /// o filtro da requisição nunca o relaxa, porque os dois são o mesmo objeto.
    /// </summary>
    public List<Usuario> Listar(FiltroUsuarios filtro)
    {
        var consulta = _context.Usuarios.AsQueryable();


        if (filtro.Papel is not null)
        {
            consulta = consulta.Where(usuario => usuario.Papel == filtro.Papel);
        }

        if (filtro.EscolaId is not null)
        {
            consulta = consulta.Where(usuario => usuario.EscolaId == filtro.EscolaId);
        }

        if (filtro.UnidadeId is not null)
        {
            consulta = consulta.Where(usuario => usuario.UnidadeId == filtro.UnidadeId);
        }

        if (filtro.Ativo is not null)
        {
            consulta = consulta.Where(usuario => usuario.Ativo == filtro.Ativo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim().ToLowerInvariant();

            consulta = consulta.Where(usuario =>
                (usuario.Nome ?? string.Empty).ToLower().Contains(termo) ||
                (usuario.Email ?? string.Empty).ToLower().Contains(termo));
        }

        return consulta.OrderBy(usuario => usuario.Nome).ToList();
    }

    public List<Usuario> ListarSemPapel() =>
        _context.Usuarios
            .Where(usuario => usuario.Papel == null)
            .OrderBy(usuario => usuario.Nome)
            .ToList();

    public void Delete(Usuario usuario)
    {
        _context.Usuarios.Remove(usuario);
    }

    public bool ExistePorEscola(int escolaId) =>
        _context.Usuarios.Any(usuario => usuario.EscolaId == escolaId);

    public bool ExistePorUnidade(int unidadeId) =>
        _context.Usuarios.Any(usuario => usuario.UnidadeId == unidadeId);

    public int ContarPorPapel(PapelUsuario papel) =>
        _context.Usuarios.Count(usuario => usuario.Papel == papel);

    public int ContarSemVinculoObrigatorio() =>
        _context.Usuarios.Count(usuario =>
            usuario.Papel != null &&
            (((usuario.Papel == PapelUsuario.AdminSecretaria ||
               usuario.Papel == PapelUsuario.Nutricionista ||
               usuario.Papel == PapelUsuario.Responsavel) &&
              usuario.UnidadeId == null) ||
             ((usuario.Papel == PapelUsuario.AdminEscola ||
               usuario.Papel == PapelUsuario.Merendeira) &&
              usuario.EscolaId == null)));
}
