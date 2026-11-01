using Ntc.Domain.Entity;

namespace Ntc.Application;

/// <summary>Categoria exposta pela API (Spec 011, RF1 e RF10).</summary>
public class CategoriaAlimentoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativa { get; set; }

    public CategoriaAlimentoDTO() { }

    public CategoriaAlimentoDTO(int id, string nome, bool ativa)
    {
        Id = id;
        Nome = nome;
        Ativa = ativa;
    }
}

/// <summary>Entrada de criação e edição de categoria: só o nome é editável.</summary>
public class SalvarCategoriaAlimentoDTO
{
    public string Nome { get; set; } = string.Empty;
}

/// <summary>Composição por 100 g no contrato de entrada e saída (RF3).</summary>
public class ValoresNutricionaisDTO
{
    public decimal CaloriasKcal { get; set; }
    public decimal ProteinasG { get; set; }
    public decimal CarboidratosG { get; set; }
    public decimal GordurasG { get; set; }
    public decimal FibrasG { get; set; }
    public decimal SodioMg { get; set; }

    public ValoresNutricionaisDTO() { }

    public ValoresNutricionaisDTO(ValoresNutricionais valores)
    {
        CaloriasKcal = valores.CaloriasKcal;
        ProteinasG = valores.ProteinasG;
        CarboidratosG = valores.CarboidratosG;
        GordurasG = valores.GordurasG;
        FibrasG = valores.FibrasG;
        SodioMg = valores.SodioMg;
    }
}

/// <summary>
/// Vínculo resolvido de um alimento (RF8). Traz a categoria da restrição — é ela que a
/// Spec 014 consome para decidir entre bloquear e alertar. Não existe motivo de vínculo.
/// </summary>
public class VinculoAlimentoDTO
{
    public int RestricaoId { get; set; }
    public string Restricao { get; set; } = string.Empty;
    public CategoriaRestricao? Categoria { get; set; }
}

/// <summary>Alimento devolvido pela API (RF8).</summary>
public class AlimentoDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public CategoriaAlimentoDTO Categoria { get; set; } = new();
    public bool Ativo { get; set; }
    public ValoresNutricionaisDTO Valores { get; set; } = new();
    public List<VinculoAlimentoDTO> Vinculos { get; set; } = new();
}

/// <summary>Entrada de criação e edição de alimento: composição e vínculos completos.</summary>
public class SalvarAlimentoDTO
{
    public string Nome { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public ValoresNutricionaisDTO? Valores { get; set; }
    public List<int>? RestricaoIds { get; set; }
}
