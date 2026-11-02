using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Verifica o padrão de contrato das rotas (Spec 020): criação devolve 201 com
/// `Location` e objeto, o perfil exige sessão e dono, e todo erro sob `/api/`
/// responde `{ "erro": "mensagem" }` — inclusive modelo inválido, JSON malformado,
/// verbo não permitido, content-type não suportado e rota inexistente.
/// </summary>
public class ContratoRotasTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ContratoRotasTests(ApiFactory factory)
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

    private static object PayloadUsuario(string email) => new
    {
        nome = "Usuario Contrato",
        cpf = "12345678901",
        dataNascimento = "1990-01-01",
        email,
        senha = "senha12345",
        telefone = (string?)null,
        foto = (string?)null
    };

    private static async Task<JsonElement> LerCorpo(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static async Task<int> IdDaSessao(HttpClient client)
    {
        using var documento = JsonDocument.Parse(await client.GetStringAsync("/api/usuario/me"));
        return documento.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Post_Aluno_Retorna201ComLocationECorpo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.PostAsJsonAsync("/api/aluno/registrar", PayloadAluno("Aluno Contrato"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        var id = corpo.GetProperty("id").GetInt32();

        Assert.True(id > 0);
        Assert.Equal("Aluno Contrato", corpo.GetProperty("nome").GetString());
        Assert.Equal($"/api/aluno/perfil/{id}", resposta.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_Usuario_Retorna201ComLocationESemSenha()
    {
        var client = _factory.CreateClient();
        var email = $"contrato.{Guid.NewGuid():N}@nutrieduc.local";

        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", PayloadUsuario(email));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        var id = corpo.GetProperty("id").GetInt32();

        Assert.Equal(email, corpo.GetProperty("email").GetString());
        Assert.Equal($"/api/usuario/perfil/{id}", resposta.Headers.Location?.ToString());
        Assert.False(corpo.TryGetProperty("senha", out _));
    }

    [Fact]
    public async Task Perfil_SemSessao_Retorna401ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/usuario/perfil/1");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Perfil_DoProprioUsuario_Retorna200SemCpfNemSenha()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var id = await IdDaSessao(client);

        var resposta = await client.GetAsync($"/api/usuario/perfil/{id}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);

        Assert.Equal(id, corpo.GetProperty("id").GetInt32());
        Assert.True(corpo.TryGetProperty("nome", out _));
        Assert.True(corpo.TryGetProperty("email", out _));
        Assert.True(corpo.TryGetProperty("telefone", out _));
        Assert.True(corpo.TryGetProperty("foto", out _));
        Assert.False(corpo.TryGetProperty("cpf", out _));
        Assert.False(corpo.TryGetProperty("senha", out _));
    }

    [Fact]
    public async Task Perfil_DeOutroUsuario_Retorna403ComErro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var id = await IdDaSessao(client);

        var resposta = await client.GetAsync($"/api/usuario/perfil/{id + 1}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Put_Perfil_ComNomeVazio_Retorna400ComErroEmPortugues()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var id = await IdDaSessao(client);

        var resposta = await client.PutAsJsonAsync($"/api/usuario/perfil/{id}", new
        {
            nome = "",
            email = $"valido.{Guid.NewGuid():N}@nutrieduc.local"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        var erro = corpo.GetProperty("erro").GetString();

        Assert.False(string.IsNullOrWhiteSpace(erro));
        Assert.Contains("nome", erro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ComJsonMalformado_Retorna400RequisicaoInvalida()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsync(
            "/api/usuario/login",
            new StringContent("{\"email\":", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        Assert.Equal("Requisição inválida.", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task VerboNaoPermitido_Retorna405ComErro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/aluno"));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        Assert.Equal("Método não permitido.", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task ContentTypeNaoSuportado_Retorna415ComErro()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.PostAsync(
            "/api/aluno/registrar",
            new StringContent("texto", Encoding.UTF8, "text/plain"));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, resposta.StatusCode);

        var texto = await resposta.Content.ReadAsStringAsync();
        using var documento = JsonDocument.Parse(texto);

        Assert.True(documento.RootElement.TryGetProperty("erro", out var erro),
            $"Corpo inesperado para 415: {texto}");
        Assert.Equal("Formato de conteúdo não suportado.", erro.GetString());
    }

    [Fact]
    public async Task RotaInexistente_Retorna404ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/rota-que-nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        var corpo = await LerCorpo(resposta);
        Assert.Equal("Recurso não encontrado.", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task Delete_AlunoExistente_Retorna204SemCorpo()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var criado = await client.PostAsJsonAsync("/api/aluno/registrar", PayloadAluno("Aluno Excluido"));
        var id = (await LerCorpo(criado)).GetProperty("id").GetInt32();

        var resposta = await client.DeleteAsync($"/api/aluno/{id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(string.Empty, await resposta.Content.ReadAsStringAsync());
    }
}
