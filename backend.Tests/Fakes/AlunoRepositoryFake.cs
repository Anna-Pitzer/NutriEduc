using Backend.Tests.Support;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória para os testes de serviço. Atribui o Id do mesmo modo que o
/// EF Core faria ao persistir: somente aqui a propriedade privada é escrita por reflexão.
///
/// Limite declarado: o fake não modela unidades nem vínculos, então apenas os escopos
/// globais (Nutricionista, AdminSecretaria) e Merendeira por unidade são reproduzíveis
/// aqui. AdminEscola e Responsavel são verificados nos testes HTTP, com SQLite real.
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

    public List<Aluno> Listar(EscopoUsuario escopo)
    {
        return Filtrar(escopo).ToList();
    }

    public Aluno? Obter(int id, EscopoUsuario escopo)
    {
        return Filtrar(escopo).FirstOrDefault(aluno => aluno.Id == id);
    }

    /// <summary>Somente para asserções dos testes; ignora escopo de propósito.</summary>
    public List<Aluno> GetAlunos() => _alunos.ToList();

    /// <summary>Somente para asserções dos testes; ignora escopo de propósito.</summary>
    public Aluno? GetAluno(int id) => _alunos.FirstOrDefault(aluno => aluno.Id == id);

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

    private IEnumerable<Aluno> Filtrar(EscopoUsuario escopo) => escopo.Papel switch
    {
        PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista => _alunos,
        PapelUsuario.Merendeira when escopo.UnidadeId is not null =>
            _alunos.Where(aluno => aluno.UnidadeId == escopo.UnidadeId),
        _ => Enumerable.Empty<Aluno>()
    };

    public bool ExistePorUnidade(int unidadeId) =>
        _alunos.Any(aluno => aluno.UnidadeId == unidadeId);

    private static void DefinirId(Aluno aluno, int id)
    {
        PropriedadesPrivadas.Definir(aluno, nameof(Aluno.Id), id);
    }
}
