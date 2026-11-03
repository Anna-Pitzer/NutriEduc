namespace Ntc.Domain.Services;

/// <summary>Severidade de um achado (Spec 014, RF2): alergia bloqueia, restrição alerta.</summary>
public enum SeveridadeCompatibilidade
{
    Alerta,
    Bloqueante
}

/// <summary>Alimento incompatível com uma restrição de alunos da unidade (Spec 014, RF5).</summary>
public sealed record AchadoCompatibilidade(
    int RefeicaoOrdem,
    string RefeicaoNome,
    int AlimentoId,
    string AlimentoNome,
    string RestricaoNome,
    SeveridadeCompatibilidade Severidade,
    IReadOnlyList<(int Id, string Nome)> AlunosAfetados);

/// <summary>
/// Resultado da análise (Spec 014, RF4): `Compativel` é falso quando existe bloqueante. As
/// listas são derivadas e já ordenadas pela função pura — nenhuma ordenação é feita no
/// serviço nem no controller.
/// </summary>
public sealed record ResultadoCompatibilidade(
    bool Compativel,
    IReadOnlyList<AchadoCompatibilidade> Bloqueantes,
    IReadOnlyList<AchadoCompatibilidade> Alertas,
    IReadOnlyList<string> AlimentosPermitidos,
    IReadOnlyList<string> AlimentosNaoPermitidos,
    int TotalAlunosAnalisados,
    IReadOnlyList<string> Avisos);
