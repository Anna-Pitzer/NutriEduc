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

        dto.Telefone = usuario.Telefone;
        dto.Foto = usuario.Foto;
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

    public int? ObterIdPeloEmail(string email)
    {
        Usuario? usuario = _usuarioRepository.GetByEmail(email);
        return usuario?.Id;
    }

    public DadosUsuarioDTO? AtualizarPerfil(
        int id,
        AtualizarPerfilDTO dto)
    {
        var usuario = _usuarioRepository.GetById(id);

        if (usuario == null)
        {
            return null;
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var usuarioComMesmoEmail = _usuarioRepository.GetByEmail(email);

        if (usuarioComMesmoEmail != null &&
            usuarioComMesmoEmail.Id != id)
        {
            throw new ArgumentException(
                "Este e-mail já está cadastrado em outra conta."
            );
        }

        usuario.AtualizarPerfil(dto.Nome, email);
        usuario.AtualizarTelefone(dto.Telefone);
        if (dto.Foto != null)
        {
            usuario.AtualizarFoto(dto.Foto);
        }
        _usuarioRepository.Update(usuario);

        return ObterUsuarioPorId(id);
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