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

    public Aluno? ObterPorId(int id)
    {
        return _context.Alunos.FirstOrDefault(aluno => aluno.Id == id);
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
    /// Filtro de negócio aplicado em SQL (RNF4 da Spec 009): unidade, escola e responsável
    /// vinculado. O recorte nunca é aplicado em memória (Spec 027, RNF1).
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
            alunos = alunos.Where(aluno => aluno.EscolaId == escolaId);
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

    public bool ExistePorEscola(int escolaId) =>
        _context.Alunos.Any(aluno => aluno.EscolaId == escolaId);

    public int ContarSemEscola() =>
        _context.Alunos.Count(aluno => aluno.EscolaId == null);
}
