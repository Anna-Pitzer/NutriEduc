using Ntc.Domain.Entity;
namespace Ntc.Domain.Interface;

public interface IAlunoRepository
{
    void Add(Aluno aluno);
    List<Aluno>? GetAlunos();
    Aluno? GetAluno(int id);
    void UpdateAluno(Aluno aluno);
    void DeleteAluno(int id);
}