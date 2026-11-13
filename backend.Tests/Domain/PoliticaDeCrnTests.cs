using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Predicado único usado pelas operações de cardápio (Spec 031, RF9). Garante que a
/// mensagem e a regra de validade não sejam reimplementadas pelos serviços de cardápio.
/// </summary>
public class PoliticaDeCrnTests
{
    private static Usuario Nutricionista(string? crn)
    {
        var usuario = new Usuario(
            "Nutri Teste",
            "12345678901",
            new DateOnly(1990, 1, 1),
            "nutri@teste.com",
            PapelUsuario.Nutricionista,
            null,
            1,
            "CRN-3-12345");

        PropriedadesPrivadas.Definir(usuario, nameof(Usuario.Crn), crn);

        return usuario;
    }

    [Fact]
    public void NutricionistaSemCrn_RetornaMensagemDeBloqueio()
    {
        var motivo = PoliticaDeCrn.ExigirParaCardapio(Nutricionista(null));

        Assert.Equal(PoliticaDeCrn.MensagemCardapioSemCrn, motivo);
    }

    [Fact]
    public void NutricionistaComCrnInvalido_RetornaMensagemDeBloqueio()
    {
        var motivo = PoliticaDeCrn.ExigirParaCardapio(Nutricionista("CRN-12-12345"));

        Assert.Equal(PoliticaDeCrn.MensagemCardapioSemCrn, motivo);
    }

    [Fact]
    public void NutricionistaComCrnValido_NaoBloqueia()
    {
        Assert.Null(PoliticaDeCrn.ExigirParaCardapio(Nutricionista("CRN-3-12345")));
    }

    [Fact]
    public void OutroPapel_NaoBloqueia()
    {
        var merendeira = new Usuario(
            "Merendeira Teste",
            "98765432100",
            new DateOnly(1990, 1, 1),
            "merenda@teste.com",
            PapelUsuario.Merendeira,
            1,
            null);

        Assert.Null(PoliticaDeCrn.ExigirParaCardapio(merendeira));
    }
}
