using Backend.Tests.Support;
using Ntc.Domain;
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

    public Usuario? GetByCrn(string crn)
    {
        return _usuarios.FirstOrDefault(usuario => usuario.Crn == crn);
    }

    public void Update(Usuario usuario)
    {
        var indice = _usuarios.FindIndex(item => item.Id == usuario.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Usuário {usuario.Id} não existe no repositório fake.");

        _usuarios[indice] = usuario;
    }

    public List<Usuario> Listar(FiltroUsuarios filtro)
    {
        var consulta = _usuarios.AsEnumerable();

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
            var termo = filtro.Busca.Trim();

            consulta = consulta.Where(usuario =>
                (usuario.Nome ?? string.Empty).Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                (usuario.Email ?? string.Empty).Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        return consulta.OrderBy(usuario => usuario.Nome).ToList();
    }

    public void Delete(Usuario usuario)
    {
        _usuarios.RemoveAll(item => item.Id == usuario.Id);
    }

    public bool ExistePorEscola(int escolaId) =>
        _usuarios.Any(usuario => usuario.EscolaId == escolaId);

    public bool ExistePorUnidade(int unidadeId) =>
        _usuarios.Any(usuario => usuario.UnidadeId == unidadeId);

    public int ContarPorPapel(PapelUsuario papel) =>
        _usuarios.Count(usuario => usuario.Papel == papel);

    public int ContarSemVinculoObrigatorio() =>
        _usuarios.Count(usuario =>
            usuario.Papel is PapelUsuario.AdminSecretaria
                or PapelUsuario.Nutricionista
                or PapelUsuario.Responsavel
                ? usuario.UnidadeId == null
                : usuario.Papel is PapelUsuario.AdminEscola or PapelUsuario.Merendeira
                    && usuario.EscolaId == null);

    private static void DefinirId(Usuario usuario, int id)
    {
        PropriedadesPrivadas.Definir(usuario, nameof(Usuario.Id), id);
    }
}
