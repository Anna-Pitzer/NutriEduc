using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória para os testes de serviço. Atribui o Id do mesmo modo que o
/// EF Core faria ao persistir: somente aqui a propriedade privada é escrita por reflexão.
/// </summary>
public class UsuarioRepositoryFake : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();
    private int _proximoId = 1;

    public void Add(Usuario usuario)
    {
        DefinirId(usuario, _proximoId++);
        _usuarios.Add(usuario);
    }

    public Usuario? GetById(int id)
    {
        return _usuarios.FirstOrDefault(usuario => usuario.Id == id);
    }

    public Usuario? GetByEmail(string email)
    {
        return _usuarios.FirstOrDefault(usuario =>
            string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public bool ExistsByEmail(string email)
    {
        return GetByEmail(email) != null;
    }

    public void Update(Usuario usuario)
    {
        var indice = _usuarios.FindIndex(item => item.Id == usuario.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Usuário {usuario.Id} não existe no repositório fake.");

        _usuarios[indice] = usuario;
    }

    private static void DefinirId(Usuario usuario, int id)
    {
        typeof(Usuario)
            .GetProperty(nameof(Usuario.Id))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(usuario, new object[] { id });
    }
}
