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

    /// <summary>
    /// Cria o usuário e devolve o id gerado. Devolve <c>null</c> quando o e-mail já
    /// está cadastrado — o controller responde 400 nesse caso.
    /// </summary>
    public int? CriarUsuario(DadosUsuarioDTO dto)
    {
        Usuario usuario = new(
            dto.Nome,
            dto.Cpf,
            dto.DataNascimento,
            dto.Email
        );

        // Senha inválida vira ArgumentException; o controller traduz para 400.
        usuario.DefinirSenha(dto.Senha ?? string.Empty);

        if (_usuarioRepository.ExistsByEmail(usuario.Email!))
        {
            return null;
        }

        _usuarioRepository.Add(usuario);
        return usuario.Id;
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
        Usuario? usuario = _usuarioRepository.GetByEmail(dto.Email ?? string.Empty);

        if (usuario == null)
        {
            return false;
        }

        if (usuario.SenhaConfere(dto.Senha))
        {
            return true;
        }

        // Registro gravado antes da Spec 002: autentica em claro uma única vez e
        // persiste o hash, de modo que o próximo login use SenhaConfere.
        if (usuario.SenhaLegada(dto.Senha))
        {
            usuario.DefinirSenha(dto.Senha!);
            _usuarioRepository.Update(usuario);
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
}