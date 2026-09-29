using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;


namespace Ntc.Database;
public class AlunoRepository()
{
    private readonly ConnectionContext _context = new();

    public void Add(Aluno aluno)
    {
        _context.Alunos.Add(aluno);
        _context.SaveChanges();
    }

    public List<Aluno>? GetAlunos()
    {
        return _context.Alunos.ToList();       
    }

    public Aluno? GetAluno(int id)
    {
        Aluno? aluno = _context.Alunos.FirstOrDefault(u => u.Id == id);       
        return aluno;
    }

    public void UpdateAluno(Aluno aluno)
    {
        _context.Alunos.Update(aluno);
        _context.SaveChanges();
    }

    public void DeleteAluno(Aluno aluno)
    {
        _context.Alunos.Remove(aluno);
        _context.SaveChanges();

    }
}