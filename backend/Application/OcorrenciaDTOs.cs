using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Entrada de criação de ocorrência (Spec 016, RF1 e RF2).</summary>
public class CriarOcorrenciaDTO
{
    public TipoOcorrencia? Tipo { get; set; }
    public string? Descricao { get; set; }
    public int? AlunoId { get; set; }
    public int? CardapioId { get; set; }
    public int? RefeicaoId { get; set; }
    public int? AlimentoId { get; set; }
}

/// <summary>Entrada de mudança de status (Spec 016, RF5).</summary>
public class MudarStatusOcorrenciaDTO
{
    public StatusOcorrencia? Status { get; set; }
    public string? Observacao { get; set; }
}

/// <summary>Evento do histórico de status (Spec 016, RF5/RF8).</summary>
public class HistoricoOcorrenciaDTO
{
    public StatusOcorrencia Status { get; set; }
    public string Autor { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

/// <summary>Linha da listagem de ocorrências. Nunca expõe id de autor (RF10).</summary>
public class OcorrenciaResumoDTO
{
    public int Id { get; set; }
    public TipoOcorrencia Tipo { get; set; }
    public StatusOcorrencia Status { get; set; }
    public int UnidadeId { get; set; }
    public int? EscolaId { get; set; }
    public int? AlunoId { get; set; }
    public string? Aluno { get; set; }
    public int? CardapioId { get; set; }
    public string? Cardapio { get; set; }
    public int? RefeicaoId { get; set; }
    public string? Refeicao { get; set; }
    public int? AlimentoId { get; set; }
    public string? Alimento { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

/// <summary>Detalhe da ocorrência com o histórico de status (Spec 016, RF8).</summary>
public class OcorrenciaDTO : OcorrenciaResumoDTO
{
    public List<HistoricoOcorrenciaDTO> Historico { get; set; } = new();

    /// <summary>True quando a sessão é o autor e a ocorrência está aberta (RF9).</summary>
    public bool PodeCancelar { get; set; }
}
