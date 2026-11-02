using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

public class ResponsavelTests
{
    [Fact]
    public void Construtor_ComDadosValidos_NormalizaNomeETelefone()
    {
        var responsavel = new Responsavel("  Ana Silva  ", "(11) 98888-8888");

        Assert.Equal("Ana Silva", responsavel.Nome);
        Assert.Equal("11988888888", responsavel.Telefone);
        Assert.True(responsavel.PossuiTelefone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_SemTelefone_UsaStringVazia(string? telefone)
    {
        var responsavel = new Responsavel("Ana Silva", telefone);

        Assert.Equal(string.Empty, responsavel.Telefone);
        Assert.False(responsavel.PossuiTelefone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Construtor_ComNomeInvalido_Lanca(string nome)
    {
        Assert.Throws<ArgumentException>(() => { new Responsavel(nome, null); });
    }

    [Fact]
    public void Construtor_ComNomeAcimaDe100Caracteres_Lanca()
    {
        Assert.Throws<ArgumentException>(() => { new Responsavel(new string('a', 101), null); });
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void Construtor_ComTelefoneInvalido_Lanca(string telefone)
    {
        Assert.Throws<ArgumentException>(() => { new Responsavel("Ana Silva", telefone); });
    }

    [Fact]
    public void AtualizarContato_ComDadosValidos_Atualiza()
    {
        var responsavel = new Responsavel("Ana Silva", "11988888888");

        responsavel.AtualizarContato("Carlos Souza", "11977776666");

        Assert.Equal("Carlos Souza", responsavel.Nome);
        Assert.Equal("11977776666", responsavel.Telefone);
    }

    [Fact]
    public void AtualizarContato_ComDadosInvalidos_NaoAlteraEstado()
    {
        var responsavel = new Responsavel("Ana Silva", "11988888888");

        Assert.Throws<ArgumentException>(() => responsavel.AtualizarContato("", "11977776666"));

        Assert.Equal("Ana Silva", responsavel.Nome);
        Assert.Equal("11988888888", responsavel.Telefone);
    }
}
