using Ntc.Application;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>Motivo da recusa de login (Spec 009, RF1/RF5).</summary>
public enum ResultadoLogin
{
    Ok,
    CredenciaisInvalidas,
    SemPapel,
    Inativo
}

/// <summary>
/// Caso de uso de autenticação (Spec 024, F9): conferir credenciais, migrar senha legada
/// e decidir se a sessão pode ser emitida. Não cria conta nem administra usuário — isso é
/// de <see cref="UsuarioService"/>.
/// </summary>
public sealed class AutenticacaoService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public AutenticacaoService(
        IUsuarioRepository usuarioRepository,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _usuarioRepository = usuarioRepository;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Confere apenas as credenciais. Não considera papel nem situação ativa: quem decide
    /// se a sessão pode ser emitida é <see cref="Autenticar"/>.
    /// </summary>
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

        // Registro gravado antes da Spec 002: autentica em claro uma única vez e persiste o
        // hash, de modo que o próximo login use SenhaConfere.
        if (usuario.SenhaLegada(dto.Senha))
        {
            usuario.DefinirSenha(dto.Senha!);
            _unidadeDeTrabalho.ExecutarEmTransacao(() => _usuarioRepository.Update(usuario));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Login completo: credenciais, situação ativa e papel definido. Só devolve
    /// <see cref="ResultadoLogin.Ok"/> quando a sessão pode ser emitida.
    /// </summary>
    public ResultadoLogin Autenticar(LoginUsuarioDTO dto, out Usuario? usuario)
    {
        usuario = null;

        if (!VerificarLogin(dto))
        {
            return ResultadoLogin.CredenciaisInvalidas;
        }

        var encontrado = _usuarioRepository.GetByEmail(dto.Email ?? string.Empty);

        if (encontrado is null)
        {
            return ResultadoLogin.CredenciaisInvalidas;
        }

        if (!encontrado.Ativo)
        {
            return ResultadoLogin.Inativo;
        }

        if (encontrado.Papel is null)
        {
            return ResultadoLogin.SemPapel;
        }

        usuario = encontrado;
        return ResultadoLogin.Ok;
    }
}
