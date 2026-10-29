using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

public class AlunoTests
{
    private static readonly DateOnly NascimentoPadrao = new(2015, 5, 20);

    private static Aluno Criar(
        string nome = "Maria Silva",
        DateOnly? nascimento = null,
        string escola = "Escola Municipal",
        string serie = "5 ano",
        string? telefone = "11999999999",
        string? observacao = null,
        IEnumerable<string>? restricoes = null,
        bool anafilaxia = false,
        string nomeResponsavel = "Ana Silva",
        string? telefoneResponsavel = "11988888888")
    {
        return new Aluno(
            nome,
            nascimento ?? NascimentoPadrao,
            new Turma(escola, serie),
            telefone,
            observacao,
            restricoes,
            anafilaxia,
            new Responsavel(nomeResponsavel, telefoneResponsavel));
    }

    [Fact]
    public void Construtor_ComDadosValidos_CriaAluno()
    {
        var aluno = Criar(observacao: "  Sem restrição  ");

        Assert.Equal("Maria Silva", aluno.Nome);
        Assert.Equal("Escola Municipal", aluno.Turma.Escola);
        Assert.Equal("5 ano", aluno.Turma.Serie);
        Assert.Equal(NascimentoPadrao, aluno.Nascimento);
        Assert.Equal("Sem restrição", aluno.Observacao);
        Assert.Equal("Ana Silva", aluno.Responsavel.Nome);
        Assert.False(aluno.Anafilaxia);
    }

    [Fact]
    public void Construtor_CompoeTurmaEResponsavel()
    {
        var turma = new Turma("Escola Municipal", "5 ano");
        var responsavel = new Responsavel("Ana Silva", "11988888888");

        var aluno = new Aluno("Maria Silva", NascimentoPadrao, turma, null, null, null, false, responsavel);

        Assert.Same(turma, aluno.Turma);
        Assert.Same(responsavel, aluno.Responsavel);
    }

