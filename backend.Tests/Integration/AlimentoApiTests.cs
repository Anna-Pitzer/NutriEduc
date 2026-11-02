using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP do catálogo de alimentos (Spec 011): criação com composição, erros de
/// validação, conflito de nome, política de leitura, paginação, busca em duas etapas,
/// soft delete e seed das categorias.
/// </summary>
public class AlimentoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AlimentoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object Payload(
        string nome,
        int categoriaId,
        decimal proteinas = 2.7m,
        params int[] restricaoIds) => new
    {
        nome,
        categoriaId,
        valores = new
        {
            caloriasKcal = 130m,
            proteinasG = proteinas,
            carboidratosG = 28.2m,
            gordurasG = 0.3m,
            fibrasG = 0.4m,
            sodioMg = 1m
        },
        restricaoIds
    };

    private static async Task<int> PrimeiraCategoria(HttpClient client)
    {
        var categorias = await client.GetFromJsonAsync<JsonElement>("/api/categorias-alimento");

        Assert.True(categorias.GetArrayLength() > 0, "O seed de categorias não populou nada.");

        return categorias[0].GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarAlimento(
        HttpClient client,
        string nome,
        int categoriaId,
        params int[] restricaoIds)
    {
        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload(nome, categoriaId, restricaoIds: restricaoIds));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> Listar(HttpClient client, string busca) =>
        await client.GetFromJsonAsync<JsonElement>(
            $"/api/alimentos?busca={Uri.EscapeDataString(busca)}");

    /// <summary>Id de uma restrição do catálogo semeado pela fábrica de testes.</summary>
    private static async Task<int> RestricaoId(HttpClient client, string nome)
    {
        var restricoes = await client.GetFromJsonAsync<JsonElement>("/api/restricoes");

        foreach (var item in restricoes.EnumerateArray())
        {
            if (string.Equals(
                    item.GetProperty("nome").GetString(),
                    nome,
                    StringComparison.OrdinalIgnoreCase))
            {
                return item.GetProperty("id").GetInt32();
            }
        }

        throw new InvalidOperationException($"Restrição {nome} não existe no catálogo de teste.");
    }

    private static bool ContemId(JsonElement pagina, int id) =>
        pagina.GetProperty("itens").EnumerateArray()
            .Any(item => item.GetProperty("id").GetInt32() == id);

    [Fact]
    public async Task Get_Categorias_TrazOSeedDaMigration()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var categorias = await client.GetFromJsonAsync<JsonElement>("/api/categorias-alimento");

        var nomes = categorias.EnumerateArray()
            .Select(item => item.GetProperty("nome").GetString())
            .ToList();

        Assert.True(nomes.Count >= 6, $"Esperava ao menos 6 categorias, veio {nomes.Count}.");
        Assert.Contains("Cereais", nomes);
        Assert.Contains("Laticínios", nomes);
    }

    [Fact]
    public async Task Post_Alimento_Retorna201ComComposicaoECategoria()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var nome = $"Arroz branco {Guid.NewGuid():N}";

        var resposta = await client.PostAsJsonAsync("/api/alimentos", Payload(nome, categoriaId));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var id = corpo.GetProperty("id").GetInt32();

        Assert.Equal($"/api/alimentos/{id}", resposta.Headers.Location?.ToString());
        Assert.Equal(nome, corpo.GetProperty("nome").GetString());
        Assert.Equal(categoriaId, corpo.GetProperty("categoria").GetProperty("id").GetInt32());
        Assert.Equal(130m, corpo.GetProperty("valores").GetProperty("caloriasKcal").GetDecimal());
        Assert.Empty(corpo.GetProperty("vinculos").EnumerateArray());
        Assert.True(corpo.GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task Post_Alimento_ComValorNegativo_Retorna400ESemPersistencia()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var nome = $"Negativo {Guid.NewGuid():N}";

        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload(nome, categoriaId, proteinas: -1));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("negativo", corpo.GetProperty("erro").GetString()!);

        var pagina = await Listar(client, nome);
        Assert.Equal(0, pagina.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Post_Alimento_ComTresCasasDecimais_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);

        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload($"Tres casas {Guid.NewGuid():N}", categoriaId, proteinas: 2.735m));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("duas casas decimais", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Post_Alimento_ComNomeDuplicado_Retorna409()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var nome = $"Feijão carioca {Guid.NewGuid():N}";

        await CriarAlimento(client, nome, categoriaId);

        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload(nome.ToUpperInvariant(), categoriaId));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("existe", corpo.GetProperty("erro").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_Alimento_ComVinculoRepetido_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var restricaoId = await RestricaoId(client, "Lactose");

        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload(
                $"Vinculo repetido {Guid.NewGuid():N}",
                categoriaId,
                proteinas: 2.7m,
                restricaoIds: new[] { restricaoId, restricaoId }));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_Alimento_PorAdminEscola_Retorna403()
    {
        var nutricao = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(nutricao);

        var (escolaId, _) = _factory.CriarEscolaComUnidade();
        var client = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId: escolaId);

        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload($"Sem permissao {Guid.NewGuid():N}", categoriaId));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Alimentos_PorAdminSecretaria_Retorna403()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var resposta = await client.GetAsync("/api/alimentos");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Alimentos_ComIncluirInativos_PorMerendeira_Retorna403()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var client = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId: escolaId, unidadeId: unidadeId);

        var resposta = await client.GetAsync("/api/alimentos?incluirInativos=true");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Alimentos_ComTamanhoForaDaFaixa_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/alimentos?tamanho=0");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Alimentos_BuscaPorPrefixo_Encontra()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var nome = $"Abacaxi {Guid.NewGuid():N}";
        var id = await CriarAlimento(client, nome, categoriaId);

        var pagina = await Listar(client, "abacaxi");

        Assert.True(ContemId(pagina, id));
        Assert.True(pagina.GetProperty("total").GetInt32() >= 1);
    }

    [Fact]
    public async Task Get_Alimentos_BuscaSemPrefixo_UsaContemComoFallback()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var id = await CriarAlimento(
            client, $"Leite de coco {Guid.NewGuid():N}", categoriaId);

        var pagina = await Listar(client, "leite");

        Assert.True(ContemId(pagina, id),
            "A busca por 'leite' deveria achar 'Leite de coco' pelo fallback de contém.");
    }

    [Fact]
    public async Task Alimento_Desativado_SomeDaListagemEContinuaPorId()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var nome = $"Desativado {Guid.NewGuid():N}";
        var id = await CriarAlimento(client, nome, categoriaId);

        var desativar = await client.PostAsync($"/api/alimentos/{id}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var pagina = await Listar(client, nome);
        Assert.Equal(0, pagina.GetProperty("total").GetInt32());

        var detalhe = await client.GetAsync($"/api/alimentos/{id}");
        Assert.Equal(HttpStatusCode.OK, detalhe.StatusCode);

        var corpo = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(corpo.GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task Categoria_Desativada_NaoEscondeOsAlimentosDela()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var nomeCategoria = $"Categoria {Guid.NewGuid():N}";

        var criada = await client.PostAsJsonAsync(
            "/api/categorias-alimento", new { nome = nomeCategoria });
        var categoriaId = (await criada.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var nome = $"Alimento de categoria {Guid.NewGuid():N}";
        var id = await CriarAlimento(client, nome, categoriaId);

        var desativar = await client.PostAsync(
            $"/api/categorias-alimento/{categoriaId}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var pagina = await Listar(client, nome);
        Assert.True(ContemId(pagina, id),
            "Desativar a categoria não pode esconder os alimentos dela.");

        var novo = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload($"Novo na categoria inativa {Guid.NewGuid():N}", categoriaId));
        Assert.Equal(HttpStatusCode.BadRequest, novo.StatusCode);
    }

    [Fact]
    public async Task Get_Alimento_Inexistente_Retorna404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/alimentos/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("não encontrado", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Post_Alimento_ComVinculo_TrazCategoriaDaRestricaoESemMotivo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var restricaoId = await RestricaoId(client, "Lactose");

        var resposta = await client.PostAsJsonAsync(
            "/api/alimentos",
            Payload(
                $"Iogurte {Guid.NewGuid():N}",
                categoriaId,
                proteinas: 3.5m,
                restricaoIds: new[] { restricaoId }));

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var vinculo = Assert.Single(corpo.GetProperty("vinculos").EnumerateArray());

        Assert.Equal(restricaoId, vinculo.GetProperty("restricaoId").GetInt32());
        Assert.Equal("Lactose", vinculo.GetProperty("restricao").GetString());
        Assert.True(vinculo.TryGetProperty("categoria", out _));
        Assert.False(vinculo.TryGetProperty("motivo", out _));
    }

    [Fact]
    public async Task Get_Alimentos_OrdenaPorNomeDescendente()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var categoriaId = await PrimeiraCategoria(client);
        var marca = $"ordenacao{Guid.NewGuid():N}"[..16];

        await CriarAlimento(client, $"{marca} Alfa", categoriaId);
        await CriarAlimento(client, $"{marca} Beta", categoriaId);

        var corpo = await client.GetFromJsonAsync<JsonElement>(
            $"/api/alimentos?busca={marca}&ordenar=nome-desc");

        var nomes = corpo
            .GetProperty("itens")
            .EnumerateArray()
            .Select(item => item.GetProperty("nome").GetString())
            .ToList();

        Assert.Equal(new[] { $"{marca} Beta", $"{marca} Alfa" }, nomes);
    }

    [Fact]
    public async Task Get_Alimentos_ComOrdenacaoDesconhecida_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/alimentos?ordenar=calorias");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Ordenação inválida", corpo.GetProperty("erro").GetString()!);
    }
}
