using Backend.Tests.Fakes;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

public class AlunoServiceTests
{
    /// <summary>Escopo global: a nutrição enxerga toda a rede (Spec 009).</summary>
    private static readonly EscopoUsuario Escopo =
        new(1, PapelUsuario.Nutricionista, null, null);

    private static AlunoEntrada DtoValido(
        string nome = "Maria Silva",
        List<string>? restricoes = null)
    {
        return new AlunoEntrada
        {
            Nome = nome,
            Nascimento = new DateOnly(2015, 5, 20),
            Escola = "Escola Municipal",
            Serie = "5 ano",
            Telefone = "11999999999",
            Observacao = "Sem observação",
            Restricoes = restricoes ?? new List<string> { "APVL" },
            Anafilaxia = false,
            NomeResponsavel = "Ana Silva",
            TelefoneResponsavel = "11988888888"
        };
    }

    /// <summary>
    /// Catálogo já semeado com as restrições usadas pelos testes: sem isso a validação do
    /// caso de uso recusaria todos os nomes (Spec 010, RF4).
    /// </summary>
    private static (AlunoService Servico, AlunoRepositoryFake Alunos, RestricaoRepositoryFake Restricoes)
        CriarServico()
    {
        var alunos = new AlunoRepositoryFake();
        var restricoes = new RestricaoRepositoryFake("APVL", "Diabetes", "Lactose");

        return (
            new AlunoService(
                alunos,
                restricoes,
                new UsuarioRepositoryFake(),
                new EscolaRepositoryFake(),
                new UnidadeDeTrabalhoFake()),
            alunos,
            restricoes);
    }

    [Fact]
    public void CriarAluno_ComDadosValidos_Persiste()
    {
        var (servico, repositorio, _) = CriarServico();

        servico.CriarAluno(DtoValido(), Escopo);

        var alunos = repositorio.GetAlunos();

        Assert.NotNull(alunos);
        Assert.Single(alunos!);
        Assert.Equal(1, alunos![0].Id);
        Assert.Equal("Maria Silva", alunos[0].Nome);
    }

    [Fact]
    public void CriarAluno_ComDadosInvalidos_LancaENaoPersiste()
    {
        var (servico, repositorio, _) = CriarServico();

        Assert.Throws<ArgumentException>(() => servico.CriarAluno(DtoValido(nome: ""), Escopo));

        Assert.Empty(repositorio.GetAlunos()!);
    }

    [Fact]
    public void CriarAluno_ComRestricaoForaDoCatalogo_LancaENaoPersiste()
    {
        var (servico, repositorio, _) = CriarServico();

        var erro = Assert.Throws<ArgumentException>(() =>
            servico.CriarAluno(DtoValido(restricoes: new List<string> { "Amendoim" }), Escopo));

        Assert.Contains("Restrição desconhecida: Amendoim.", erro.Message);
        Assert.Empty(repositorio.GetAlunos()!);
    }

    [Fact]
    public void CriarAluno_ComRestricaoInativa_LancaENaoPersiste()
    {
        var (servico, repositorio, restricoes) = CriarServico();
        restricoes.Desativar("Lactose");

        var erro = Assert.Throws<ArgumentException>(() =>
            servico.CriarAluno(DtoValido(restricoes: new List<string> { "Lactose" }), Escopo));

        Assert.Contains("Restrição desconhecida: Lactose.", erro.Message);
        Assert.Empty(repositorio.GetAlunos()!);
    }

    [Fact]
    public void CriarAluno_UsaNomeCanonicoDoCatalogo()
    {
        var (servico, repositorio, _) = CriarServico();

        servico.CriarAluno(DtoValido(restricoes: new List<string> { "apvl" }), Escopo);

        var aluno = repositorio.GetAluno(1);

        Assert.Equal("APVL", Assert.Single(aluno!.Restricoes));
    }

    [Fact]
    public void CriarAluno_RegistraUmEventoPorRestricao()
    {
        var (servico, repositorio, _) = CriarServico();

        servico.CriarAluno(
            DtoValido(restricoes: new List<string> { "APVL", "Diabetes" }),
            Escopo);

        var aluno = repositorio.GetAluno(1);

        // Criação: um evento "Criado" seguido de um por restrição (Spec 023, RF2).
        Assert.Equal(3, aluno!.Historico.Count);
        Assert.Equal(TipoEventoAluno.Criado, aluno.Historico[0].Tipo);

        var adicionadas = aluno.Historico.Skip(1).ToList();
        Assert.All(adicionadas, evento =>
            Assert.Equal(TipoEventoAluno.RestricaoAdicionada, evento.Tipo));
        Assert.Equal(new[] { "APVL", "Diabetes" }, adicionadas.Select(evento => evento.ValorNovo));
    }

    [Fact]
    public void ListarAlunos_DevolveRestricoesComoLista()
    {
        var (servico, _, _) = CriarServico();
        servico.CriarAluno(DtoValido(restricoes: new List<string> { "APVL", "Diabetes" }), Escopo);

        var alunos = servico.ListarAlunos(Escopo);

        var aluno = Assert.Single(alunos);
        Assert.Equal(1, aluno.Id);
        Assert.Equal(new[] { "APVL", "Diabetes" }, aluno.Restricoes);
    }

