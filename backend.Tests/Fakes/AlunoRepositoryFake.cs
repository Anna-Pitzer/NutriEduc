using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória para os testes de serviço. Atribui o Id do mesmo modo que o
/// EF Core faria ao persistir: somente aqui a propriedade privada é escrita por reflexão.
/// </summary>
public class AlunoRepositoryFake : IAlunoRepository
{
    private readonly List<Aluno> _alunos = new();
    private int _proximoId = 1;

    public void Add(Aluno aluno)
    {
        DefinirId(aluno, _proximoId++);
        _alunos.Add(aluno);
    }

    public List<Aluno>? GetAlunos()
    {
        return _alunos.ToList();
    }

    public Aluno? GetAluno(int id)
    {
        return _alunos.FirstOrDefault(aluno => aluno.Id == id);
    }

    public void UpdateAluno(Aluno aluno)
    {
        var indice = _alunos.FindIndex(item => item.Id == aluno.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Aluno {aluno.Id} não existe no repositório fake.");

        _alunos[indice] = aluno;
    }

    public void DeleteAluno(Aluno aluno)
    {
        _alunos.RemoveAll(item => item.Id == aluno.Id);
    }

    private static void DefinirId(Aluno aluno, int id)
    {
        typeof(Aluno)
            .GetProperty(nameof(Aluno.Id))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(aluno, new object[] { id });
    }
}
