using System.Security.Cryptography;
using System.Text;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Bootstrap de administrador (Spec 022 v2). Duas vias de entrada, ambas com token:
///
/// 1. <b>Token de ambiente</b> (<c>Bootstrap__Token</c>): cria um <c>AdminSecretaria</c>
///    por unidade, uma única vez por unidade. A unidade é informada pelo nome exato.
/// 2. <b>Token temporário</b>: gerado no painel da secretaria, vale 30 minutos e uma
///    única vez, preso à unidade de quem gerou.
///
/// O token de ambiente é a única barreira da primeira via; por isso exige 32+
/// caracteres e comparação em tempo constante. O token temporário nunca é persistido em
/// claro — só o hash SHA-256.
/// </summary>
public class BootstrapService
{
    public const int TamanhoMinimoDoToken = 32;

    private const int BytesDoTokenTemporario = 32;

    private readonly IUsuarioRepository _usuarios;
    private readonly IEscolaRepository _escolas;
    private readonly IConsumoBootstrapRepository _consumos;
    private readonly ITokenDeCadastroRepository _tokens;
    private readonly IConfiguration _configuracao;
    private readonly TimeProvider _relogio;
    private readonly ILogger<BootstrapService> _logger;

    public BootstrapService(
        IUsuarioRepository usuarios,
        IEscolaRepository escolas,
        IConsumoBootstrapRepository consumos,
        ITokenDeCadastroRepository tokens,
        IConfiguration configuracao,
        TimeProvider relogio,
        ILogger<BootstrapService> logger)
    {
        _usuarios = usuarios;
        _escolas = escolas;
        _consumos = consumos;
        _tokens = tokens;
        _configuracao = configuracao;
        _relogio = relogio;
        _logger = logger;
    }

    /// <summary>
    /// Rota viva e motivo curto, numa única leitura de configuração e de banco. Chamar
    /// duas propriedades separadas repetiria a consulta de token válido no status.
    /// </summary>
    public (bool Disponivel, string Motivo) Estado()
    {
        if (!PermitidoNoAmbiente())
        {
            return (false, "desativado-ambiente");
        }

        var disponivel = TokenDeAmbiente() is not null || _tokens.ExisteValido(Agora());

        return (disponivel, disponivel ? "ok" : "sem-token");
    }

    /// <summary>
    /// Cria o administrador a partir do token informado. A ordem das checagens importa:
    /// a trava de ambiente vem primeiro, e o token temporário só é procurado quando o
    /// valor não corresponde ao token de ambiente.
    /// </summary>
    public Resultado<int> CriarAdmin(CriarAdminBootstrapDTO dto)
    {
        if (!PermitidoNoAmbiente())
        {
            _logger.LogWarning(
                "Bootstrap recusado: criação desativada por configuração ({Email}).",
                dto.Email);
            return Resultado<int>.Proibido("Criação de administrador desativada.");
        }

        var agora = Agora();
        var esperado = TokenDeAmbiente();

        if (esperado is not null && TokenConfere(dto.Token, esperado))
        {
            return CriarPeloTokenDeAmbiente(dto, agora);
        }

        var temporario = _tokens.ObterPorHash(HashDoToken(dto.Token));

        if (temporario is null || !temporario.EstaValido(agora))
        {
            // Sem token de ambiente configurado e sem token temporário vivo, a rota se
            // comporta como inexistente — não revela que o recurso existe.
            if (esperado is null && !_tokens.ExisteValido(agora))
            {
                return Resultado<int>.NaoEncontrado("Rota não encontrada.");
            }

            _logger.LogWarning(
                "Bootstrap recusado: token inválido ou expirado para {Email}.",
                dto.Email);
            return Resultado<int>.Proibido("Token inválido ou expirado.");
        }

        return CriarPeloTokenTemporario(dto, temporario, agora);
    }

    /// <summary>
    /// Emite o token temporário do painel. Uma unidade só pode ter um token válido por
    /// vez: emitir outro invalidaria o anterior sem o usuário saber (só o hash é
    /// guardado), então a resposta é 409.
    /// </summary>
    public Resultado<TokenDeCadastroGerado> GerarToken(EscopoUsuario escopo)
    {
        if (!PermitidoNoAmbiente())
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
            HashDoToken(valor),
            escopo.UsuarioId,
            agora,
            agora.AddMinutes(TokenDeCadastro.ValidadeEmMinutos));

        _tokens.Add(token);

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

