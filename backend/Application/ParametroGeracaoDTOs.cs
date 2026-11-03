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

/// <summary>Entrada de geração (Spec 013, RF3): unidade, nome, período e semente opcional.</summary>
public class GerarCardapioDTO
{
    public int UnidadeId { get; set; }
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

/// <summary>Resposta de geração (Spec 013, RF10): cardápio, semente, totais e avisos.</summary>
public class GeracaoCardapioDTO
{
    public int CardapioId { get; set; }
    public int Seed { get; set; }
    public List<TotalRefeicaoGeradaDTO> Totais { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
}
