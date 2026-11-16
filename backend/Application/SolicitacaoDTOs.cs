using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Entrada de criação de solicitação (Spec 017, RF1 e RF2; Spec 030 para dados).</summary>
public class CriarSolicitacaoDTO
{
    public TipoSolicitacao? Tipo { get; set; }
    public string? Descricao { get; set; }
    public int? AlunoId { get; set; }
    public int? CardapioId { get; set; }
    public string? RestricaoNome { get; set; }

    /// <summary>
    /// Para <c>InclusaoAlergia</c>/<c>AlteracaoRestricao</c>: o solicitante informa que a
    /// reação é anafilática. Aplicado ao aluno somente na aprovação (Spec 035, revisão).
    /// </summary>
    public bool Anafilaxia { get; set; }

    public string? Ingrediente { get; set; }

    /// <summary>Alterações do tipo <c>DadosCadastrais</c> (Spec 030, RF2).</summary>
    public List<AlteracaoSolicitadaDTO>? Alteracoes { get; set; }

    /// <summary>Justificativa do tipo <c>DadosCadastrais</c> (Spec 030, RF2).</summary>
    public string? Justificativa { get; set; }
}

/// <summary>Par campo/valor solicitado no pedido de dados cadastrais (Spec 030).</summary>
public class AlteracaoSolicitadaDTO
{
    public string? Campo { get; set; }
    public string? ValorSolicitado { get; set; }
}

/// <summary>Resposta de decisão (Spec 017, RF5).</summary>
public class DecidirSolicitacaoDTO
{
    public string? Resposta { get; set; }
}

/// <summary>Evento da trilha de status (Spec 017, RF9).</summary>
public class HistoricoSolicitacaoDTO
{
    public StatusSolicitacao Status { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string? Resposta { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>Linha da listagem de solicitações.</summary>
public class SolicitacaoResumoDTO
{
    public int Id { get; set; }
    public TipoSolicitacao Tipo { get; set; }
    public StatusSolicitacao Status { get; set; }
    public int UnidadeId { get; set; }
    public int? EscolaId { get; set; }
    public int? AlunoId { get; set; }
    public string? Aluno { get; set; }
    public int? CardapioId { get; set; }
    public string? Cardapio { get; set; }
    public string? RestricaoNome { get; set; }
    public string? Ingrediente { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int SolicitanteId { get; set; }
    public string Solicitante { get; set; } = string.Empty;
    public string? Resposta { get; set; }
    public string? RespondidoPor { get; set; }
    public DateTime CriadoEm { get; set; }
}

/// <summary>Detalhe da solicitação com a trilha de status (Spec 017, RF9).</summary>
public class SolicitacaoDTO : SolicitacaoResumoDTO
{
    public List<HistoricoSolicitacaoDTO> Historico { get; set; } = new();

    /// <summary>Solicitante informou reação anafilática para a restrição (Spec 035, revisão).</summary>
    public bool Anafilaxia { get; set; }

    /// <summary>Diff do pedido de dados cadastrais (Spec 030, RF12).</summary>
    public List<AlteracaoDeDadosDTO> Alteracoes { get; set; } = new();

    /// <summary>
    /// Explica o efeito da aprovação quando ele não é automático (Spec 017, RF7).
    /// </summary>
    public string? Aviso { get; set; }
}

/// <summary>Campo, valor atual no pedido e valor solicitado (Spec 030, RF12).</summary>
public class AlteracaoDeDadosDTO
{
    public CampoDeDadoDoAluno Campo { get; set; }
    public string ValorSolicitado { get; set; } = string.Empty;
    public string ValorNoPedido { get; set; } = string.Empty;
}
