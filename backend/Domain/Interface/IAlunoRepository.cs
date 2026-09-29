using Ntc.Domain.Entity;
namespace Ntc.Domain.Interface;

public interface IAlunoRepository
{
    bool Add(Aluno aluno);
    List<Aluno> GetAlunos();
    Aluno GetAluno(int id);
    bool UpdateAluno(int id);
    bool DeleteAluno(int id);
}