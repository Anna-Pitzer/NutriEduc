using Ntc.Domain.Entity;

namespace Ntc.Domain;

/// <summary>
/// Escopo de dados do usuário autenticado, derivado das claims (Spec 009). É a única
/// fonte de verdade sobre "quem pode ver o quê": repositórios recebem este record e
/// aplicam o filtro no banco; controllers nunca recalculam regra de papel.
/// </summary>
public sealed record EscopoUsuario(
    int UsuarioId,
    PapelUsuario Papel,
    int? EscolaId,
    int? UnidadeId)
{
    /// <summary>Nutricionista e AdminSecretaria enxergam toda a rede.</summary>
    public bool Global => Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista;

    /// <summary>Papéis que administram usuários (RF4, RF5).</summary>
    public bool Administra => Papel is PapelUsuario.AdminSecretaria or PapelUsuario.AdminEscola;
}

/// <summary>
/// Filtros da listagem de usuários (RF7). Campos nulos não filtram.
/// </summary>
public sealed record FiltroUsuarios(
    PapelUsuario? Papel,
    int? EscolaId,
    int? UnidadeId,
    bool? Ativo,
    string? Busca);
