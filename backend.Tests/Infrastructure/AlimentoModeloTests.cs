using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ntc.Database;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Modelo e migration do catálogo de alimentos (Spec 011, T4): composição como tipo
/// complexo sem tabela filha, vínculo com chave composta e sem coluna de motivo, e seed
/// de categorias aplicado pela migration.
/// </summary>
public class AlimentoModeloTests
{
    private static ConnectionContext CriarContexto(SqliteConnection conexao)
    {
        var options = new DbContextOptionsBuilder<ConnectionContext>()
            .UseSqlite(conexao)
            .Options;

        return new ConnectionContext(options);
    }

    [Fact]
    public void Valores_EhTipoComplexo_SemTabelaFilha()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        using var contexto = CriarContexto(conexao);

        var alimento = contexto.Model.FindEntityType(typeof(Alimento));

        Assert.NotNull(alimento);

        // Tipo complexo não é navegação: não existe tabela filha nem chave própria.
        Assert.Null(alimento!.FindNavigation(nameof(Alimento.Valores)));

        var valores = alimento.FindComplexProperty(nameof(Alimento.Valores));
        Assert.NotNull(valores);
        Assert.Equal(6, valores!.ComplexType.GetProperties().Count());

        Assert.DoesNotContain(
            contexto.Model.GetEntityTypes(),
            tipo => tipo.ClrType == typeof(ValoresNutricionais));
    }

    [Fact]
    public void Vinculo_TemChaveComposta_E_NaoTemMotivo()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        using var contexto = CriarContexto(conexao);

        var vinculo = contexto.Model.FindEntityType(typeof(AlimentoRestricao));

        Assert.NotNull(vinculo);

        var chave = Assert.Single(vinculo!.GetKeys());
        Assert.Equal(
            new[] { nameof(AlimentoRestricao.AlimentoId), nameof(AlimentoRestricao.RestricaoId) },
            chave.Properties.Select(propriedade => propriedade.Name).ToArray());

        Assert.DoesNotContain(
            vinculo.GetProperties(),
            propriedade => propriedade.Name.Contains("Motivo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Alimento_TemIndiceUnicoGlobalNoNomeNormalizado()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        using var contexto = CriarContexto(conexao);

        var alimento = contexto.Model.FindEntityType(typeof(Alimento))!;

        var indice = Assert.Single(
            alimento.GetIndexes(),
            candidato => candidato.Properties.Count == 1 &&
                         candidato.Properties[0].Name == nameof(Alimento.NomeNormalizado));

        Assert.True(indice.IsUnique);
    }

    [Fact]
    public void Migration_SemeiaAsSeisCategorias_UmaVez()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        using var contexto = CriarContexto(conexao);

        contexto.Database.Migrate();

        var categorias = contexto.CategoriasAlimento
            .OrderBy(categoria => categoria.Nome)
            .Select(categoria => categoria.Nome)
            .ToList();

        Assert.Equal(
            new[]
            {
                "Carnes e ovos",
                "Cereais",
                "Frutas",
                "Hortaliças",
                "Laticínios",
                "Leguminosas"
            },
            categorias);
    }

    [Fact]
    public void Alimento_PersisteComposicaoEVinculo()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        using var contexto = CriarContexto(conexao);

        contexto.Database.Migrate();

        var categoria = contexto.CategoriasAlimento.Single(item => item.Nome == "Laticínios");
        var restricao = new RestricaoAlimentar("Lactose", CategoriaRestricao.Alergia);
        contexto.RestricoesAlimentares.Add(restricao);
        contexto.SaveChanges();

        var alimento = new Alimento(
            "Iogurte natural",
            categoria.Id,
            ValoresNutricionais.Criar(61, 3.5m, 4.7m, 3.3m, 0m, 46m),
            new[] { restricao.Id });

        contexto.Alimentos.Add(alimento);
        contexto.SaveChanges();

        contexto.ChangeTracker.Clear();

        var gravado = contexto.Alimentos
            .Include(item => item.Categoria)
            .Include(item => item.Vinculos)
                .ThenInclude(vinculo => vinculo.Restricao)
            .Single(item => item.Nome == "Iogurte natural");

        Assert.Equal("Laticínios", gravado.Categoria!.Nome);
        Assert.Equal(4.7m, gravado.Valores.CarboidratosG);
        Assert.Equal(46m, gravado.Valores.SodioMg);

        var vinculo = Assert.Single(gravado.Vinculos);
        Assert.Equal(CategoriaRestricao.Alergia, vinculo.Restricao!.Categoria);
    }
}
