using Ntc.Domain.Entity;
using Ntc.Application;
public class UsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public void CriarUsuario(DadosUsuarioDTO dto)
    {   
        Usuario usuario = new(
            dto.Nome,
            dto.Cpf,
            dto.DataNascimento,
            dto.Email,
            dto.Senha
        );
        _usuarioRepository.RegisterUser(usuario);

    }

    public DadosUsuarioDTO ObterUsuarioPorId(int id)
    {
        var user = _usuarioRepository.GetById(id);
        if (user == null)
        {
            return null;
        }

        DadosUsuarioDTO dto = new(
            user.Nome,
            user.Cpf,
            user.DataNascimento,
            user.Email
        );

        return dto;
    }

    public bool VerificarLogin(LoginUsuarioDTO dto)
    {
        bool value = _usuarioRepository.LoginUser(dto.Email, dto.Senha);

        return value;
    }

    public string ObterNomePeloEmail(string email)
    {
        string nome = _usuarioRepository.GetName(email);
        return nome;
    }

    public bool TrocarSenha(string email, string senha)
    {
        int id = _usuarioRepository.GetIdByEmail(email);
        if (id == null)
        {
            return false;
        }
        _usuarioRepository.ChangePassword(id, senha);
        return true;
    }
}