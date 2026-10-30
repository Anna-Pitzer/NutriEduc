using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Backend.Tests.Integration;

public class AlunoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AlunoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object Payload(string nome) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Escola Teste",
        serie = "3o ano",
        telefone = "11999999999",
        observacao = "",
        restricoes = new[] { "Lactose" },
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888"
    };

    private async Task<int> CriarAluno(HttpClient client, string nome)
    {
        var resposta = await client.PostAsJsonAsync("/api/aluno/registrar", Payload(nome));
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var alunos = await client.GetFromJsonAsync<List<JsonElement>>("/api/aluno");
        Assert.NotNull(alunos);

        var aluno = alunos!.Single(a => a.GetProperty("nome").GetString() == nome);
        return aluno.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Post_ComDadosValidos_Retorna201ComLocation()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.PostAsJsonAsync("/api/aluno/registrar", Payload("Ana Cadastro"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var id = corpo.RootElement.GetProperty("id").GetInt32();
        Assert.Equal($"/api/aluno/perfil/{id}", resposta.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_ComDadosInvalidos_Retorna400ComErro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.PostAsJsonAsync("/api/aluno/registrar", Payload(""));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(corpo.RootElement.TryGetProperty("erro", out var erro));
        Assert.False(string.IsNullOrWhiteSpace(erro.GetString()));
    }

    [Fact]
    public async Task Get_ListaDevolveAlunoCadastrado()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        await CriarAluno(client, "Beto Lista");

        var alunos = await client.GetFromJsonAsync<List<JsonElement>>("/api/aluno");

        Assert.NotNull(alunos);
        Assert.Contains(alunos!, a => a.GetProperty("nome").GetString() == "Beto Lista");

        var beto = alunos!.Single(a => a.GetProperty("nome").GetString() == "Beto Lista");
        Assert.Contains(beto.GetProperty("restricoes").EnumerateArray(),
                        r => r.GetString() == "Lactose");
    }

    [Fact]
    public async Task Put_ComDadosValidos_PersisteAlteracao()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var id = await CriarAluno(client, "Carla Antes");

        var resposta = await client.PutAsJsonAsync($"/api/aluno/{id}", Payload("Carla Depois"));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var atualizado = await client.GetFromJsonAsync<JsonElement>($"/api/aluno/perfil/{id}");
        Assert.Equal("Carla Depois", atualizado.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Put_ComIdInexistente_Retorna404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.PutAsJsonAsync("/api/aluno/999999", Payload("Fantasma"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(corpo.RootElement.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Delete_ComIdInexistente_Retorna404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.DeleteAsync("/api/aluno/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Endpoints_SemSessao_Retornam401SemDados()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/aluno/registrar", Payload("Anonimo"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/aluno")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/aluno/perfil/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/api/aluno/1", Payload("Anonimo"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.DeleteAsync("/api/aluno/1")).StatusCode);

        var respostaLista = await client.GetAsync("/api/aluno");
        var corpo = await respostaLista.Content.ReadAsStringAsync();
        Assert.DoesNotContain("nome", corpo, StringComparison.OrdinalIgnoreCase);
    }
}
