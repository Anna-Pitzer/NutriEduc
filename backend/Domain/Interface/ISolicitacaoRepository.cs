using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de persistência da solicitação (Spec 017). O filtro chega pronto do serviço; o
/// repositório não conhece papel, sessão nem claims.
/// </summary>
public interface ISolicitacaoRepository
{
    Solicitacao Add(Solicitacao solicitacao);

    void Update(Solicitacao solicitacao);

    /// <summary>Detalhe com a trilha de status, ordenada por data crescente.</summary>
    Solicitacao? ObterComHistorico(int id);

    (List<Solicitacao> Itens, int Total) Listar(
        FiltroSolicitacao filtro,
        int pagina,
        int tamanho);

    /// <summary>
    /// Id da solicitação de dados cadastrais aberta ou em análise que já pede o campo para
    /// o aluno (Spec 030, RF13); nulo quando não existe.
    /// </summary>
    int? IdDaAbertaParaCampo(int alunoId, CampoDeDadoDoAluno campo);

    /// <summary>
    /// Id da solicitação de inclusão/alteração de restrição aberta ou em análise para o
    /// mesmo aluno e a mesma restrição (Spec 035, RF20); nulo quando não existe.
    /// </summary>
    int? IdDaAbertaParaRestricao(int alunoId, string restricaoNome);
}
