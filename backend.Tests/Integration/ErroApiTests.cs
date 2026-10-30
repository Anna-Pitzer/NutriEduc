using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Verifica o contrato único de erro `{ "erro": "..." }` (ADR-005 e Spec 007).
/// </summary>
public class ErroApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ErroApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object PayloadAluno(string nome) => new
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

    private static async Task<JsonElement> LerCorpo(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    [Fact]
    public async Task Aluno_Invalido_Retorna400ComErro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.PostAsJsonAsync("/api/aluno/registrar", PayloadAluno(""));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Aluno_Inexistente_Retorna404ComErro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/aluno/perfil/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Usuario_PerfilSemSessao_Retorna401ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/usuario/perfil/999999");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Login_Invalido_Retorna401ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/usuario/login", new
        {
            email = "nao.existe@nutrieduc.local",
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Aluno_SemSessao_Retorna401ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/aluno");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task ExcecaoDeConflito_Retorna409ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/diagnostico/conflito");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        var corpo = await LerCorpo(resposta);
        Assert.Equal("Conflito de teste.", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task ExcecaoNaoTratada_Retorna500SemStackTrace()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/diagnostico/erro");

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);

        var texto = await resposta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);
        Assert.Equal("Erro interno.", documento.RootElement.GetProperty("erro").GetString());
        Assert.DoesNotContain("Exception", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", texto, StringComparison.OrdinalIgnoreCase);
    }
}
