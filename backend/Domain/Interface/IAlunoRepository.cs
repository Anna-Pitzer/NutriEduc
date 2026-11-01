using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

public interface IAlunoRepository
{
    void Add(Aluno aluno);

    /// <summary>
    /// Aplica o filtro de negócio na consulta (RF7/RNF4): o recorte vai para o SQL, nunca
    /// para depois do <c>ToList()</c>. Quem traduz o papel do solicitante para o filtro é a
    /// aplicação (Spec 024, F5).
    /// </summary>
    List<Aluno> Listar(FiltroAluno filtro);

    Aluno? Obter(int id, FiltroAluno filtro);

    void UpdateAluno(Aluno aluno);
    void DeleteAluno(Aluno aluno);

    bool ExistePorUnidade(int unidadeId);
}
