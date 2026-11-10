using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>Persistência do feedback no contexto Scoped (Spec 018).</summary>
public class FeedbackRepository : IFeedbackRepository
{
    private readonly ConnectionContext _context;

    public FeedbackRepository(ConnectionContext context)
    {
        _context = context;
    }

    public Feedback Add(Feedback feedback)
    {
        _context.Feedbacks.Add(feedback);
        return feedback;
    }

    public void Update(Feedback feedback) =>
        _context.Feedbacks.Update(feedback);

    public Feedback? Obter(int id) =>
        _context.Feedbacks.FirstOrDefault(feedback => feedback.Id == id);

    public (List<Feedback> Itens, int Total) Listar(
        FiltroFeedback filtro,
        int pagina,
        int tamanho)
    {
        var consulta = _context.Feedbacks.AsQueryable();

        if (filtro.Status is { } status)
        {
            consulta = consulta.Where(feedback => feedback.Status == status);
        }

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(feedback => feedback.UnidadeId == unidadeId);
        }

        if (filtro.EscolaId is { } escolaId)
        {
            consulta = consulta.Where(feedback => feedback.EscolaId == escolaId);
        }

        if (filtro.AutorId is { } autorId)
        {
            consulta = consulta.Where(feedback => feedback.AutorId == autorId);
        }

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(feedback => feedback.CriadoEmUtc)
            .ThenByDescending(feedback => feedback.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }
}
