using System.Security.Cryptography;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Caso de uso do token temporário de cadastro (Spec 022, Spec 024 F9): gerar, consultar o
/// estado e registrar o consumo da unidade. Não cria administrador — isso é de
/// <see cref="BootstrapService"/>.
/// </summary>
public sealed class BootstrapTokenService
{
    private const int BytesDoTokenTemporario = 32;

    private readonly ConfiguracaoDeBootstrap _configuracao;
    private readonly ITokenDeCadastroRepository _tokens;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;
    private readonly ILogger<BootstrapTokenService> _logger;

    public BootstrapTokenService(
        ConfiguracaoDeBootstrap configuracao,
        ITokenDeCadastroRepository tokens,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        ILogger<BootstrapTokenService> logger)
    {
        _configuracao = configuracao;
        _tokens = tokens;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
        _logger = logger;
    }

    public Resultado<TokenDeCadastroGerado> GerarToken(EscopoUsuario escopo)
    {
        if (!_configuracao.Permitido)
        {
            return Resultado<TokenDeCadastroGerado>.Proibido(
                "Criação de administrador desativada.");
        }

        if (escopo.Papel != PapelUsuario.AdminSecretaria)
        {
            return Resultado<TokenDeCadastroGerado>.Proibido(
                "Apenas a secretaria pode gerar o token de cadastro.");
        }

        if (escopo.UnidadeId is null)
        {
            return Resultado<TokenDeCadastroGerado>.Proibido(
                "Sua conta não está vinculada a uma unidade.");
        }

        var agora = Agora();
        var unidadeId = escopo.UnidadeId.Value;

        if (_tokens.ObterValidoDaUnidade(unidadeId, agora) is not null)
        {
            return Resultado<TokenDeCadastroGerado>.Conflito(
                "Já existe um token válido. Aguarde expirar.");
        }

        var valor = GerarValorDeToken();
        var token = new TokenDeCadastro(
            unidadeId,
            HashDeToken.Criar(valor),
            escopo.UsuarioId,
            agora,
            agora.AddMinutes(TokenDeCadastro.ValidadeEmMinutos));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _tokens.Add(token));

        _logger.LogWarning(
            "Token de cadastro gerado para a unidade {UnidadeId} por {UsuarioId} (expira em {ExpiraEm}).",
            unidadeId,
            escopo.UsuarioId,
            token.ExpiraEm);

        return Resultado<TokenDeCadastroGerado>.Ok(
            new TokenDeCadastroGerado(valor, token.ExpiraEm));
    }

    /// <summary>Estado do token da unidade do solicitante. Nunca devolve o valor.</summary>
    public TokenDeCadastroAtual ConsultarToken(EscopoUsuario escopo)
    {
        if (escopo.UnidadeId is null)
        {
            return new TokenDeCadastroAtual(false, null);
        }

        var token = _tokens.ObterValidoDaUnidade(escopo.UnidadeId.Value, Agora());

        return new TokenDeCadastroAtual(token is not null, token?.ExpiraEm);
    }

    private DateTime Agora() => _relogio.GetUtcNow().UtcDateTime;

    /// <summary>Token temporário em base64url, sem padding — seguro para copiar e colar.</summary>
    private static string GerarValorDeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(BytesDoTokenTemporario);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
