namespace Ntc.Application;

/// <summary>Parâmetros de geração na leitura (Spec 013, RF2).</summary>
public class ParametroGeracaoDTO
{
    public int Id { get; set; }
    public int UnidadeId { get; set; }
    public decimal CaloriasMinimasRefeicao { get; set; }
    public decimal CaloriasMaximasRefeicao { get; set; }
    public int OpcoesPorRefeicao { get; set; }
    public int JanelaRepeticaoDias { get; set; }
    public bool ExcluirAlimentosIncompativeis { get; set; }
    public List<int> CategoriasObrigatorias { get; set; } = new();
    public List<string> RefeicoesPadrao { get; set; } = new();
}

/// <summary>Entrada de criação/edição dos parâmetros de geração (Spec 013, RF2).</summary>
public class SalvarParametroGeracaoDTO
{
    public decimal CaloriasMinimasRefeicao { get; set; }
    public decimal CaloriasMaximasRefeicao { get; set; }
    public int OpcoesPorRefeicao { get; set; }
    public int JanelaRepeticaoDias { get; set; }
    public bool ExcluirAlimentosIncompativeis { get; set; }
    public List<int> CategoriasObrigatorias { get; set; } = new();
    public List<string> RefeicoesPadrao { get; set; } = new();
}

/// <summary>
/// Entrada de geração (Spec 013, RF3; Spec 027): unidade, nutricionista responsável,
/// nome, período e semente opcional. O cardápio gerado vale para todas as escolas da
/// unidade informada.
/// </summary>
public class GerarCardapioDTO
{
    public int UnidadeId { get; set; }

    /// <summary>Nutricionista que elaborou o cardápio; obrigatória na geração.</summary>
    public int NutricionistaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public PeriodoCardapioDTO? Periodo { get; set; }
    public int? Seed { get; set; }
}

/// <summary>Total calórico de uma refeição gerada (Spec 013, RF10).</summary>
public class TotalRefeicaoGeradaDTO
{
    public string Refeicao { get; set; } = string.Empty;
    public decimal CaloriasKcal { get; set; }
}

/// <summary>Tentativas consumidas por uma refeição gerada (Spec 032, RF12).</summary>
public class TentativaRefeicaoGeradaDTO
{
    public string Refeicao { get; set; } = string.Empty;
    public int Tentativas { get; set; }
}

/// <summary>Versão adaptada criada junto com o geral (Spec 032, RF8/RF12).</summary>
public class VersaoAdaptadaGeradaDTO
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public string Conjunto { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Motivo { get; set; }
}

/// <summary>Diagnóstico de uma refeição que não pôde ser gerada (Spec 032, RF15).</summary>
public class DiagnosticoRefeicaoDTO
{
    public string Refeicao { get; set; } = string.Empty;
    public int Tentativas { get; set; }
    public decimal FaixaMinimaAlcancavel { get; set; }
    public decimal FaixaMaximaAlcancavel { get; set; }
    public int? CategoriaAusente { get; set; }
    public int CandidatosDisponiveis { get; set; }
}

/// <summary>
/// Resposta de geração (Spec 032, RF12): ids do geral e dos adaptados, semente, tentativas,
/// totais por refeição, avisos e diagnóstico.
/// </summary>
public class GeracaoCardapioDTO
{
    public int CardapioGeralId { get; set; }
    public int Seed { get; set; }
    public List<TentativaRefeicaoGeradaDTO> TentativasPorRefeicao { get; set; } = new();
    public List<TotalRefeicaoGeradaDTO> TotaisPorRefeicao { get; set; } = new();
    public List<VersaoAdaptadaGeradaDTO> VersaoAdaptadaIds { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
    public List<DiagnosticoRefeicaoDTO> Diagnostico { get; set; } = new();
}
