using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

public class PoliticaDeAprovacaoTests
{
    [Fact]
    public void PapelPadrao_MapeiaCadaTipo()
    {
        Assert.Equal(PapelUsuario.Nutricionista,
            PoliticaDeAprovacao.PapelPadrao(TipoCadastroSolicitado.Nutricionista));
        Assert.Equal(PapelUsuario.Merendeira,
            PoliticaDeAprovacao.PapelPadrao(TipoCadastroSolicitado.Merendeira));
        Assert.Equal(PapelUsuario.Responsavel,
            PoliticaDeAprovacao.PapelPadrao(TipoCadastroSolicitado.Responsavel));
    }

    [Theory]
    [InlineData(PapelUsuario.AdminSecretaria)]
    [InlineData(PapelUsuario.AdminEscola)]
    public void Planejar_PapelAdministrativo_Rejeita(PapelUsuario papel)
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Responsavel, papel, null, null, null);

        Assert.Null(resultado.Plano);
        Assert.Contains(PoliticaDeAprovacao.MensagemPapelAdministrativo, resultado.Erros);
    }

    [Fact]
    public void Planejar_NutricionistaSemCrn_Rejeita()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Nutricionista, null, null, null, null);

        Assert.Null(resultado.Plano);
        Assert.NotEmpty(resultado.Erros);
    }

    [Fact]
    public void Planejar_NutricionistaComCrn_NormalizaCrn()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Nutricionista, null, null, "crn-3-12345", null);

        Assert.NotNull(resultado.Plano);
        Assert.Equal(PapelUsuario.Nutricionista, resultado.Plano!.Papel);
        Assert.Equal("CRN-3-12345", resultado.Plano.Crn);
        Assert.Null(resultado.Plano.EscolaId);
    }

    [Fact]
    public void Planejar_MerendeiraComCrn_Rejeita()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Merendeira, null, 7, "CRN-3-12345", null);

        Assert.Null(resultado.Plano);
        Assert.Contains(CrnDeNutricionista.MensagemSomenteNutricionista, resultado.Erros);
    }

    [Fact]
    public void Planejar_MerendeiraSemEscola_Rejeita()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Merendeira, null, null, null, null);

        Assert.Null(resultado.Plano);
        Assert.NotEmpty(resultado.Erros);
    }

    [Fact]
    public void Planejar_MerendeiraComEscola_ExigeEscola()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Merendeira, null, 7, null, null);

        Assert.NotNull(resultado.Plano);
        Assert.Equal(PapelUsuario.Merendeira, resultado.Plano!.Papel);
        Assert.Equal(7, resultado.Plano.EscolaId);
        Assert.Null(resultado.Plano.Crn);
    }

    [Fact]
    public void Planejar_ResponsavelSemAluno_Rejeita()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Responsavel, null, null, null, new List<int>());

        Assert.Null(resultado.Plano);
        Assert.NotEmpty(resultado.Erros);
    }

    [Fact]
    public void Planejar_ResponsavelComAluno_ExigeAlunos()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Responsavel, null, null, null, new List<int> { 12, 15 });

        Assert.NotNull(resultado.Plano);
        Assert.Equal(PapelUsuario.Responsavel, resultado.Plano!.Papel);
        Assert.True(resultado.Plano.ExigeAlunos);
        Assert.Null(resultado.Plano.EscolaId);
    }

    [Fact]
    public void Planejar_ResponsavelComEscola_Rejeita()
    {
        var resultado = PoliticaDeAprovacao.Planejar(
            TipoCadastroSolicitado.Responsavel, null, 7, null, new List<int> { 12 });

        Assert.Null(resultado.Plano);
        Assert.NotEmpty(resultado.Erros);
    }
}
