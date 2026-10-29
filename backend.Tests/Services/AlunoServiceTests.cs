using Backend.Tests.Fakes;
using Ntc.Application;
using Xunit;

namespace Backend.Tests.Services;

public class AlunoServiceTests
{
    private static DadosAlunoDTO DtoValido(
        string nome = "Maria Silva",
        List<string>? restricoes = null)
    {
        return new DadosAlunoDTO(
            nome,
            new DateOnly(2015, 5, 20),
            "Escola Municipal",
            "5 ano",
            "11999999999",
            "Sem observação",
            restricoes ?? new List<string> { "APVL" },
            false,
            "Ana Silva",
            "11988888888");
    }

    [Fact]
    public void CriarAluno_ComDadosValidos_Persiste()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);

        servico.CriarAluno(DtoValido());

        var alunos = repositorio.GetAlunos();

        Assert.NotNull(alunos);
        Assert.Single(alunos!);
        Assert.Equal(1, alunos![0].Id);
        Assert.Equal("Maria Silva", alunos[0].Nome);
    }

    [Fact]
    public void CriarAluno_ComDadosInvalidos_LancaENaoPersiste()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);

        Assert.Throws<ArgumentException>(() => servico.CriarAluno(DtoValido(nome: "")));

        Assert.Empty(repositorio.GetAlunos()!);
    }

    [Fact]
    public void ListarAlunos_DevolveRestricoesComoLista()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);
        servico.CriarAluno(DtoValido(restricoes: new List<string> { "APVL", "Diabetes" }));

        var alunos = servico.ListarAlunos();

        var aluno = Assert.Single(alunos);
        Assert.Equal(1, aluno.Id);
        Assert.Equal(new[] { "APVL", "Diabetes" }, aluno.Restricoes);
    }

    [Fact]
    public void ObterAlunoPorId_Existente_DevolveDto()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);
        servico.CriarAluno(DtoValido());

        var aluno = servico.ObterAlunoPorId(1);

        Assert.NotNull(aluno);
        Assert.Equal("Maria Silva", aluno!.Nome);
    }

    [Fact]
    public void ObterAlunoPorId_Inexistente_DevolveNull()
    {
        var servico = new AlunoService(new AlunoRepositoryFake());

        Assert.Null(servico.ObterAlunoPorId(99));
    }

    [Fact]
    public void AtualizarAluno_Existente_AtualizaEDevolveDto()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);
        servico.CriarAluno(DtoValido());

        var atualizado = servico.AtualizarAluno(
            1,
            DtoValido(nome: "João Souza", restricoes: new List<string> { "Diabetes" }));

        Assert.NotNull(atualizado);
        Assert.Equal("João Souza", atualizado!.Nome);
        Assert.Equal(new[] { "Diabetes" }, atualizado.Restricoes);

        var persistido = repositorio.GetAluno(1);
        Assert.NotNull(persistido);
        Assert.Equal("João Souza", persistido!.Nome);
        Assert.Equal(new[] { "Diabetes" }, persistido.Restricoes);
    }

    [Fact]
    public void AtualizarAluno_Inexistente_DevolveNull()
    {
        var servico = new AlunoService(new AlunoRepositoryFake());

        Assert.Null(servico.AtualizarAluno(99, DtoValido()));
    }

    [Fact]
    public void AtualizarAluno_ComDadosInvalidos_NaoAlteraDados()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);
        servico.CriarAluno(DtoValido());

        Assert.Throws<ArgumentException>(() =>
        {
            servico.AtualizarAluno(1, DtoValido(nome: "", restricoes: new List<string> { "Diabetes" }));
        });

        var persistido = repositorio.GetAluno(1);

        Assert.NotNull(persistido);
        Assert.Equal("Maria Silva", persistido!.Nome);
        Assert.Equal("APVL", Assert.Single(persistido.Restricoes));
    }

    [Fact]
    public void ExcluirAluno_Existente_RemoveDoRepositorio()
    {
        var repositorio = new AlunoRepositoryFake();
        var servico = new AlunoService(repositorio);
        servico.CriarAluno(DtoValido());

        Assert.True(servico.ExcluirAluno(1));
        Assert.Null(repositorio.GetAluno(1));
    }

    [Fact]
    public void ExcluirAluno_Inexistente_RetornaFalse()
    {
        var servico = new AlunoService(new AlunoRepositoryFake());

        Assert.False(servico.ExcluirAluno(99));
    }
}
