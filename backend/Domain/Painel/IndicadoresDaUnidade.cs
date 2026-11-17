using Ntc.Domain.Entity;

namespace Ntc.Domain.Painel;

/// <summary>
/// Contagem de alunos de uma escola dentro da unidade (Spec 025, RF1). <see cref="EscolaId"/>
/// nulo agrupa os alunos sem escola, exibidos como "Sem escola".
/// </summary>
public sealed record ContagemPorEscola(int? EscolaId, string Escola, int Total);

/// <summary>Contagem de usuários ativos do papel dentro da unidade (Spec 025, RF1).</summary>
public sealed record ContagemPorPapel(PapelUsuario Papel, int Total);

/// <summary>
/// Indicadores agregados da unidade do solicitante (Spec 025, RF1/RF2). Nenhum campo
/// identifica aluno: o painel da secretaria é contagem pura.
/// </summary>
public sealed record IndicadoresDaUnidade(
    int Alunos,
    IReadOnlyList<ContagemPorEscola> AlunosPorEscola,
    IReadOnlyList<ContagemPorPapel> UsuariosAtivosPorPapel,
    int CardapiosPublicados,
    int OcorrenciasAbertas,
    int SolicitacoesPendentes);
