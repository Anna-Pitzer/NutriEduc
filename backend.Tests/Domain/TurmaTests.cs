using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

public class TurmaTests
{
    [Fact]
    public void Construtor_ComDadosValidos_NormalizaTextos()
    {
        var turma = new Turma("  Escola Municipal  ", "  5 ano  ");

        Assert.Equal("Escola Municipal", turma.Escola);
        Assert.Equal("5 ano", turma.Serie);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("E")]
    public void Construtor_ComEscolaInvalida_Lanca(string escola)
    {
        Assert.Throws<ArgumentException>(() => { new Turma(escola, "5 ano"); });
    }

    [Fact]
    public void Construtor_ComEscolaAcimaDe150Caracteres_Lanca()
    {
        Assert.Throws<ArgumentException>(() => { new Turma(new string('a', 151), "5 ano"); });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1")]
    public void Construtor_ComSerieInvalida_Lanca(string serie)
    {
        Assert.Throws<ArgumentException>(() => { new Turma("Escola Municipal", serie); });
    }

    [Fact]
    public void Construtor_ComSerieAcimaDe50Caracteres_Lanca()
    {
        Assert.Throws<ArgumentException>(() => { new Turma("Escola Municipal", new string('a', 51)); });
    }

    [Fact]
    public void Transferir_ComDadosValidos_Atualiza()
    {
        var turma = new Turma("Escola Municipal", "5 ano");

        turma.Transferir("Escola Nova", "6 ano");

        Assert.Equal("Escola Nova", turma.Escola);
        Assert.Equal("6 ano", turma.Serie);
    }

    [Fact]
    public void Transferir_ComDadosInvalidos_NaoAlteraEstado()
    {
        var turma = new Turma("Escola Municipal", "5 ano");

        Assert.Throws<ArgumentException>(() => turma.Transferir("", "6 ano"));

        Assert.Equal("Escola Municipal", turma.Escola);
        Assert.Equal("5 ano", turma.Serie);
    }
}
