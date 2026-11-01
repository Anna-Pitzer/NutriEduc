using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain;
using Ntc.Domain.Interface;

namespace Ntc.Database;
public class AlunoRepository : IAlunoRepository
{
    private readonly ConnectionContext _context;

    public AlunoRepository(ConnectionContext context)
{
    _context = context;
}
    public void Add(Aluno aluno)
    {
        _context.Alunos.Add(aluno);
    }

    public List<Aluno> Listar(FiltroAluno filtro)
    {
        return Filtrar(filtro).ToList();
    }

    public Aluno? Obter(int id, FiltroAluno filtro)
    {
        return Filtrar(filtro).FirstOrDefault(aluno => aluno.Id == id);
    }

    public void UpdateAluno(Aluno aluno)
    {
        _context.Alunos.Update(aluno);
    }

    public void DeleteAluno(Aluno aluno)
    {
        _context.Alunos.Remove(aluno);
    }

    /// <summary>
    /// Filtro de negócio aplicado em SQL (RNF4 da Spec 009): unidade, escola (via unidade
    /// de origem) e responsável vinculado. Aluno sem unidade só aparece no filtro global.
    /// </summary>
    private IQueryable<Aluno> Filtrar(FiltroAluno filtro)
    {
        var alunos = _context.Alunos.AsQueryable();

        if (filtro.UnidadeId is { } unidadeId)
        {
            alunos = alunos.Where(aluno => aluno.UnidadeId == unidadeId);
        }

        if (filtro.EscolaId is { } escolaId)
        {
            var unidadesDaEscola = _context.Unidades
                .Where(unidade => unidade.EscolaId == escolaId);

            alunos = alunos.Where(aluno =>
                aluno.UnidadeId != null &&
                unidadesDaEscola.Any(unidade => unidade.Id == aluno.UnidadeId));
        }

        if (filtro.ResponsavelUsuarioId is { } usuarioId)
        {
            var vinculos = _context.Vinculos
                .Where(vinculo => vinculo.ResponsavelUsuarioId == usuarioId);

            alunos = alunos.Where(aluno =>
                vinculos.Any(vinculo => vinculo.AlunoId == aluno.Id));
        }

        return alunos;
    }

    public bool ExistePorUnidade(int unidadeId) =>
        _context.Alunos.Any(aluno => aluno.UnidadeId == unidadeId);
}
