using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Consulta da trilha do aluno (Spec 023). Recebe o <see cref="ConnectionContext"/> por
/// construtor (L4); a escrita dos eventos é do agregado <see cref="Aluno"/>.
/// </summary>
public class HistoricoAlunoRepository : IHistoricoAlunoRepository
{
    private readonly ConnectionContext _context;

    public HistoricoAlunoRepository(ConnectionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Ordem decrescente de data com desempate por id, para a paginação ser estável quando
    /// dois eventos compartilham o mesmo instante (RF6).
    /// </summary>
    public (List<HistoricoAluno> Itens, int Total) Listar(
        int alunoId,
        IReadOnlyCollection<TipoEventoAluno>? tipos,
        int pagina,
        int tamanho)
    {
        var consulta = _context.HistoricosAlunos
            .Where(evento => evento.AlunoId == alunoId);

        if (tipos is not null)
        {
            var filtrados = tipos.ToList();
            consulta = consulta.Where(evento => filtrados.Contains(evento.Tipo));
        }

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(evento => evento.CriadoEmUtc)
            .ThenByDescending(evento => evento.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }
}
