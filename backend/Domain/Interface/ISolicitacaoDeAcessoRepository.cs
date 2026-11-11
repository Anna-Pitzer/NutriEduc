using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de persistência da solicitação de acesso (Spec 033). O filtro chega pronto do
/// serviço; o repositório não conhece papel, sessão nem claims.
/// </summary>
public interface ISolicitacaoDeAcessoRepository
{
    SolicitacaoDeAcesso Add(SolicitacaoDeAcesso solicitacao);

    void Update(SolicitacaoDeAcesso solicitacao);

    SolicitacaoDeAcesso? Obter(int id);

    /// <summary>Solicitação pendente do usuário; nula quando não existe (RF11).</summary>
    SolicitacaoDeAcesso? ObterPendenteDoUsuario(int usuarioId);

    /// <summary>True quando o usuário já possui ao menos uma solicitação (diagnóstico de legado).</summary>
    bool ExistePorUsuario(int usuarioId);

    (List<SolicitacaoDeAcesso> Itens, int Total) Listar(
        FiltroSolicitacaoAcesso filtro,
        int pagina,
        int tamanho);
}
