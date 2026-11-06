using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Fluxo HTTP da Spec 029: criar o geral com adaptação automática, consultar o contexto de
/// restrições, listar o par, readaptar sem duplicar e servir a versão publicada ao aluno.
/// </summary>
public class CardapioAdaptadoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CardapioAdaptadoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private int ObterRestricaoId(string nome)
    {
        using var scope = _factory.Services.CreateScope();
        var restricoes = scope.ServiceProvider.GetRequiredService<IRestricaoRepository>();
        return restricoes.Listar(null, false).First(restricao => restricao.Nome == nome).Id;
    }

    private async Task<int> CriarAlimento(
        HttpClient client,
        string nome,
        IEnumerable<int>? restricaoIds = null)
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
            restricaoIds = restricaoIds ?? Array.Empty<int>()
        });

        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    private int CriarAlunoComRestricao(int unidadeId, int escolaId, string restricao)
    {
        using var scope = _factory.Services.CreateScope();
        var alunos = scope.ServiceProvider.GetRequiredService<IAlunoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var aluno = new Aluno(
            $"Aluno {Guid.NewGuid():N}",
            new DateOnly(2015, 5, 20),
            new Turma("Escola", "5 ano"),
            null,
            null,
            new[] { restricao },
            false,
            new Responsavel("Responsável", "11988888888"),
            escolaId);

        aluno.DefinirUnidade(unidadeId);

        unidadeDeTrabalho.ExecutarEmTransacao(() => alunos.Add(aluno));

        return aluno.Id;
    }

    private static object Payload(
        int unidadeId,
        int nutricionistaId,
        int alimentoId) => new
        {
            nome = "Semana 12",
            unidadeId,
            nutricionistaId,
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
        };

    [Fact]
    public async Task FluxoCompleto_CriaParListaServeEReadaptaSemDuplicar()
    {
        var (client, nutricionistaId, unidadeId) = await _factory.CriarNutricionistaComUnidadeAsync();
        var escolaId = _factory.CriarEscolaNaUnidade(unidadeId);
        var lactoseId = ObterRestricaoId("Lactose");

        var leite = await CriarAlimento(client, $"Leite {Guid.NewGuid():N}", new[] { lactoseId });
        await CriarAlimento(client, $"Bebida {Guid.NewGuid():N}");
        var alunoId = CriarAlunoComRestricao(unidadeId, escolaId, "Lactose");

        var criacao = await client.PostAsJsonAsync(
            "/api/cardapios",
            Payload(unidadeId, nutricionistaId, leite));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var geral = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var geralId = geral.GetProperty("id").GetInt32();

        Assert.Equal("Geral", geral.GetProperty("tipo").GetString());
        Assert.False(geral.GetProperty("semRestricoes").GetBoolean());
        Assert.Single(geral.GetProperty("confirmacoes").EnumerateArray());

        var adaptados = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/{geralId}/adaptados");

        var adaptado = Assert.Single(adaptados.EnumerateArray());
        var adaptadoId = adaptado.GetProperty("id").GetInt32();

        Assert.Equal(escolaId, adaptado.GetProperty("escolaId").GetInt32());
        Assert.Equal(lactoseId.ToString(), adaptado.GetProperty("conjuntoRestricoesChave").GetString());
        Assert.Equal("Atualizado", adaptado.GetProperty("statusAdaptacao").GetString());
        Assert.Equal(1, adaptado.GetProperty("totalSubstituicoes").GetInt32());

        var contexto = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/contexto-restricoes?unidadeId={unidadeId}&escolaId={escolaId}");

        var escolaDoContexto = Assert.Single(contexto.GetProperty("escolas").EnumerateArray());
        Assert.False(escolaDoContexto.GetProperty("semRestricoes").GetBoolean());
        Assert.Single(escolaDoContexto.GetProperty("alunos").EnumerateArray());

        var indisponivel = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/cardapio?data=2026-11-02");

        Assert.False(indisponivel.GetProperty("disponivel").GetBoolean());
        Assert.True(indisponivel.GetProperty("alerta").GetBoolean());

        // Readaptar antes de publicar: a versão publicada só muda pelo fluxo de revisão.
        var readaptar = await client.PostAsJsonAsync(
            $"/api/cardapios/{geralId}/adaptar",
            new { escolaId, restricaoIds = new[] { lactoseId } });

        Assert.Equal(HttpStatusCode.OK, readaptar.StatusCode);

        var depois = await client.GetFromJsonAsync<JsonElement>(
            $"/api/cardapios/{geralId}/adaptados");

        Assert.Single(depois.EnumerateArray());

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/cardapios/{geralId}/publicar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/cardapios/{adaptadoId}/publicar", null)).StatusCode);

        var servido = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/cardapio?data=2026-11-02");

        Assert.True(servido.GetProperty("disponivel").GetBoolean());
        Assert.Equal(adaptadoId, servido.GetProperty("cardapio").GetProperty("id").GetInt32());
        Assert.Equal("Adaptado", servido.GetProperty("cardapio").GetProperty("tipo").GetString());
    }
}
