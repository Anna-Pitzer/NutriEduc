using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Regra de vínculo do aluno com o catálogo (Spec 010, RF3 e RF4), agora no domínio:
/// resolve nomes para a forma canônica e recusa desconhecido ou inativo.
/// </summary>
public class CatalogoDeRestricoesTests
{
    private static CatalogoDeRestricoes CatalogoPadrao()
    {
        return new CatalogoDeRestricoes(new[]
        {
            new RestricaoAlimentar("Lactose", CategoriaRestricao.Alergia),
            new RestricaoAlimentar("Diabetes", CategoriaRestricao.Restricao)
        });
    }

    [Fact]
    public void ResolverNomes_DevolveNomeCanonicoDoCatalogo()
    {
        var canonicos = CatalogoPadrao().ResolverNomes(new[] { "  lactose  " });

        Assert.Equal(new[] { "Lactose" }, canonicos);
    }

    [Fact]
    public void ResolverNomes_SemAcentoECaixa_ResolveIgual()
    {
        var catalogo = new CatalogoDeRestricoes(new[]
        {
            new RestricaoAlimentar("Intolerância ao Glúten", CategoriaRestricao.Restricao)
        });

        var canonicos = catalogo.ResolverNomes(new[] { "intolerancia ao gluten" });

        Assert.Equal(new[] { "Intolerância ao Glúten" }, canonicos);
    }

    [Fact]
    public void ResolverNomes_Desconhecido_LancaNomeandoOValorInformado()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            CatalogoPadrao().ResolverNomes(new[] { "Amendoim" }));

        Assert.Equal("Restrição desconhecida: Amendoim.", erro.Message);
    }

    [Fact]
    public void ResolverNomes_ItemInativo_NaoEhVinculavel()
    {
        var inativa = new RestricaoAlimentar("Lactose", CategoriaRestricao.Alergia);
        inativa.Desativar();

        var catalogo = new CatalogoDeRestricoes(new[] { inativa });

        Assert.Throws<ArgumentException>(() => catalogo.ResolverNomes(new[] { "Lactose" }));
    }

    [Fact]
    public void ResolverNomes_EntradaVaziaDevolveListaVazia()
    {
        Assert.Empty(CatalogoPadrao().ResolverNomes(Array.Empty<string>()));
    }

    [Fact]
    public void NomesInformados_RemoveVaziosEEspacosEDuplicatas()
    {
        var nomes = CatalogoDeRestricoes.NomesInformados(
            new[] { " Lactose ", "", "   ", "lactose", "Diabetes" });

        Assert.Equal(new[] { "Lactose", "Diabetes" }, nomes);
    }

    [Fact]
    public void NomesNormalizados_RemoveAcentoEDuplicatas()
    {
        var nomes = CatalogoDeRestricoes.NomesNormalizados(
            new[] { "Açúcar", "acucar" });

        Assert.Equal(new[] { "acucar" }, nomes);
    }
}
