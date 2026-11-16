using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Entrada da aprovação (Spec 033, RF7–RF9).</summary>
public class AprovarAcessoDTO
{
    /// <summary>Obrigatório para merendeira; proibido nos demais.</summary>
    public int? EscolaId { get; set; }

    /// <summary>Obrigatório para responsável; ignorado nos demais.</summary>
    public List<int>? AlunosIds { get; set; }

    /// <summary>Opcional; padrão = tipo solicitado. Nunca papel administrativo.</summary>
    public PapelUsuario? PapelConcedido { get; set; }

    /// <summary>Obrigatória quando PapelConcedido difere do tipo solicitado (RF13).</summary>
    public string? JustificativaCorrecaoPapel { get; set; }
}

/// <summary>Entrada da recusa (Spec 033, RF10).</summary>
public class RecusarAcessoDTO
{
    public string? Motivo { get; set; }
}

/// <summary>
/// Entrada da reconsideração: nova solicitação criada pelo ADM para um usuário existente
/// (Spec 033, RF10/D8). Exclusiva do ADM da unidade e bloqueada se já existe pendente.
/// </summary>
public class CriarSolicitacaoAcessoDTO
{
    public int? UsuarioId { get; set; }
    public TipoCadastroSolicitado? Tipo { get; set; }
    public int? UnidadeId { get; set; }
    public string? Crn { get; set; }
}

/// <summary>Linha da fila de solicitações de acesso (Spec 033, RF6/RF16).</summary>
public class SolicitacaoAcessoResumoDTO
{
    public int Id { get; set; }
    public TipoCadastroSolicitado TipoSolicitado { get; set; }
    public StatusSolicitacaoAcesso Status { get; set; }
    public int UnidadeId { get; set; }
    public int UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public DateTime SolicitadoEm { get; set; }
}

/// <summary>Detalhe da solicitação com a decisão (Spec 033, RF7/RF10).</summary>
public class SolicitacaoAcessoDTO : SolicitacaoAcessoResumoDTO
{
    public string? CrnSolicitado { get; set; }
    public PapelUsuario? PapelConcedido { get; set; }
    public int? EscolaConcedidaId { get; set; }
    public string? Motivo { get; set; }
    public string? JustificativaCorrecaoPapel { get; set; }
    public DateTime? DecididoEm { get; set; }
    public string? DecididoPorNome { get; set; }
}

/// <summary>Conta legada sem papel e sem solicitação (Spec 033, RF15/T13).</summary>
public sealed record DiagnosticoSemSolicitacaoDTO(int UsuarioId, string Nome, string Email);
