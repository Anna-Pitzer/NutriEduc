using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Regras do catálogo de restrições (Spec 010, RF1): limite do nome, normalização usada
/// como chave de comparação e desativação sem exclusão (ADR-008).
/// </summary>
public class RestricaoAlimentarTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("")]
    [InlineData("   ")]
    public void NomeCurtoDemaisLancaArgumentException(string nome)
    {
        Assert.Throws<ArgumentException>(() =>
            new RestricaoAlimentar(nome, CategoriaRestricao.Alergia));
    }

    [Fact]
    public void NomeCom61CaracteresLancaArgumentException()
    {
        var nome = new string('a', 61);

        Assert.Throws<ArgumentException>(() =>
            new RestricaoAlimentar(nome, CategoriaRestricao.Restricao));
    }

    [Fact]
    public void NomeNosLimitesEhAceito()
    {
        var minimo = new RestricaoAlimentar("Le", CategoriaRestricao.Alergia);
        var maximo = new RestricaoAlimentar(new string('a', 60), CategoriaRestricao.Restricao);

        Assert.Equal("Le", minimo.Nome);
        Assert.Equal(60, maximo.Nome.Length);
        Assert.True(minimo.Ativa);
    }

    [Theory]
    [InlineData("Lactose", "lactose")]
    [InlineData("  Lactose  ", "lactose")]
    [InlineData("Açúcar", "acucar")]
    [InlineData("GLÚTEN", "gluten")]
    public void NormalizarRemoveCaixaEEspacosEAcento(string entrada, string esperado)
    {
        Assert.Equal(esperado, RestricaoAlimentar.Normalizar(entrada));
    }

    [Fact]
    public void NormalizarDeNuloDevolveVazio()
    {
        Assert.Equal(string.Empty, RestricaoAlimentar.Normalizar(null));
    }

    [Fact]
    public void NomeNormalizadoEhGravadoNaCriacao()
    {
        var restricao = new RestricaoAlimentar(" Açúcar ", CategoriaRestricao.Restricao);

        Assert.Equal("Açúcar", restricao.Nome);
        Assert.Equal("acucar", restricao.NomeNormalizado);
    }

    [Fact]
    public void DesativarEReativarAlteramSituacaoSemApagar()
    {
        var restricao = new RestricaoAlimentar("Amendoim", CategoriaRestricao.Alergia);

        restricao.Desativar();
        Assert.False(restricao.Ativa);

        restricao.Reativar();
        Assert.True(restricao.Ativa);
    }

    [Fact]
    public void CategoriaInvalidaLancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new RestricaoAlimentar("Lactose", (CategoriaRestricao)99));
    }
}
