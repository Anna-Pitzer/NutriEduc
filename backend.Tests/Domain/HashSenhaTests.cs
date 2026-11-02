using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

public class HashSenhaTests
{
    [Fact]
    public void CriarEConfere_ComSenhaCorreta_RetornaTrue()
    {
        var hash = HashSenha.Criar("abcdefgh");

        Assert.True(hash.Confere("abcdefgh"));
    }

    [Fact]
    public void Confere_ComSenhaIncorreta_RetornaFalse()
    {
        var hash = HashSenha.Criar("abcdefgh");

        Assert.False(hash.Confere("abcdefgi"));
    }

    [Fact]
    public void ToStringEConfere_RoundTripPreservaSenha()
    {
        var original = HashSenha.Criar("abcdefgh");

        var persistido = original.ToString();

        Assert.True(HashSenha.TentarLer(persistido, out var lido));
        Assert.True(lido.Confere("abcdefgh"));
        Assert.False(lido.Confere("12345678"));
    }

    [Fact]
    public void Criar_ComMesmaSenha_GeraSaltDistinto()
    {
        var primeiro = HashSenha.Criar("abcdefgh").ToString();
        var segundo = HashSenha.Criar("abcdefgh").ToString();

        Assert.NotEqual(primeiro, segundo);
    }

    [Fact]
    public void ToString_UsaFormatoVersionado()
    {
        var partes = HashSenha.Criar("abcdefgh").ToString().Split('$');

        Assert.Equal(4, partes.Length);
        Assert.Equal("pbkdf2-sha256", partes[0]);
        Assert.Equal(100_000, int.Parse(partes[1]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("pbkdf2-sha256")]
    [InlineData("pbkdf2-sha256$100000$YWJj")]
    [InlineData("pbkdf2-sha256$100000$YWJj$ZGVm$Z2hp")]
    [InlineData("outro-algoritmo$100000$YWJj$ZGVm")]
    [InlineData("pbkdf2-sha256$zero$YWJj$ZGVm")]
    [InlineData("pbkdf2-sha256$0$YWJj$ZGVm")]
    [InlineData("pbkdf2-sha256$100000$@@@$ZGVm")]
    public void TentarLer_ComValorInvalido_RetornaFalse(string? valor)
    {
        Assert.False(HashSenha.TentarLer(valor, out _));
    }

    [Fact]
    public void Confere_EmHashNaoInicializado_RetornaFalse()
    {
        var hash = default(HashSenha);

        Assert.False(hash.Confere("abcdefgh"));
    }

    [Fact]
    public void Criar_ComSenhaVazia_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            HashSenha.Criar("");
        });
    }
}