    [Fact]
    public void Construtor_SemTurma_Lanca()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new Aluno(
                "Maria Silva", NascimentoPadrao, null!, null, null, null, false,
                new Responsavel("Ana Silva", null));
        });
    }

    [Fact]
    public void Construtor_SemResponsavel_Lanca()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new Aluno(
                "Maria Silva", NascimentoPadrao, new Turma("Escola Municipal", "5 ano"),
                null, null, null, false, null!);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Construtor_ComNomeInvalido_Lanca(string nome)
    {
        Assert.Throws<ArgumentException>(() => { Criar(nome: nome); });
    }

    [Fact]
    public void Construtor_ComNomeAcimaDe100Caracteres_Lanca()
    {
        Assert.Throws<ArgumentException>(() => { Criar(nome: new string('a', 101)); });
    }

    [Theory]
    [InlineData("")]
    [InlineData("E")]
    public void Construtor_ComEscolaInvalida_Lanca(string escola)
    {
        Assert.Throws<ArgumentException>(() => { Criar(escola: escola); });
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    public void Construtor_ComSerieInvalida_Lanca(string serie)
    {
        Assert.Throws<ArgumentException>(() => { Criar(serie: serie); });
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Construtor_ComResponsavelInvalido_Lanca(string nomeResponsavel)
    {
        Assert.Throws<ArgumentException>(() => { Criar(nomeResponsavel: nomeResponsavel); });
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void Construtor_ComTelefoneInvalido_Lanca(string telefone)
    {
        Assert.Throws<ArgumentException>(() => { Criar(telefone: telefone); });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_SemTelefone_UsaStringVazia(string? telefone)
    {
        var aluno = Criar(telefone: telefone);

        Assert.Equal(string.Empty, aluno.Telefone);
    }

    [Fact]
    public void Construtor_NormalizaTelefoneComMascara()
    {
        var aluno = Criar(telefone: "(11) 99999-9999");

        Assert.Equal("11999999999", aluno.Telefone);
    }

    [Fact]
    public void Construtor_ComNascimentoFuturo_Lanca()
    {
        var futuro = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Assert.Throws<ArgumentException>(() => { Criar(nascimento: futuro); });
    }

    [Fact]
    public void Construtor_ComNascimentoAnteriorA1900_Lanca()
    {
        Assert.Throws<ArgumentException>(() => { Criar(nascimento: new DateOnly(1899, 12, 31)); });
    }

    [Fact]
    public void Construtor_ComObservacaoAcimaDe500Caracteres_Lanca()
    {
        Assert.Throws<ArgumentException>(() => { Criar(observacao: new string('a', 501)); });
    }

    [Fact]
    public void Construtor_IgnoraRestricoesVaziasEDuplicadas()
    {
        var aluno = Criar(restricoes: new[] { "", "   ", "APVL", "apvl", "Diabetes" });

        Assert.Equal(2, aluno.Restricoes.Count);
        Assert.Contains("APVL", aluno.Restricoes);
        Assert.Contains("Diabetes", aluno.Restricoes);
    }

    [Fact]
    public void Construtor_ComMaisDe20Restricoes_Lanca()
    {
        var restricoes = Enumerable.Range(1, 21).Select(numero => $"Restrição {numero}");

        Assert.Throws<ArgumentException>(() => { Criar(restricoes: restricoes); });
    }

    [Fact]
    public void Restricoes_NaoExpoeListaMutavel()
    {
        var aluno = Criar(restricoes: new[] { "APVL" });

        var restricoes = Assert.IsAssignableFrom<IReadOnlyList<string>>(aluno.Restricoes);

        Assert.False(restricoes is List<string>);
    }

    [Fact]
    public void AdicionarRestricao_NormalizaEEspecificaOValor()
    {
        var aluno = Criar();

        aluno.AdicionarRestricao("  APVL  ");

        Assert.Single(aluno.Restricoes);
        Assert.Equal("APVL", aluno.Restricoes[0]);
    }

    [Fact]
    public void AdicionarRestricao_Duplicada_Lanca()
    {
        var aluno = Criar(restricoes: new[] { "APVL" });

        Assert.Throws<ArgumentException>(() => aluno.AdicionarRestricao("apvl"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AdicionarRestricao_Vazia_Lanca(string restricao)
    {
        var aluno = Criar();

        Assert.Throws<ArgumentException>(() => aluno.AdicionarRestricao(restricao));
    }

    [Fact]
    public void AdicionarRestricao_AcimaDoLimite_Lanca()
    {
        var aluno = Criar();

        for (var numero = 1; numero <= 20; numero++)
            aluno.AdicionarRestricao($"Restrição {numero}");

        Assert.Throws<ArgumentException>(() => aluno.AdicionarRestricao("Restrição 21"));
    }

    [Fact]
    public void RemoverRestricao_Existente_RetornaTrue()
    {
        var aluno = Criar(restricoes: new[] { "APVL" });

        Assert.True(aluno.RemoverRestricao("apvl"));
        Assert.Empty(aluno.Restricoes);
    }

    [Fact]
    public void RemoverRestricao_Inexistente_RetornaFalse()
    {
        var aluno = Criar(restricoes: new[] { "APVL" });

        Assert.False(aluno.RemoverRestricao("Diabetes"));
        Assert.Single(aluno.Restricoes);
    }

    [Fact]
    public void AtualizarDados_ComDadosValidos_SubstituiEstadoERestricoes()
    {
        var aluno = Criar(restricoes: new[] { "APVL" });

        aluno.AtualizarDados(
            "João Souza",
            new DateOnly(2014, 3, 10),
            new Turma("Escola Nova", "6 ano"),
            "(11) 98888-7777",
            "  Atualizado  ",
            new[] { "Diabetes" },
            true,
            new Responsavel("Carlos Souza", "11977776666"));

        Assert.Equal("João Souza", aluno.Nome);
        Assert.Equal("Escola Nova", aluno.Turma.Escola);
        Assert.Equal("6 ano", aluno.Turma.Serie);
        Assert.Equal("11988887777", aluno.Telefone);
        Assert.Equal("Atualizado", aluno.Observacao);
        Assert.True(aluno.Anafilaxia);
        Assert.Equal("Carlos Souza", aluno.Responsavel.Nome);
        Assert.Equal("11977776666", aluno.Responsavel.Telefone);
        Assert.Single(aluno.Restricoes);
        Assert.Equal("Diabetes", aluno.Restricoes[0]);
    }

    [Fact]
    public void AtualizarDados_ComPayloadInvalido_NaoAlteraEstado()
    {
        var aluno = Criar(restricoes: new[] { "APVL" });

        Assert.Throws<ArgumentException>(() => aluno.AtualizarDados(
            "",
            new DateOnly(2014, 3, 10),
            new Turma("Escola Nova", "6 ano"),
            null,
            null,
            new[] { "Diabetes" },
            true,
            new Responsavel("Carlos Souza", null)));

        Assert.Equal("Maria Silva", aluno.Nome);
        Assert.Equal("Escola Municipal", aluno.Turma.Escola);
        Assert.Equal("Ana Silva", aluno.Responsavel.Nome);
        Assert.False(aluno.Anafilaxia);
        Assert.Single(aluno.Restricoes);
        Assert.Equal("APVL", aluno.Restricoes[0]);
    }
}
