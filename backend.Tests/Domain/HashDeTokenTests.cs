using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Objeto de valor do resumo de token (Spec 024, F2): o hash é formato do domínio, a
/// conferência é em tempo constante e o valor em claro nunca entra na entidade.
/// </summary>
public class HashDeTokenTests
{
    [Fact]
    public void Criar_ProduzSha256EmHexadecimal()
    {
        var hash = HashDeToken.Criar("token-de-teste");

        Assert.Equal(64, hash.ToString().Length);
        Assert.Matches("^[0-9A-F]{64}$", hash.ToString());
    }

    [Fact]
    public void Criar_ComEspacosNasPontas_ProduzMesmoResumo()
    {
        var semEspaco = HashDeToken.Criar("token");
        var comEspaco = HashDeToken.Criar("  token  ");

        Assert.Equal(semEspaco.ToString(), comEspaco.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Criar_ComValorVazio_Lanca(string? valor)
    {
        Assert.Throws<ArgumentException>(() => HashDeToken.Criar(valor));
    }

    [Fact]
    public void Confere_ComMesmoValor_RetornaTrue()
    {
        var hash = HashDeToken.Criar("segredo");

        Assert.True(hash.Confere("segredo"));
        Assert.True(hash.Confere("  segredo  "));
    }

    [Fact]
    public void Confere_ComValorDiferente_RetornaFalse()
    {
        var hash = HashDeToken.Criar("segredo");

        Assert.False(hash.Confere("outro"));
        Assert.False(hash.Confere(""));
        Assert.False(hash.Confere(null));
    }

    [Fact]
    public void TentarLer_ComResumoPersistido_PermiteConferir()
    {
        var original = HashDeToken.Criar("segredo");

        Assert.True(HashDeToken.TentarLer(original.ToString(), out var lido));
        Assert.True(lido.Confere("segredo"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void TentarLer_ComValorAusente_RetornaFalse(string? persistido)
    {
        Assert.False(HashDeToken.TentarLer(persistido, out _));
    }

    [Fact]
    public void Default_NaoConfereNada()
    {
        var vazio = default(HashDeToken);

        Assert.False(vazio.Confere("qualquer"));
        Assert.Equal(string.Empty, vazio.ToString());
    }

    [Fact]
    public void SegredosConferem_ComparaEmTempoConstante()
    {
        Assert.True(HashDeToken.SegredosConferem("abc", "abc"));
        Assert.False(HashDeToken.SegredosConferem("abc", "abd"));
        Assert.False(HashDeToken.SegredosConferem("abc", "abcd"));
        Assert.False(HashDeToken.SegredosConferem(null, "abc"));
        Assert.False(HashDeToken.SegredosConferem("abc", ""));
    }
}
