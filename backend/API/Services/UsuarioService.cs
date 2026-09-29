using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Application;

public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public bool CriarUsuario(DadosUsuarioDTO dto)
    {   
        Usuario usuario = new(
            dto.Nome,
            dto.Cpf,
            dto.DataNascimento,
            dto.Email,
            dto.Senha
        );

        if (_usuarioRepository.ExistsByEmail(usuario.Email) || !usuario.SenhaValida())
        {
            return false;
        }

        _usuarioRepository.Add(usuario);
        return true;
    }

    public DadosUsuarioDTO? ObterUsuarioPorId(int id)
    {
        Usuario? usuario = _usuarioRepository.GetById(id);
        if (usuario == null)
        {
            return null;
        }

        DadosUsuarioDTO? dto = new(
            usuario.Nome,
            usuario.Cpf,
            usuario.DataNascimento,
            usuario.Email
        );

        return dto;
    }

    public bool VerificarLogin(LoginUsuarioDTO dto)
    {
        Usuario? usuario = _usuarioRepository.GetByEmail(dto.Email);

        if (usuario != null && dto.Senha == usuario.Senha)
        {
            return true;
        }

        return false;
    }

    public string? ObterNomePeloEmail(string email)
    {
        Usuario? usuario = _usuarioRepository.GetByEmail(email);
        return usuario?.Nome;
    }

    public bool TrocarSenha(string email, string NovaSenha)
    {
        Usuario? usuario = _usuarioRepository.GetByEmail(email);

        if (usuario == null)
        {
            return false;
        }
        try
        {
            usuario.AtualizarSenha(NovaSenha);
        }
        catch (ArgumentException)
        {
            return false;
        }
        _usuarioRepository.Update(usuario);
        return true;
    }
}