    private Resultado<int> CriarPeloTokenDeAmbiente(CriarAdminBootstrapDTO dto, DateTime agora)
    {
        // E-mail primeiro: nada é criado se o cadastro vai falhar por conflito.
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (_usuarios.ExistsByEmail(email))
        {
            return Resultado<int>.Conflito("E-mail já cadastrado.");
        }

        var unidade = ResolverOuCriarUnidade(dto.Unidade);

        if (_consumos.UnidadeJaConsumiu(unidade.Id))
        {
            return Resultado<int>.Conflito(
                "Esta unidade já usou o token de criação.");
        }

        var criado = Criar(dto, unidade.Id);

        if (criado.Status != ResultadoStatus.Ok)
        {
            return criado;
        }

        // Só o token de ambiente consome a unidade: o token temporário é outra via e não
        // pode queimar a possibilidade de a unidade usar o token de configuração.
        _consumos.Registrar(new ConsumoBootstrap(unidade.Id, agora));

        _logger.LogWarning(
            "AdminSecretaria criado pelo token de ambiente da unidade {UnidadeId} (id {Id}).",
            unidade.Id,
            criado.Valor);

        return criado;
    }

    private Resultado<int> CriarPeloTokenTemporario(
        CriarAdminBootstrapDTO dto,
        TokenDeCadastro token,
        DateTime agora)
    {
        var unidade = _escolas.GetUnidade(token.UnidadeId);

        if (unidade is null || !unidade.Ativa)
        {
            return Resultado<int>.Proibido(
                "A unidade deste token não está mais disponível.");
        }

        var criado = Criar(dto, unidade.Id);

        if (criado.Status != ResultadoStatus.Ok)
        {
            return criado;
        }

        token.MarcarUsado(agora);
        _tokens.Update(token);

        _logger.LogWarning(
            "AdminSecretaria criado pelo token temporário da unidade {UnidadeId} (id {Id}).",
            unidade.Id,
            criado.Valor);

        return criado;
    }

    private Resultado<int> Criar(CriarAdminBootstrapDTO dto, int unidadeId)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (_usuarios.ExistsByEmail(email))
        {
            return Resultado<int>.Conflito("E-mail já cadastrado.");
        }

        var admin = new Usuario(
            dto.Nome,
            dto.Cpf,
            dto.DataNascimento,
            email,
            PapelUsuario.AdminSecretaria,
            escolaId: null,
            unidadeId: unidadeId);

        admin.DefinirSenha(dto.Senha);
        _usuarios.Add(admin);

        // Warning de propósito: criação de administrador precisa aparecer no log.
        _logger.LogWarning(
            "AdminSecretaria criado pelo bootstrap: {Email} (id {Id}, unidade {UnidadeId}).",
            admin.Email,
            admin.Id,
            unidadeId);

        return Resultado<int>.Ok(admin.Id);
    }

    /// <summary>
    /// Resolve a unidade pelo nome exato e, se ela não existir, **cria** — o primeiro
    /// administrador define a unidade da rede (decisão de 2026-10-31). A criação é a
    /// única porta para uma unidade nascer: não há tela de cadastro de unidade.
    /// </summary>
    private UnidadeEscolar ResolverOuCriarUnidade(string? nome)
    {
        var valor = (nome ?? string.Empty).Trim();

        if (valor.Length == 0)
        {
            throw new ArgumentException("Informe a unidade de destino.");
        }

        var encontradas = _escolas.BuscarUnidadesPorNome(valor);

        if (encontradas.Count > 1)
        {
            throw new ArgumentException(
                "Há mais de uma unidade com esse nome. Ajuste o cadastro da rede antes.");
        }

        var existente = encontradas.FirstOrDefault();

        if (existente is null)
        {
            var criada = _escolas.AddUnidade(new UnidadeEscolar(valor));

            _logger.LogWarning(
                "Unidade {Nome} criada pelo bootstrap (id {Id}).",
                criada.Nome,
                criada.Id);

            return criada;
        }

        if (!existente.Ativa)
        {
            throw new ArgumentException("A unidade informada está desativada.");
        }

        return existente;
    }

    private DateTime Agora() => _relogio.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Lê <c>Bootstrap:Permitir</c>. Ausente significa permitido (deploy que só configura
    /// o token); valor inválido significa negado, com erro no log — falha fechada.
    /// </summary>
    private bool PermitidoNoAmbiente()
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

    private string? TokenDeAmbiente()
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

    private static bool TokenConfere(string? informado, string esperado)
    {
        if (string.IsNullOrWhiteSpace(informado))
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(informado.Trim());
        var b = Encoding.UTF8.GetBytes(esperado);

        // FixedTimeEquals exige o mesmo tamanho; comparar o tamanho antes não vaza o
        // conteúdo, apenas o comprimento.
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>Token temporário em base64url, sem padding — seguro para copiar e colar.</summary>
    private static string GerarValorDeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(BytesDoTokenTemporario);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashDoToken(string? valor)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes((valor ?? string.Empty).Trim()));

        return Convert.ToHexString(bytes);
    }
}
