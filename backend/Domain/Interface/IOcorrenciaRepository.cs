using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de persistência da ocorrência (Spec 016). O filtro chega pronto do serviço; o
/// repositório não conhece papel, sessão nem claims.
/// </summary>
public interface IOcorrenciaRepository
{
    Ocorrencia Add(Ocorrencia ocorrencia);

    void Update(Ocorrencia ocorrencia);

    /// <summary>Detalhe com o histórico de status.</summary>
    Ocorrencia? ObterComHistorico(int id);

    (List<Ocorrencia> Itens, int Total) Listar(
        FiltroOcorrencia filtro,
        int pagina,
        int tamanho);
}
