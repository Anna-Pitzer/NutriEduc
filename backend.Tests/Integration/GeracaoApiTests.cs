using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP da geração automática (Spec 013): parâmetros por unidade, geração
/// determinística e escopo exclusivo de Nutricionista.
/// </summary>
public class GeracaoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public GeracaoApiTests(ApiFactory factory)
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

    private static async Task<int> CriarAlimento(
        HttpClient client,
        int categoriaId,
        string nome,
        decimal calorias)
    {
        var resposta = await client.PostAsJsonAsync("/api/alimentos", new
        {
            nome,
            categoriaId,
            valores = new
            {
                caloriasKcal = calorias,
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

    private static object PayloadParametros(int opcoes = 2) => new
    {
        caloriasMinimasRefeicao = 0m,
        caloriasMaximasRefeicao = 500m,
        opcoesPorRefeicao = opcoes,
        janelaRepeticaoDias = 0,
        excluirAlimentosIncompativeis = false,
        categoriasObrigatorias = Array.Empty<int>(),
        refeicoesPadrao = new[] { "Almoço" }
    };

    private static async Task<int> CriarAlimentoComVinculo(
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

    private static async Task<int> NutricionistaId(HttpClient client)
    {
        var lista = await client.GetFromJsonAsync<JsonElement>("/api/nutricionistas");
        return lista[0].GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarCardapio(
        HttpClient client,
        int unidadeId,
        int alimentoId,
        string inicio,
        string fim)
    {
        var resposta = await client.PostAsJsonAsync("/api/cardapios", new
        {
            nome = "Semana 12",
            unidadeId,
            nutricionistaId = await NutricionistaId(client),
            periodo = new { inicio, fim },
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

    [Fact]
    public async Task Gerar_ComParametrosValidos_RetornaCardapioDeterministico()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var categoriaId = await CriarCategoria(client, $"Categoria {Guid.NewGuid():N}");

        await CriarAlimento(client, categoriaId, $"Arroz {Guid.NewGuid():N}", 130m);
        await CriarAlimento(client, categoriaId, $"Feijão {Guid.NewGuid():N}", 200m);
        await CriarAlimento(client, categoriaId, $"Carne {Guid.NewGuid():N}", 300m);

        var parametros = await client.PutAsJsonAsync(
            $"/api/unidades/{unidadeId}/parametros-geracao",
            PayloadParametros());

        Assert.Equal(HttpStatusCode.OK, parametros.StatusCode);

        var corpoGerado = await Gerar(client, unidadeId, nutricionistaId, seed: 42);
        var novamente = await Gerar(client, unidadeId, nutricionistaId, seed: 42);

        Assert.Equal(corpoGerado.GetProperty("seed").GetInt32(), novamente.GetProperty("seed").GetInt32());
        Assert.Equal(
            corpoGerado.GetProperty("totais").GetRawText(),
            novamente.GetProperty("totais").GetRawText());

        // Regra: o cardápio gerado registra a nutricionista autora (snapshot da criação).
        var cardapioId = corpoGerado.GetProperty("cardapioId").GetInt32();
        var detalhe = await client.GetFromJsonAsync<JsonElement>($"/api/cardapios/{cardapioId}");

        Assert.False(string.IsNullOrWhiteSpace(
            detalhe.GetProperty("nutricionista").GetProperty("nome").GetString()));
    }

    [Fact]
    public async Task Gerar_SemParametros_Retorna400()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();

        var resposta = await client.PostAsJsonAsync("/api/cardapios/gerar", new
        {
            unidadeId,
            nutricionistaId,
            nome = "Semana 12",
            periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
            seed = 42
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Gerar_PorAdminEscola_Retorna403()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId, null);

        var resposta = await adminEscola.PostAsJsonAsync("/api/cardapios/gerar", new
        {
            unidadeId,
            nome = "Semana 12",
            periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
            seed = 42
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    private static async Task<JsonElement> Gerar(
        HttpClient client,
        int unidadeId,
        int nutricionistaId,
        int seed)
    {
        var resposta = await client.PostAsJsonAsync("/api/cardapios/gerar", new
        {
            unidadeId,
            nutricionistaId,
            nome = $"Semana {Guid.NewGuid():N}",
            periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
            seed
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }
}