    [Fact]
    public void ObterAlunoPorId_Existente_DevolveDto()
    {
        var (servico, _, _) = CriarServico();
        servico.CriarAluno(DtoValido(), Escopo);

        var aluno = servico.ObterAlunoPorId(1, Escopo);

        Assert.NotNull(aluno);
        Assert.Equal("Maria Silva", aluno!.Nome);
    }

    [Fact]
    public void ObterAlunoPorId_Inexistente_DevolveNull()
    {
        var (servico, _, _) = CriarServico();

        Assert.Null(servico.ObterAlunoPorId(99, Escopo));
    }

    [Fact]
    public void AtualizarAluno_Existente_AtualizaEDevolveDto()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(), Escopo);

        var atualizado = servico.AtualizarAluno(
            1,
            DtoValido(nome: "João Souza", restricoes: new List<string> { "Diabetes" }),
            Escopo);

        Assert.NotNull(atualizado);
        Assert.Equal("João Souza", atualizado!.Nome);
        Assert.Equal(new[] { "Diabetes" }, atualizado.Restricoes);

        var persistido = repositorio.GetAluno(1);
        Assert.NotNull(persistido);
        Assert.Equal("João Souza", persistido!.Nome);
        Assert.Equal(new[] { "Diabetes" }, persistido.Restricoes);
    }

    [Fact]
    public void AtualizarAluno_TrocandoRestricoes_RegistraUmEventoDeCadaAcao()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(restricoes: new List<string> { "APVL" }), Escopo);

        var antes = repositorio.GetAluno(1)!.Historico.Count;

        servico.AtualizarAluno(
            1,
            DtoValido(restricoes: new List<string> { "Diabetes" }),
            Escopo);

        var historico = repositorio.GetAluno(1)!.Historico;

        // A troca gera exatamente dois eventos novos: uma remoção e uma adição.
        Assert.Equal(antes + 2, historico.Count);
        Assert.Contains(historico, evento =>
            evento.Tipo == TipoEventoAluno.RestricaoRemovida && evento.ValorAnterior == "APVL");
        Assert.Contains(historico, evento =>
            evento.Tipo == TipoEventoAluno.RestricaoAdicionada && evento.ValorNovo == "Diabetes");
    }

    [Fact]
    public void AtualizarAluno_SemMudarNada_NaoRegistraEvento()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(restricoes: new List<string> { "APVL" }), Escopo);

        var antes = repositorio.GetAluno(1)!.Historico.Count;

        servico.AtualizarAluno(
            1,
            DtoValido(restricoes: new List<string> { "APVL" }),
            Escopo);

        Assert.Equal(antes, repositorio.GetAluno(1)!.Historico.Count);
    }

    [Fact]
    public void AtualizarAluno_MudandoApenasOTelefone_RegistraUmEventoDeCampo()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(), Escopo);

        var antes = repositorio.GetAluno(1)!.Historico.Count;

        var dto = DtoValido() with { Telefone = "11888887777" };

        servico.AtualizarAluno(1, dto, Escopo);

        var evento = Assert.Single(repositorio.GetAluno(1)!.Historico.Skip(antes));

        Assert.Equal(TipoEventoAluno.Alterado, evento.Tipo);
        Assert.Equal("Telefone", evento.Campo);
        Assert.Equal("11999999999", evento.ValorAnterior);
        Assert.Equal("11888887777", evento.ValorNovo);
    }

    [Fact]
    public void AtualizarAluno_MudandoAnafilaxia_RegistraValorLegivel()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(), Escopo);

        var antes = repositorio.GetAluno(1)!.Historico.Count;

        var dto = DtoValido() with { Anafilaxia = true };

        servico.AtualizarAluno(1, dto, Escopo);

        var evento = Assert.Single(repositorio.GetAluno(1)!.Historico.Skip(antes));

        Assert.Equal("Anafilaxia", evento.Campo);
        Assert.Equal("Não", evento.ValorAnterior);
        Assert.Equal("Sim", evento.ValorNovo);
    }

    [Fact]
    public void AtualizarAluno_Inexistente_DevolveNull()
    {
        var (servico, _, _) = CriarServico();

        Assert.Null(servico.AtualizarAluno(99, DtoValido(), Escopo));
    }

    [Fact]
    public void AtualizarAluno_ComDadosInvalidos_NaoAlteraDados()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(), Escopo);

        Assert.Throws<ArgumentException>(() =>
        {
            servico.AtualizarAluno(
                1,
                DtoValido(nome: "", restricoes: new List<string> { "Diabetes" }),
                Escopo);
        });

        var persistido = repositorio.GetAluno(1);

        Assert.NotNull(persistido);
        Assert.Equal("Maria Silva", persistido!.Nome);
        Assert.Equal("APVL", Assert.Single(persistido.Restricoes));
    }

    [Fact]
    public void ExcluirAluno_Existente_RemoveDoRepositorio()
    {
        var (servico, repositorio, _) = CriarServico();
        servico.CriarAluno(DtoValido(), Escopo);

        Assert.True(servico.ExcluirAluno(1, Escopo));
        Assert.Null(repositorio.GetAluno(1));
    }

    [Fact]
    public void ExcluirAluno_Inexistente_RetornaFalse()
    {
        var (servico, _, _) = CriarServico();

        Assert.False(servico.ExcluirAluno(99, Escopo));
    }
}
