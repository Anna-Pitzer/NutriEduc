using Ntc.Domain.Services;

namespace Ntc.Application;

/// <summary>Aluno afetado por um achado de compatibilidade (Spec 014, RF5).</summary>
public class AlunoAfetadoCompatibilidadeDTO
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
}

/// <summary>Alimento incompatível, com restrição, severidade e alunos afetados (RF5).</summary>
public class AchadoCompatibilidadeDTO
{
    public string Refeicao { get; set; } = string.Empty;
    public int AlimentoId { get; set; }
    public string Alimento { get; set; } = string.Empty;
    public string Restricao { get; set; } = string.Empty;
    public SeveridadeCompatibilidade Severidade { get; set; }
    public List<AlunoAfetadoCompatibilidadeDTO> Alunos { get; set; } = new();
}

/// <summary>Contrato da rota <c>GET /api/cardapios/{id}/compatibilidade</c> (Spec 014, RF4).</summary>
public class CompatibilidadeDTO
{
    public bool Compativel { get; set; }
    public int TotalAlunosAnalisados { get; set; }
    public List<AchadoCompatibilidadeDTO> Bloqueantes { get; set; } = new();
    public List<AchadoCompatibilidadeDTO> Alertas { get; set; } = new();
    public List<string> AlimentosPermitidos { get; set; } = new();
    public List<string> AlimentosNaoPermitidos { get; set; } = new();
    public List<string> Avisos { get; set; } = new();
}
