using Microsoft.EntityFrameworkCore;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Persistência da ocorrência (Spec 016). Compartilha o <see cref="ConnectionContext"/>
/// Scoped; o histórico de status é navegação do agregado e sobe/desce junto com ele.
/// </summary>
public class OcorrenciaRepository : IOcorrenciaRepository
{
    private readonly ConnectionContext _context;

    public OcorrenciaRepository(ConnectionContext context)
    {
        _context = context;
    }

    public Ocorrencia Add(Ocorrencia ocorrencia)
    {
        _context.Ocorrencias.Add(ocorrencia);
        return ocorrencia;
    }

    public void Update(Ocorrencia ocorrencia) =>
        _context.Ocorrencias.Update(ocorrencia);

    public Ocorrencia? ObterComHistorico(int id) =>
        _context.Ocorrencias
            .Include(ocorrencia => ocorrencia.Historico)
            .FirstOrDefault(ocorrencia => ocorrencia.Id == id);

    public (List<Ocorrencia> Itens, int Total) Listar(
        FiltroOcorrencia filtro,
        int pagina,
        int tamanho)
    {
        var consulta = _context.Ocorrencias.AsQueryable();

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.Tipo == tipo);
        }

        if (filtro.Status is { } status)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.Status == status);
        }

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.UnidadeId == unidadeId);
        }

        if (filtro.EscolaId is { } escolaId)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.EscolaId == escolaId);
        }

        if (filtro.AlunoId is { } alunoId)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.AlunoId == alunoId);
        }

        if (filtro.De is { } de)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.CriadoEmUtc >= de);
        }

        if (filtro.Ate is { } ate)
        {
            consulta = consulta.Where(ocorrencia => ocorrencia.CriadoEmUtc <= ate);
        }

        if (filtro.ResponsavelUsuarioId is { } responsavelId)
        {
            // RF6/RF10: o responsável vê apenas as ocorrências dos alunos vinculados.
            consulta = consulta.Where(ocorrencia =>
                ocorrencia.AlunoId != null &&
                _context.Vinculos.Any(vinculo =>
                    vinculo.AlunoId == ocorrencia.AlunoId &&
                    vinculo.ResponsavelUsuarioId == responsavelId));
        }

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(ocorrencia => ocorrencia.CriadoEmUtc)
            .ThenByDescending(ocorrencia => ocorrencia.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }
}
