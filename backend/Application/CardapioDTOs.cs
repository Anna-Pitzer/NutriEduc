using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Período de vigência no contrato (Spec 012, RF1).</summary>
public class PeriodoCardapioDTO
{
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }

    public PeriodoCardapioDTO() { }

    public PeriodoCardapioDTO(PeriodoVigencia periodo)
    {
        Inicio = periodo.Inicio;
        Fim = periodo.Fim;
    }
}

/// <summary>Item do cardápio com o alimento resolvido e o total calculado (RF11).</summary>
public class ItemCardapioDTO
{
    public int AlimentoId { get; set; }
    public string Alimento { get; set; } = string.Empty;
    public decimal Gramas { get; set; }
    public ValoresNutricionaisDTO Valores { get; set; } = new();
}

/// <summary>Refeição com itens e total próprio (RF11).</summary>
public class RefeicaoCardapioDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public List<ItemCardapioDTO> Itens { get; set; } = new();
    public ValoresNutricionaisDTO Total { get; set; } = new();
}

/// <summary>Cardápio completo: refeições, itens e totais (RF11).</summary>
public class CardapioDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int UnidadeId { get; set; }
    public SituacaoCardapio Situacao { get; set; }
    public PeriodoCardapioDTO Periodo { get; set; } = new();
    public int? CardapioDuplicadoDeId { get; set; }

    /// <summary>Geral ou adaptado (Spec 029, RF1).</summary>
    public TipoCardapio Tipo { get; set; }

    /// <summary>Geral de origem do adaptado; nulo no geral.</summary>
    public int? OrigemId { get; set; }

    /// <summary>Escola da versão adaptada; nulo no geral.</summary>
    public int? EscolaId { get; set; }

    /// <summary>Chave canônica do conjunto de restrições; nula no geral.</summary>
    public string? ConjuntoRestricoesChave { get; set; }

    /// <summary>Restrições do conjunto com nome e categoria (RF18).</summary>
    public List<RestricaoDaVersaoDTO> Restricoes { get; set; } = new();

    /// <summary>Estado da adaptação; nulo no geral (RF7).</summary>
    public StatusAdaptacao? StatusAdaptacao { get; set; }

    /// <summary>Verdadeiro quando a unidade não tem nenhum conjunto de restrições (RF10, RF21).</summary>
    public bool SemRestricoes { get; set; }

    public List<SubstituicaoCardapioDTO> Substituicoes { get; set; } = new();
    public List<PendenciaAdaptacaoDTO> Pendencias { get; set; } = new();
    public List<ConfirmacaoIncompatibilidadeDTO> Confirmacoes { get; set; } = new();

    /// <summary>Nutricionista que elaborou o cardápio; nulo em cardápios legados.</summary>
    public NutricionistaDTO? Nutricionista { get; set; }

    public List<RefeicaoCardapioDTO> Refeicoes { get; set; } = new();
    public ValoresNutricionaisDTO Total { get; set; } = new();
}

/// <summary>
/// Linha da listagem: metadados, quantidade de refeições e a contagem de
/// incompatibilidades (Spec 014, RF9) para o selo visual — calculada no serviço e
/// apenas renderizada pelo frontend.
/// </summary>
public class CardapioResumoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int UnidadeId { get; set; }
    public SituacaoCardapio Situacao { get; set; }
    public PeriodoCardapioDTO Periodo { get; set; } = new();
    public TipoCardapio Tipo { get; set; }
    public int? OrigemId { get; set; }
    public int? EscolaId { get; set; }
    public StatusAdaptacao? StatusAdaptacao { get; set; }
    public int TotalRefeicoes { get; set; }
    public int TotalBloqueantes { get; set; }
    public int TotalAlertas { get; set; }
}

/// <summary>Item na entrada de criação e edição (RF3).</summary>
public class SalvarItemCardapioDTO
{
    public int AlimentoId { get; set; }
    public decimal Gramas { get; set; }
}

/// <summary>Refeição na entrada de criação e edição (RF2).</summary>
public class SalvarRefeicaoDTO
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public List<SalvarItemCardapioDTO>? Itens { get; set; }
}

/// <summary>
/// Entrada de criação e edição do cardápio (RF1 a RF3 e RF12). A unidade é obrigatória na
/// criação; na edição o serviço mantém a unidade do cardápio existente.
/// </summary>
public class SalvarCardapioDTO
{
    public string Nome { get; set; } = string.Empty;
    public int UnidadeId { get; set; }
    public PeriodoCardapioDTO? Periodo { get; set; }
    public List<SalvarRefeicaoDTO>? Refeicoes { get; set; }

    /// <summary>Nutricionista responsável, obrigatória na criação (Spec 029, RF9).</summary>
    public int? NutricionistaId { get; set; }

    /// <summary>
    /// Confirma que a nutricionista manteve um alimento incompatível com substituto seguro
    /// (Spec 029, RF12). Sem esta confirmação a criação devolve 400.
    /// </summary>
    public bool ConfirmarIncompatibilidades { get; set; }
}

/// <summary>Entrada da duplicação: o novo período é obrigatório (RF6).</summary>
public class DuplicarCardapioDTO
{
    public string? Nome { get; set; }
    public PeriodoCardapioDTO? Periodo { get; set; }
}
