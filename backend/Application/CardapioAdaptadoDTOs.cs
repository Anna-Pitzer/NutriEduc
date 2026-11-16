using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Restrição exibida no conjunto de uma versão adaptada (Spec 029, RF18).</summary>
public class RestricaoDaVersaoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public CategoriaRestricao Categoria { get; set; }
}

/// <summary>Substituição aplicada na versão adaptada (Spec 029, RF18 e T5).</summary>
public class SubstituicaoCardapioDTO
{
    public int RefeicaoOrdem { get; set; }
    public string RefeicaoNome { get; set; } = string.Empty;
    public int AlimentoOriginalId { get; set; }
    public string AlimentoOriginal { get; set; } = string.Empty;
    public int AlimentoSubstitutoId { get; set; }
    public string AlimentoSubstituto { get; set; } = string.Empty;
    public decimal Gramas { get; set; }
    public string RestricoesBloqueantes { get; set; } = string.Empty;
}

/// <summary>Motivo de pendência que impede publicar ou servir a versão (Spec 029, RF8).</summary>
public class PendenciaAdaptacaoDTO
{
    public int RefeicaoOrdem { get; set; }
    public string RefeicaoNome { get; set; } = string.Empty;
    public int? AlimentoId { get; set; }
    public string? Alimento { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>Confirmação append-only de incompatibilidade mantida no geral (Spec 029, RF13).</summary>
public class ConfirmacaoIncompatibilidadeDTO
{
    public string Autor { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public int AlimentoId { get; set; }
    public string Alimento { get; set; } = string.Empty;
    public string Escolas { get; set; } = string.Empty;
    public string Conjuntos { get; set; } = string.Empty;
}

/// <summary>Linha do par geral↔adaptado na listagem de versões (Spec 029, RF18).</summary>
public class CardapioAdaptadoResumoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int OrigemId { get; set; }
    public int EscolaId { get; set; }
    public string EscolaNome { get; set; } = string.Empty;
    public string ConjuntoRestricoesChave { get; set; } = string.Empty;
    public StatusAdaptacao? StatusAdaptacao { get; set; }
    public SituacaoCardapio Situacao { get; set; }
    public int TotalSubstituicoes { get; set; }
    public int TotalPendencias { get; set; }
    public bool SemRestricoes { get; set; }
}

/// <summary>Entrada da adaptação manual: escola e, opcionalmente, o conjunto exato (Spec 029, T6).</summary>
public class AdaptarCardapioDTO
{
    public int EscolaId { get; set; }

    /// <summary>
    /// Restrições do conjunto. Quando nulo, o serviço adapta todos os conjuntos distintos
    /// existentes entre os alunos da escola (RF10).
    /// </summary>
    public List<int>? RestricaoIds { get; set; }
}

/// <summary>Aluno e suas restrições no painel da montagem manual (Spec 029, RF11).</summary>
public class AlunoNoContextoDTO
{
    public int AlunoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public List<RestricaoDaVersaoDTO> Restricoes { get; set; } = new();
    public string ConjuntoRestricoesChave { get; set; } = string.Empty;
}

/// <summary>Escola com alunos e restrições, insumo do painel e do cálculo dos conjuntos (RF11).</summary>
public class EscolaNoContextoDTO
{
    public int EscolaId { get; set; }
    public string EscolaNome { get; set; } = string.Empty;
    public bool SemRestricoes { get; set; }
    public List<AlunoNoContextoDTO> Alunos { get; set; } = new();
}

/// <summary>Contexto de restrições da unidade, opcionalmente filtrado por escola (RF11, RNF7).</summary>
public class ContextoDeRestricoesDTO
{
    public int UnidadeId { get; set; }
    public List<EscolaNoContextoDTO> Escolas { get; set; } = new();
}

/// <summary>
/// Versões visíveis para o aluno (Spec 029, RF15; revisão para o portal dos pais).
/// <see cref="Cardapio"/> continua sendo a versão servida (geral sem restrição, adaptada
/// com restrição); <see cref="CardapioGeral"/> e <see cref="CardapioAdaptado"/> permitem
/// que o responsável veja as duas versões publicadas para a data.
/// </summary>
public class CardapioServidoDTO
{
    public int AlunoId { get; set; }
    public string AlunoNome { get; set; } = string.Empty;
    public int? EscolaId { get; set; }
    public string ConjuntoRestricoesChave { get; set; } = string.Empty;
    public bool Disponivel { get; set; }
    public string? Motivo { get; set; }
    public bool Alerta { get; set; }
    public CardapioDTO? Cardapio { get; set; }

    /// <summary>Cardápio geral publicado para a data (nulo quando não existe).</summary>
    public CardapioDTO? CardapioGeral { get; set; }

    /// <summary>Versão adaptada publicada para o aluno (nulo quando não se aplica ou não existe).</summary>
    public CardapioDTO? CardapioAdaptado { get; set; }

    /// <summary>True quando o aluno tem restrições e depende de uma versão adaptada.</summary>
    public bool RequerAdaptacao { get; set; }
}
