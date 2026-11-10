using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de persistência do feedback (Spec 018). O filtro chega pronto do serviço, já com o
/// recorte por papel traduzido (Spec 024, F5); o repositório não conhece sessão nem claims.
/// </summary>
public interface IFeedbackRepository
{
    Feedback Add(Feedback feedback);

    void Update(Feedback feedback);

    Feedback? Obter(int id);

    (List<Feedback> Itens, int Total) Listar(
        FiltroFeedback filtro,
        int pagina,
        int tamanho);
}
