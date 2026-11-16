using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>
/// Resposta de leitura de usuário. Não carrega `senha` nem hash: o que a sessão
/// precisa é identidade (id/nome/e-mail) e autorização (papel/escola/unidade/ativo).
/// </summary>
public sealed record UsuarioRespostaDTO(
    int Id,
    string? Nome,
    string? Email,
    string? Telefone,
    string? Foto,
    PapelUsuario? Papel,
    int? EscolaId,
    int? UnidadeId,
    bool Ativo,
    string? Crn = null,
    bool CrnValido = false);

/// <summary>Resposta de escola.</summary>
public sealed record EscolaRespostaDTO(int Id, string Nome, int? UnidadeId, bool Ativa);

/// <summary>Resposta de unidade da rede.</summary>
public sealed record UnidadeRespostaDTO(int Id, string Nome, bool Ativa);

/// <summary>Responsável vinculado a um aluno.</summary>
public sealed record ResponsavelVinculadoDTO(int UsuarioId, string? Nome, string? Email);

/// <summary>Nutricionista em situação de legado: sem CRN ou com CRN inválido (Spec 031, RF10).</summary>
public sealed record DiagnosticoCrnDTO(int Id, string? Nome, int? UnidadeId, string Motivo);
