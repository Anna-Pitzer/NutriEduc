namespace Ntc.Domain.Entity;

/// <summary>
/// Token temporário gerado pelo painel da secretaria para cadastrar outro administrador
/// (Spec 022 v2). Vale 30 minutos, uma única vez, e fica preso à unidade de quem o
/// gerou — o cadastro herda essa unidade.
///
/// O valor em claro nunca é persistido: a entidade guarda apenas o hash.
/// </summary>
public class TokenDeCadastro
{
    public const int ValidadeEmMinutos = 30;

    public int Id { get; private set; }

    public int UnidadeId { get; private set; }

    /// <summary>SHA-256 do token em hexadecimal. O valor em claro existe só na resposta.</summary>
    public string Hash { get; private set; } = string.Empty;

    public int CriadoPorUsuarioId { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime ExpiraEm { get; private set; }

    public DateTime? UsadoEm { get; private set; }

    /// <summary>Somente para o EF Core.</summary>
    private TokenDeCadastro()
    {
    }

    public TokenDeCadastro(
        int unidadeId,
        HashDeToken hash,
        int criadoPorUsuarioId,
        DateTime criadoEm,
        DateTime expiraEm)
    {
        if (unidadeId <= 0)
        {
            throw new ArgumentException("Token de cadastro exige unidade válida.");
        }

        if (string.IsNullOrWhiteSpace(hash.ToString()))
        {
            throw new ArgumentException("Token de cadastro exige hash.");
        }

        if (criadoPorUsuarioId <= 0)
        {
            throw new ArgumentException("Token de cadastro exige autor.");
        }

        if (expiraEm <= criadoEm)
        {
            throw new ArgumentException("A expiração deve ser posterior à criação.");
        }

        UnidadeId = unidadeId;
        Hash = hash.ToString();
        CriadoPorUsuarioId = criadoPorUsuarioId;
        CriadoEm = criadoEm;
        ExpiraEm = expiraEm;
    }

    /// <summary>Válido enquanto não foi usado e o relógio não alcançou a expiração.</summary>
    public bool EstaValido(DateTime agora) => UsadoEm is null && agora < ExpiraEm;

    /// <summary>
    /// Consome o token. Revalida no momento do uso: entre a consulta e a gravação o
    /// relógio pode ter passado da expiração ou outra requisição pode ter usado.
    /// </summary>
    public void MarcarUsado(DateTime agora)
    {
        if (!EstaValido(agora))
        {
            throw new ArgumentException("Token inválido ou expirado.");
        }

        UsadoEm = agora;
    }
}
