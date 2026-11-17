using Ntc.Domain.Entity;
using Ntc.Domain.Painel;

namespace Ntc.Domain.Interface;

/// <summary>
/// Leitura agregada do painel (Spec 025, RNF1). O escopo chega já resolvido pelo serviço;
/// o porto não conhece papel, sessão nem claims. Somente leitura: nenhum método grava.
/// </summary>
public interface IPainelRepository
{
    /// <summary>
    /// Contagens da unidade para o painel da secretaria (RF1). Os cardápios consideram
    /// apenas os publicados a partir de <paramref name="publicadoDesdeUtc"/>.
    /// </summary>
    IndicadoresDaUnidade ContarIndicadores(int unidadeId, DateTime publicadoDesdeUtc);

    /// <summary>
    /// Eventos de restrição do aluno no período, em ordem decrescente de data (RF6).
    /// </summary>
    List<HistoricoAluno> AlteracoesDeRestricao(
        int alunoId,
        DateTime inicioUtc,
        DateTime fimUtc);

    /// <summary>Ocorrências do aluno no período, em ordem decrescente de data (RF6).</summary>
    List<Ocorrencia> OcorrenciasDoAluno(
        int alunoId,
        DateTime inicioUtc,
        DateTime fimUtc);
}
