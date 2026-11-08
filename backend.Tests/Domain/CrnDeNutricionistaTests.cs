using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Formato e normalização do objeto de valor do CRN (Spec 031, RF1/RF2/RF5).
/// </summary>
public class CrnDeNutricionistaTests
{
    [Theory]
    [InlineData("CRN-3-12345", "CRN-3-12345")]
    [InlineData("crn-3-12345", "CRN-3-12345")]
    [InlineData("CRN 3 12345", "CRN-3-12345")]
    [InlineData("CRN.3.12345", "CRN-3-12345")]
    [InlineData("  crn-11-12345678  ", "CRN-11-12345678")]
    public void TryParse_AceitaVariacoes_Normaliza(string entrada, string esperado)
    {
        Assert.True(CrnDeNutricionista.TryParse(entrada, null, out var crn, out var erro), erro);

        Assert.NotNull(crn);
        Assert.Equal(esperado, crn!.Valor);
        Assert.Null(erro);
    }

    [Theory]
    [InlineData("CRN-3")]            // número ausente
    [InlineData("CRN-3-12")]         // número curto
    [InlineData("CRN-3-123456789")]  // número longo
    [InlineData("CRN-12-12345")]     // região fora da faixa
    [InlineData("CRN-0-12345")]      // região zero
    [InlineData("CRN-3-1234A")]      // letra no número
    [InlineData("CRN-3-12345/P")]    // sufixo não suportado
    [InlineData("RN-3-12345")]       // prefixo errado
    [InlineData("")]                 // vazio
    [InlineData(null)]               // nulo
    public void TryParse_RejeitaFormatoInvalido(string? entrada)
    {
        Assert.False(CrnDeNutricionista.TryParse(entrada, null, out var crn, out var erro));

        Assert.Null(crn);
        Assert.NotNull(erro);
    }

    [Fact]
    public void EhValido_ComCrnCanonico_RetornaTrue()
    {
        Assert.True(CrnDeNutricionista.EhValido("CRN-3-12345"));
    }

    [Fact]
    public void EhValido_ComCrnNulo_RetornaFalse()
    {
        Assert.False(CrnDeNutricionista.EhValido(null));
    }

    [Fact]
    public void RegioesPadrao_CobreUmAOnze()
    {
        for (var regiao = 1; regiao <= 11; regiao++)
        {
            Assert.True(CrnDeNutricionista.EhValido($"CRN-{regiao}-12345"));
        }

        Assert.False(CrnDeNutricionista.EhValido("CRN-12-12345"));
    }
}
