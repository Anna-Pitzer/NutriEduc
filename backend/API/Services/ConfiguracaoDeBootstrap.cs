using System.Text;

/// <summary>
/// Leitura única da configuração do bootstrap (Spec 024, F9). Antes vivia dentro de
/// <see cref="BootstrapService"/>; a criação de token, a criação de administrador e o
/// status precisam da mesma regra, e regra duplicada diverge na primeira correção.
///
/// Falha fechada: configuração inválida desabilita o recurso e registra o motivo.
/// </summary>
public sealed class ConfiguracaoDeBootstrap
{
    public const int TamanhoMinimoDoToken = 32;

    private readonly IConfiguration _configuracao;
    private readonly ILogger<ConfiguracaoDeBootstrap> _logger;

    public ConfiguracaoDeBootstrap(
        IConfiguration configuracao,
        ILogger<ConfiguracaoDeBootstrap> logger)
    {
        _configuracao = configuracao;
        _logger = logger;
    }

    /// <summary>
    /// Lê <c>Bootstrap:Permitir</c>. Ausente significa permitido (deploy que só configura
    /// o token); valor inválido significa negado, com erro no log.
    /// </summary>
    public bool Permitido
    {
        get
        {
            var valor = _configuracao["Bootstrap:Permitir"];

            if (string.IsNullOrWhiteSpace(valor))
            {
                return true;
            }

            if (!bool.TryParse(valor.Trim(), out var permitido))
            {
                _logger.LogError(
                    "Bootstrap desabilitado: Bootstrap:Permitir={Valor} não é true nem false.",
                    valor);
                return false;
            }

            return permitido;
        }
    }

    /// <summary>
    /// Token de ambiente, ou <c>null</c> quando ausente ou curto demais. Curto demais é
    /// recusado com erro no log, nunca aceito silenciosamente.
    /// </summary>
    public string? TokenDeAmbiente
    {
        get
        {
            var token = _configuracao["Bootstrap:Token"]?.Trim();

            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            if (token.Length < TamanhoMinimoDoToken)
            {
                _logger.LogError(
                    "Bootstrap desabilitado: o token configurado tem menos de {Minimo} caracteres.",
                    TamanhoMinimoDoToken);
                return null;
            }

            return token;
        }
    }
}
