using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

public interface IAlunoRepository
{
    void Add(Aluno aluno);

    /// <summary>
    /// Aplica o escopo na consulta (RF7/RNF4): o filtro vai para o SQL, nunca para
    /// depois do <c>ToList()</c>.
    /// </summary>
    List<Aluno> Listar(EscopoUsuario escopo);

    Aluno? Obter(int id, EscopoUsuario escopo);

    void UpdateAluno(Aluno aluno);
    void DeleteAluno(Aluno aluno);

    bool ExistePorUnidade(int unidadeId);
}
