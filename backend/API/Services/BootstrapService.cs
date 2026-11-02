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




    private readonly IUsuarioRepository _usuarios;
    private readonly IEscolaRepository _escolas;
    private readonly IConsumoBootstrapRepository _consumos;
    private readonly ITokenDeCadastroRepository _tokens;
    private readonly ConfiguracaoDeBootstrap _configuracao;
    private readonly TimeProvider _relogio;
    private readonly ILogger<BootstrapService> _logger;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public BootstrapService(
        IUsuarioRepository usuarios,
        IEscolaRepository escolas,
        IConsumoBootstrapRepository consumos,
        ITokenDeCadastroRepository tokens,
        ConfiguracaoDeBootstrap configuracao,
        TimeProvider relogio,
        ILogger<BootstrapService> logger,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _usuarios = usuarios;
        _escolas = escolas;
        _consumos = consumos;
        _tokens = tokens;
        _configuracao = configuracao;
        _relogio = relogio;
        _logger = logger;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    /// <summary>
    /// Rota viva e motivo curto, numa única leitura de configuração e de banco. Chamar
    /// duas propriedades separadas repetiria a consulta de token válido no status.
    /// </summary>
    public (bool Disponivel, string Motivo) Estado()
    {
        if (!_configuracao.Permitido)
        {
            return (false, "desativado-ambiente");
        }

        var disponivel = _configuracao.TokenDeAmbiente is not null || _tokens.ExisteValido(Agora());

        return (disponivel, disponivel ? "ok" : "sem-token");
    }

    /// <summary>
    /// Cria o administrador a partir do token informado. A ordem das checagens importa:
    /// a trava de ambiente vem primeiro, e o token temporário só é procurado quando o
    /// valor não corresponde ao token de ambiente.
    /// </summary>
    public Resultado<int> CriarAdmin(CriarAdminBootstrapDTO dto)
    {
        if (!_configuracao.Permitido)
        {
            _logger.LogWarning(
                "Bootstrap recusado: criação desativada por configuração ({Email}).",
                dto.Email);
            return Resultado<int>.Proibido("Criação de administrador desativada.");
        }

        var agora = Agora();
        var esperado = _configuracao.TokenDeAmbiente;

        if (esperado is not null && HashDeToken.SegredosConferem(dto.Token, esperado))
        {
            return CriarPeloTokenDeAmbiente(dto, agora);
        }

        var temporario = string.IsNullOrWhiteSpace(dto.Token)
            ? null
            : _tokens.ObterPorHash(HashDeToken.Criar(dto.Token).ToString());

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
        _unidadeDeTrabalho.ExecutarEmTransacao(
            () => _consumos.Registrar(new ConsumoBootstrap(unidade.Id, agora)));

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
        _unidadeDeTrabalho.ExecutarEmTransacao(() => _tokens.Update(token));

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
        _unidadeDeTrabalho.ExecutarEmTransacao(() => _usuarios.Add(admin));

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
            var criada = _unidadeDeTrabalho.ExecutarEmTransacao(
                () => _escolas.AddUnidade(new UnidadeEscolar(valor)));

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
}
