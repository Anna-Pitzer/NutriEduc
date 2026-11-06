using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP da publicação, versionamento e histórico (Spec 015).
/// </summary>
public class PublicacaoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PublicacaoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CriarCategoria(HttpClient client, string nome)
    {
        var resposta = await client.PostAsJsonAsync("/api/categorias-alimento", new { nome });
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarAlimento(HttpClient client, int categoriaId, string nome)
    {
        var resposta = await client.PostAsJsonAsync("/api/alimentos", new
        {
            nome,
            categoriaId,
            valores = new
            {
                caloriasKcal = 130m,
                proteinasG = 2.7m,
                carboidratosG = 28.2m,
                gordurasG = 0.3m,
                fibrasG = 0.4m,
                sodioMg = 1m
            },
            restricaoIds = Array.Empty<int>()
        });

        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    private static async Task<int> NutricionistaId(HttpClient client)
    {
        var lista = await client.GetFromJsonAsync<JsonElement>("/api/nutricionistas");
        return lista[0].GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarCardapio(
        HttpClient client,
        int unidadeId,
        int alimentoId,
        string nome = "Semana 12")
    {
        var resposta = await client.PostAsJsonAsync("/api/cardapios", new
        {
            nome,
            unidadeId,
            nutricionistaId = await NutricionistaId(client),
            confirmarIncompatibilidades = true,
            periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
            refeicoes = new[]
            {
                new
                {
                    nome = "Almoço",
                    ordem = 1,
                    itens = new[] { new { alimentoId, gramas = 100m } }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarAlimentoComRestricao(
        HttpClient client,
        int categoriaId,
        int restricaoId,
        string nome)
    {
        var resposta = await client.PostAsJsonAsync("/api/alimentos", new
        {
            nome,
            categoriaId,
            valores = new
            {
                caloriasKcal = 130m,
                proteinasG = 2.7m,
                carboidratosG = 28.2m,
                gordurasG = 0.3m,
                fibrasG = 0.4m,
                sodioMg = 1m
            },
            restricaoIds = new[] { restricaoId }
        });

        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Substituto seguro da mesma categoria: sem ele a montagem manual bloqueia o item
        // incompatível (Spec 029, RF12), e o teste de publicacao nao chegaria ao 422.
        var seguro = await client.PostAsJsonAsync("/api/alimentos", new
        {
            nome = $"Seguro {Guid.NewGuid():N}",
            categoriaId,
            valores = new
            {
                caloriasKcal = 130m,
                proteinasG = 2.7m,
                carboidratosG = 28.2m,
                gordurasG = 0.3m,
                fibrasG = 0.4m,
                sodioMg = 1m
            },
            restricaoIds = Array.Empty<int>()
        });

        seguro.EnsureSuccessStatusCode();

        return corpo.GetProperty("id").GetInt32();
    }

    private int ObterRestricaoId(string nome)
    {
        using var scope = _factory.Services.CreateScope();
        var restricoes = scope.ServiceProvider.GetRequiredService<IRestricaoRepository>();
        return restricoes.Listar(null, false).First(restricao => restricao.Nome == nome).Id;
    }

    private void CriarAlunoComRestricao(int unidadeId, string restricao)
    {
        using var scope = _factory.Services.CreateScope();
        var alunos = scope.ServiceProvider.GetRequiredService<IAlunoRepository>();
        var escolas = scope.ServiceProvider.GetRequiredService<IEscolaRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var escolaId = escolas.ListarPorUnidade(unidadeId).FirstOrDefault()?.Id
            ?? throw new InvalidOperationException(
                $"Unidade {unidadeId} não possui escola para vincular o aluno.");

        var aluno = new Aluno(
            $"Aluno {Guid.NewGuid():N}",
            new DateOnly(2015, 5, 20),
            new Turma("Escola Municipal", "5 ano"),
            null,
            null,
            new[] { restricao },
            false,
            new Responsavel("Ana Silva", "11988888888"),
            escolaId);

        aluno.DefinirUnidade(unidadeId);

        unidadeDeTrabalho.ExecutarEmTransacao(() => alunos.Add(aluno));
    }

    [Fact]
    public async Task Publicar_Rascunho_RetornaPublicado()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(client, unidadeId, arroz);

        var resposta = await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Publicado", corpo.GetProperty("situacao").GetString());
    }

    [Fact]
    public async Task Publicar_JaPublicado_Retorna409()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(client, unidadeId, arroz);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null)).StatusCode);
    }

    [Fact]
    public async Task Revisao_CriaNovaVersao()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(client, unidadeId, arroz);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null)).StatusCode);

        var revisao = await client.PostAsync($"/api/cardapios/{cardapioId}/revisao", null);

        Assert.Equal(HttpStatusCode.Created, revisao.StatusCode);

        var corpo = await revisao.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(cardapioId, corpo.GetProperty("id").GetInt32());
        Assert.Equal("Rascunho", corpo.GetProperty("situacao").GetString());
    }

    [Fact]
    public async Task Cancelar_SemMotivo_Retorna400()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(client, unidadeId, arroz);

        var resposta = await client.PostAsJsonAsync($"/api/cardapios/{cardapioId}/cancelar", new { motivo = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Historico_ListaEventos()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(client, unidadeId, arroz);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null)).StatusCode);

        var historico = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/{cardapioId}/historico");

        var eventos = historico.GetProperty("itens").EnumerateArray().ToList();

        Assert.Contains(eventos, evento => evento.GetProperty("evento").GetString() == "Criado");
        Assert.Contains(eventos, evento => evento.GetProperty("evento").GetString() == "Publicado");
    }

    [Fact]
    public async Task Publicar_ComAlergenicoEAdaptadoAtualizado_PublicaGeralEAdaptado()
    {
        _factory.GarantirCatalogo();
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var lactoseId = ObterRestricaoId("Lactose");
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var iogurte = await CriarAlimentoComRestricao(client, categoriaId, lactoseId, $"Iogurte {Guid.NewGuid():N}");

        CriarAlunoComRestricao(unidadeId, "Lactose");

        var cardapioId = await CriarCardapio(client, unidadeId, iogurte);

        // Spec 029, RF4 e RF15: o geral serve apenas aluno sem restrição; o aluno com
        // alergia é servido pela versão adaptada. O alergênico não bloqueia o geral.
        var resposta = await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var adaptados = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/{cardapioId}/adaptados");

        var adaptado = Assert.Single(adaptados.EnumerateArray());

        Assert.Equal("Atualizado", adaptado.GetProperty("statusAdaptacao").GetString());
        Assert.Equal(0, adaptado.GetProperty("totalPendencias").GetInt32());

        var publicacaoAdaptada = await client.PostAsync(
            $"/api/cardapios/{adaptado.GetProperty("id").GetInt32()}/publicar",
            null);

        Assert.Equal(HttpStatusCode.OK, publicacaoAdaptada.StatusCode);
    }

    [Fact]
    public async Task Publicar_PeriodoSobreposto_Retorna409ComCardapioId()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var primeiroId = await CriarCardapio(client, unidadeId, arroz);
        var segundoId = await CriarCardapio(client, unidadeId, arroz);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/cardapios/{primeiroId}/publicar", null)).StatusCode);

        var resposta = await client.PostAsync($"/api/cardapios/{segundoId}/publicar", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(primeiroId, corpo.GetProperty("detalhes").GetProperty("cardapioId").GetInt32());
    }

    [Fact]
    public async Task Put_Publicado_Retorna409()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(client, unidadeId, arroz);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/cardapios/{cardapioId}/publicar", null)).StatusCode);

        var resposta = await client.PutAsJsonAsync($"/api/cardapios/{cardapioId}", new
        {
            nome = "Semana alterada",
            unidadeId,
            periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
            refeicoes = new[]
            {
                new
                {
                    nome = "Almoço",
                    ordem = 1,
                    itens = new[] { new { alimentoId = arroz, gramas = 100m } }
                }
            }
        });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task PublicarRevisao_MarcaOriginalComoSubstituido()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var originalId = await CriarCardapio(client, unidadeId, arroz);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/cardapios/{originalId}/publicar", null)).StatusCode);

        var revisaoResposta = await client.PostAsync($"/api/cardapios/{originalId}/revisao", null);
        Assert.Equal(HttpStatusCode.Created, revisaoResposta.StatusCode);
        var revisaoId = (await revisaoResposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/cardapios/{revisaoId}/publicar", null)).StatusCode);

        var original = await client.GetFromJsonAsync<JsonElement>($"/api/cardapios/{originalId}");
        Assert.Equal("Substituido", original.GetProperty("situacao").GetString());

        var revisao = await client.GetFromJsonAsync<JsonElement>($"/api/cardapios/{revisaoId}");
        Assert.Equal("Publicado", revisao.GetProperty("situacao").GetString());
    }

    [Fact]
    public async Task Alteracoes_MerendeiraVeApenasPropriaUnidade()
    {
        var (_, unidadeA) = _factory.CriarEscolaComUnidade();
        var escolaA = _factory.CriarEscolaNaUnidade(unidadeA);
        var (nutricionista, nutricionistaId) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            escolaId: null,
            unidadeId: unidadeA);
        var (_, unidadeB) = _factory.CriarEscolaComUnidade();
        var categoriaId = await CriarCategoria(nutricionista, $"Categoria {Guid.NewGuid():N}");
        var arroz = await CriarAlimento(nutricionista, categoriaId, $"Arroz {Guid.NewGuid():N}");

        var (nutricionistaB, _) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            escolaId: null,
            unidadeId: unidadeB);

        var cardapioA = await CriarCardapio(nutricionista, unidadeA, arroz, "Cardápio da Unidade A");
        var cardapioB = await CriarCardapio(nutricionistaB, unidadeB, arroz, "Cardápio da Unidade B");

        Assert.Equal(HttpStatusCode.OK,
            (await nutricionista.PostAsync($"/api/cardapios/{cardapioA}/publicar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await nutricionistaB.PostAsync($"/api/cardapios/{cardapioB}/publicar", null)).StatusCode);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);

        var pagina = await merendeira.GetFromJsonAsync<JsonElement>(
            "/api/cardapios/alteracoes?desde=2000-01-01T00:00:00Z");

        var eventos = pagina.GetProperty("itens").EnumerateArray().ToList();

        Assert.NotEmpty(eventos);
        Assert.All(eventos, evento =>
            Assert.Equal("Cardápio da Unidade A", evento.GetProperty("cardapioNome").GetString()));
    }
}
