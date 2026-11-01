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
    public List<RefeicaoCardapioDTO> Refeicoes { get; set; } = new();
    public ValoresNutricionaisDTO Total { get; set; } = new();
}

/// <summary>Linha da listagem: metadados e quantidade de refeições, sem itens.</summary>
public class CardapioResumoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int UnidadeId { get; set; }
    public SituacaoCardapio Situacao { get; set; }
    public PeriodoCardapioDTO Periodo { get; set; } = new();
    public int TotalRefeicoes { get; set; }
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
}

/// <summary>Entrada da duplicação: o novo período é obrigatório (RF6).</summary>
public class DuplicarCardapioDTO
{
    public string? Nome { get; set; }
    public PeriodoCardapioDTO? Periodo { get; set; }
}
