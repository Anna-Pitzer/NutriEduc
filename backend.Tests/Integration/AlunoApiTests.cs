using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Backend.Tests.Integration;

public class AlunoApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AlunoApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
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

    private async Task<int> CriarAluno(string nome)
    {
        var resposta = await _client.PostAsJsonAsync("/api/aluno/registrar", Payload(nome));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var alunos = await _client.GetFromJsonAsync<List<JsonElement>>("/api/aluno");
        Assert.NotNull(alunos);

        var aluno = alunos!.Single(a => a.GetProperty("nome").GetString() == nome);
        return aluno.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Post_ComDadosValidos_Retorna200()
    {
        var resposta = await _client.PostAsJsonAsync("/api/aluno/registrar", Payload("Ana Cadastro"));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_ComDadosInvalidos_Retorna400ComErro()
    {
        var resposta = await _client.PostAsJsonAsync("/api/aluno/registrar", Payload(""));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(corpo.RootElement.TryGetProperty("erro", out var erro));
        Assert.False(string.IsNullOrWhiteSpace(erro.GetString()));
    }

    [Fact]
    public async Task Get_ListaDevolveAlunoCadastrado()
    {
        await CriarAluno("Beto Lista");

        var alunos = await _client.GetFromJsonAsync<List<JsonElement>>("/api/aluno");

        Assert.NotNull(alunos);
        Assert.Contains(alunos!, a => a.GetProperty("nome").GetString() == "Beto Lista");

        var beto = alunos!.Single(a => a.GetProperty("nome").GetString() == "Beto Lista");
        Assert.Contains(beto.GetProperty("restricoes").EnumerateArray(),
                        r => r.GetString() == "Lactose");
    }

    [Fact]
    public async Task Put_ComDadosValidos_PersisteAlteracao()
    {
        var id = await CriarAluno("Carla Antes");

        var resposta = await _client.PutAsJsonAsync($"/api/aluno/{id}", Payload("Carla Depois"));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var atualizado = await _client.GetFromJsonAsync<JsonElement>($"/api/aluno/perfil/{id}");
        Assert.Equal("Carla Depois", atualizado.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task Put_ComIdInexistente_Retorna404()
    {
        var resposta = await _client.PutAsJsonAsync("/api/aluno/999999", Payload("Fantasma"));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(corpo.RootElement.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Delete_ComIdInexistente_Retorna404()
    {
        var resposta = await _client.DeleteAsync("/api/aluno/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
