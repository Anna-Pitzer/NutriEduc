using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Leitura da trilha append-only do aluno (Spec 023, RF6). O agregado grava os eventos;
/// este port existe apenas para a consulta paginada e filtrada por tipo.
/// </summary>
public interface IHistoricoAlunoRepository
{
    /// <summary>
    /// Eventos em ordem decrescente de data. <paramref name="tipos"/> nulo não filtra;
    /// lista vazia devolve nenhum evento.
    /// </summary>
    (List<HistoricoAluno> Itens, int Total) Listar(
        int alunoId,
        IReadOnlyCollection<TipoEventoAluno>? tipos,
        int pagina,
        int tamanho);
}
