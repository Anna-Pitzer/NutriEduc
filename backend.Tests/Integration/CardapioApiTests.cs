using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP do cardápio (Spec 012): criação com totais, validação de unidade e de
/// alimento, edição atômica, duplicação, exclusão só de rascunho e rota pública anônima.
/// </summary>
public class CardapioApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CardapioApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object Payload(
        string nome,
        int unidadeId,
        int nutricionistaId,
        string inicio,
        string fim,
        params (string Nome, int Ordem, (int AlimentoId, decimal Gramas)[] Itens)[] refeicoes) => new
        {
            nome,
            unidadeId,
            nutricionistaId,
            periodo = new { inicio, fim },
            refeicoes = refeicoes.Select(refeicao => new
            {
                nome = refeicao.Nome,
                ordem = refeicao.Ordem,
                itens = refeicao.Itens.Select(item => new
                {
                    alimentoId = item.AlimentoId,
                    gramas = item.Gramas
                })
            })
        };

    private static async Task<int> NutricionistaId(HttpClient client)
    {
        var lista = await client.GetFromJsonAsync<JsonElement>("/api/nutricionistas");
        return lista[0].GetProperty("id").GetInt32();
    }

    /// <summary>Cria um alimento com 130 kcal por 100 g e devolve o id.</summary>
    private static async Task<int> CriarAlimento(HttpClient client, string nome)
    {
        var categorias = await client.GetFromJsonAsync<JsonElement>("/api/categorias-alimento");
        var categoriaId = categorias[0].GetProperty("id").GetInt32();

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

    private static async Task<int> CriarCardapio(
        HttpClient client,
        int unidadeId,
        string nome,
        string inicio,
        string fim,
        params (string Nome, int Ordem, (int AlimentoId, decimal Gramas)[] Itens)[] refeicoes)
    {
        var nutricionistaId = await NutricionistaId(client);

        var resposta = await client.PostAsJsonAsync(
            "/api/cardapios",
            Payload(nome, unidadeId, nutricionistaId, inicio, fim, refeicoes));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Publica direto no banco: a transição pertence à Spec 015, então não há endpoint
    /// para isso nesta spec.
    /// </summary>
    private void Publicar(int cardapioId)
    {
        using var scope = _factory.Services.CreateScope();
        var cardapios = scope.ServiceProvider.GetRequiredService<ICardapioRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var cardapio = cardapios.ObterComRefeicoes(cardapioId)
            ?? throw new InvalidOperationException($"Cardápio {cardapioId} não existe.");

        PropriedadesPrivadas.Definir(cardapio, nameof(Cardapio.Situacao), SituacaoCardapio.Publicado);
        unidadeDeTrabalho.ExecutarEmTransacao(() => cardapios.Update(cardapio));
    }

    [Fact]
    public async Task Post_Cardapio_Retorna201ComTotaisCalculados()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");
        var feijao = await CriarAlimento(client, $"Feijão {Guid.NewGuid():N}");

        var resposta = await client.PostAsJsonAsync("/api/cardapios", Payload(
            "Semana 12",
            unidadeId,
            nutricionistaId,
            "2026-11-02",
            "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m), (feijao, 50m) })));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Rascunho", corpo.GetProperty("situacao").GetString());
        Assert.Equal(unidadeId, corpo.GetProperty("unidadeId").GetInt32());
        Assert.Equal("2026-11-02", corpo.GetProperty("periodo").GetProperty("inicio").GetString());

        var refeicao = Assert.Single(corpo.GetProperty("refeicoes").EnumerateArray());
        var itens = refeicao.GetProperty("itens").EnumerateArray().ToList();

        Assert.Equal(2, itens.Count);
        Assert.Equal(130m, itens[0].GetProperty("valores").GetProperty("caloriasKcal").GetDecimal());
        Assert.Equal(65m, itens[1].GetProperty("valores").GetProperty("caloriasKcal").GetDecimal());
        Assert.Equal(195m, refeicao.GetProperty("total").GetProperty("caloriasKcal").GetDecimal());
        Assert.Equal(195m, corpo.GetProperty("total").GetProperty("caloriasKcal").GetDecimal());
    }

    [Fact]
    public async Task Post_Cardapio_ComUnidadeInexistente_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");

        var nutricionistaId = await NutricionistaId(client);

        var resposta = await client.PostAsJsonAsync("/api/cardapios", Payload(
            "Semana 12",
            999999,
            nutricionistaId,
            "2026-11-02",
            "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) })));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Unidade", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Post_Cardapio_PorMerendeira_Retorna403()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var (nutricionista, nutricionistaId) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            escolaId: null,
            unidadeId: unidadeId);
        var arroz = await CriarAlimento(nutricionista, $"Arroz {Guid.NewGuid():N}");
        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId,
            unidadeId);

        var resposta = await merendeira.PostAsJsonAsync("/api/cardapios", Payload(
            "Semana 12",
            unidadeId,
            nutricionistaId,
            "2026-11-02",
            "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) })));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_CardapioInexistente_Retorna404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/cardapios/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Put_ComQuantidadeInvalida_NaoAlteraOCardapio()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(
            client, unidadeId, "Semana 12", "2026-11-02", "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        var invalido = await client.PutAsJsonAsync($"/api/cardapios/{cardapioId}", Payload(
            "Semana alterada",
            unidadeId,
            nutricionistaId,
            "2026-11-02",
            "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 0m) })));

        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);

        var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/cardapios/{cardapioId}");

        Assert.Equal("Semana 12", detalhe.GetProperty("nome").GetString());
        Assert.Equal(130m, detalhe.GetProperty("total").GetProperty("caloriasKcal").GetDecimal());
    }

    [Fact]
    public async Task Post_Duplicar_CopiaComposicaoComNovoPeriodo()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");

        var origemId = await CriarCardapio(
            client, unidadeId, "Semana 12", "2026-11-02", "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        var resposta = await client.PostAsJsonAsync(
            $"/api/cardapios/{origemId}/duplicar",
            new { nome = "Semana 13", periodo = new { inicio = "2026-11-09", fim = "2026-11-13" } });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.NotEqual(origemId, corpo.GetProperty("id").GetInt32());
        Assert.Equal("Rascunho", corpo.GetProperty("situacao").GetString());
        Assert.Equal(origemId, corpo.GetProperty("cardapioDuplicadoDeId").GetInt32());
        Assert.Equal("2026-11-09", corpo.GetProperty("periodo").GetProperty("inicio").GetString());
        Assert.Equal(130m, corpo.GetProperty("total").GetProperty("caloriasKcal").GetDecimal());
    }

    [Fact]
    public async Task Delete_Rascunho_Retorna204EDepoisNaoExiste()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(
            client, unidadeId, "Semana 12", "2026-11-02", "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        var exclusao = await client.DeleteAsync($"/api/cardapios/{cardapioId}");

        Assert.Equal(HttpStatusCode.NoContent, exclusao.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/cardapios/{cardapioId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_Publicado_Retorna409()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(
            client, unidadeId, "Semana 12", "2026-11-02", "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        Publicar(cardapioId);

        var resposta = await client.DeleteAsync($"/api/cardapios/{cardapioId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("publicado", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Get_Publicados_SemSessao_DevolveApenasPublicados()
    {
        var (nutricionista, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(nutricionista, $"Arroz {Guid.NewGuid():N}");

        var rascunho = await CriarCardapio(
            nutricionista, unidadeId, "Rascunho", "2026-12-01", "2026-12-05",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        var publicado = await CriarCardapio(
            nutricionista, unidadeId, "Publicado", "2026-12-01", "2026-12-05",
            ("Jantar", 2, new[] { (arroz, 200m) }));

        Publicar(publicado);

        var anonimo = _factory.CreateClient();
        var pagina = await anonimo.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/publicados?unidadeId={unidadeId}&data=2026-12-02");

        var itens = pagina.GetProperty("itens").EnumerateArray().ToList();

        Assert.Single(itens);
        Assert.Equal("Publicado", itens[0].GetProperty("nome").GetString());
        Assert.NotEqual(rascunho, itens[0].GetProperty("id").GetInt32());
        Assert.Equal(260m, itens[0].GetProperty("total").GetProperty("caloriasKcal").GetDecimal());
    }

    [Fact]
    public async Task Get_Publicados_SemUnidade_Retorna400()
    {
        var anonimo = _factory.CreateClient();

        var resposta = await anonimo.GetAsync("/api/cardapios/publicados");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("unidade", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Get_Publicados_FiltraPorRefeicaoSemDiferenciarCaixa()
    {
        var (nutricionista, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(nutricionista, $"Arroz {Guid.NewGuid():N}");

        var almoco = await CriarCardapio(
            nutricionista, unidadeId, "Com almoço", "2026-12-10", "2026-12-11",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        var jantar = await CriarCardapio(
            nutricionista, unidadeId, "Com jantar", "2026-12-10", "2026-12-11",
            ("Jantar", 1, new[] { (arroz, 100m) }));

        Publicar(almoco);
        Publicar(jantar);

        var anonimo = _factory.CreateClient();
        var pagina = await anonimo.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/publicados?unidadeId={unidadeId}&data=2026-12-10&refeicao=almoço");

        var itens = pagina.GetProperty("itens").EnumerateArray().ToList();

        Assert.Single(itens);
        Assert.Equal("Com almoço", itens[0].GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Get_Publicados_FiltroPorRefeicaoSemAcento_NaoEncontra()
    {
        var (nutricionista, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(nutricionista, $"Arroz {Guid.NewGuid():N}");

        var cardapio = await CriarCardapio(
            nutricionista, unidadeId, "Com almoço", "2026-12-20", "2026-12-21",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        Publicar(cardapio);

        var anonimo = _factory.CreateClient();

        var semAcento = await anonimo.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/publicados?unidadeId={unidadeId}&data=2026-12-20&refeicao=almoco");

        // RF10: a comparação ignora caixa, mas o acento continua significativo.
        Assert.Empty(semAcento.GetProperty("itens").EnumerateArray());

        var comAcento = await anonimo.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/publicados?unidadeId={unidadeId}&data=2026-12-20&refeicao=ALMOÇO");

        Assert.Single(comAcento.GetProperty("itens").EnumerateArray());
    }

    [Fact]
    public async Task Get_Cardapios_FiltraPorDataESituacaoNaListagemInterna()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var arroz = await CriarAlimento(client, $"Arroz {Guid.NewGuid():N}");

        var daSemana = await CriarCardapio(
            client, unidadeId, "Semana de novembro", "2026-11-02", "2026-11-06",
            ("Almoço", 1, new[] { (arroz, 100m) }));

        var publicado = await CriarCardapio(
            client, unidadeId, "Semana de dezembro", "2026-12-07", "2026-12-11",
            ("Jantar", 1, new[] { (arroz, 100m) }));

        Publicar(publicado);

        var porData = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios?unidadeId={unidadeId}&data=2026-11-04");

        Assert.Equal(daSemana, Assert.Single(porData.GetProperty("itens").EnumerateArray())
            .GetProperty("id").GetInt32());

        var porSituacao = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios?unidadeId={unidadeId}&situacao=Publicado");

        var publicados = porSituacao.GetProperty("itens").EnumerateArray().ToList();

        Assert.Equal(publicado, Assert.Single(publicados).GetProperty("id").GetInt32());
        Assert.Equal("Publicado", publicados[0].GetProperty("situacao").GetString());
    }

    [Fact]
    public async Task Get_Cardapios_ComSituacaoInvalida_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/cardapios?situacao=Encerrado");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Situação inválida", corpo.GetProperty("erro").GetString()!);
    }
}
