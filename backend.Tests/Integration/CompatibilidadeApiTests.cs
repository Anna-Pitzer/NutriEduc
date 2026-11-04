using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP da compatibilidade (Spec 014): bloqueante por alergia, alerta por restrição,
/// cardápio inexistente e escopo por papel.
/// </summary>
public class CompatibilidadeApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CompatibilidadeApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CriarCardapio(HttpClient client, int unidadeId, int alimentoId)
    {
        var resposta = await client.PostAsJsonAsync("/api/cardapios", new
        {
            nome = "Semana 12",
            unidadeId,
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

    private async Task<int> CriarAlimentoComVinculo(HttpClient client, int restricaoId, string nome)
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
            restricaoIds = new[] { restricaoId }
        });

        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
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

    private int ObterRestricaoId(string nome)
    {
        using var scope = _factory.Services.CreateScope();
        var restricoes = scope.ServiceProvider.GetRequiredService<IRestricaoRepository>();
        return restricoes.Listar(null, false).First(restricao => restricao.Nome == nome).Id;
    }

    [Fact]
    public async Task Get_Compatibilidade_ComAlergenico_DevolveBloqueante()
    {
        _factory.GarantirCatalogo();
        var client = await _factory.CreateAuthenticatedClientAsync();
        var unidadeId = _factory.CriarEscolaComUnidade().UnidadeId;
        var lactoseId = ObterRestricaoId("Lactose");
        var nomeIogurte = $"Iogurte {Guid.NewGuid():N}";
        var iogurte = await CriarAlimentoComVinculo(client, lactoseId, nomeIogurte);

        CriarAlunoComRestricao(unidadeId, "Lactose");

        var cardapioId = await CriarCardapio(client, unidadeId, iogurte);

        var resposta = await client.GetAsync($"/api/cardapios/{cardapioId}/compatibilidade");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(corpo.GetProperty("compativel").GetBoolean());
        Assert.Equal(1, corpo.GetProperty("totalAlunosAnalisados").GetInt32());

        var achado = Assert.Single(corpo.GetProperty("bloqueantes").EnumerateArray());
        Assert.Equal("Bloqueante", achado.GetProperty("severidade").GetString());
        Assert.Equal("Lactose", achado.GetProperty("restricao").GetString());
        Assert.Contains(nomeIogurte, corpo.GetProperty("alimentosNaoPermitidos").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Get_Compatibilidade_ComRestricao_DevolveAlertaNaoBloqueante()
    {
        _factory.GarantirCatalogo();
        var client = await _factory.CreateAuthenticatedClientAsync();
        var unidadeId = _factory.CriarEscolaComUnidade().UnidadeId;
        var glutenId = ObterRestricaoId("Gluten");
        var pao = await CriarAlimentoComVinculo(client, glutenId, $"Pão {Guid.NewGuid():N}");

        CriarAlunoComRestricao(unidadeId, "Gluten");

        var cardapioId = await CriarCardapio(client, unidadeId, pao);

        var resposta = await client.GetAsync($"/api/cardapios/{cardapioId}/compatibilidade");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(corpo.GetProperty("compativel").GetBoolean());
        Assert.Empty(corpo.GetProperty("bloqueantes").EnumerateArray());
        Assert.Single(corpo.GetProperty("alertas").EnumerateArray());
    }

    [Fact]
    public async Task Get_Compatibilidade_CardapioInexistente_Retorna404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/cardapios/999999/compatibilidade");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Compatibilidade_PorResponsavel_Retorna403()
    {
        _factory.GarantirCatalogo();
        var nutricionista = await _factory.CreateAuthenticatedClientAsync();
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var iogurte = await CriarAlimentoComVinculo(
            nutricionista, ObterRestricaoId("Lactose"), $"Iogurte {Guid.NewGuid():N}");

        var cardapioId = await CriarCardapio(nutricionista, unidadeId, iogurte);

        var responsavel = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Responsavel, unidadeId: unidadeId);

        var resposta = await responsavel.GetAsync($"/api/cardapios/{cardapioId}/compatibilidade");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
