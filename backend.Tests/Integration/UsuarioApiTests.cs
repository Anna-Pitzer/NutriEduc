using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Backend.Tests.Integration;

public class UsuarioApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UsuarioApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string EmailUnico() => $"teste.{Guid.NewGuid():N}@nutrieduc.local";

    private static object Payload(string email, string senha = "senha12345") => new
    {
        nome = "Usuario Teste",
        cpf = "12345678901",
        dataNascimento = "1990-01-01",
        email,
        senha,
        telefone = (string?)null,
        foto = (string?)null
    };

    [Fact]
    public async Task Post_ComDadosValidos_Retorna201ComLocation()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", Payload(EmailUnico()));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        Assert.StartsWith("/api/usuario/perfil/", resposta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_ComEmailDuplicado_Retorna400ComErro()
    {
        var client = _factory.CreateClient();
        var email = EmailUnico();

        await client.PostAsJsonAsync("/api/usuario/registrar", Payload(email));
        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", Payload(email));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(corpo.RootElement.TryGetProperty("erro", out var erro));
        Assert.Contains("E-mail", erro.GetString());
    }

    [Fact]
    public async Task Post_ComSenhaCurta_Retorna400ComErro()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            "/api/usuario/registrar", Payload(EmailUnico(), "curta"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.True(corpo.RootElement.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_EmiteCookieDeSessao()
    {
        var email = EmailUnico();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        await client.PostAsJsonAsync("/api/usuario/registrar", Payload(email));

        var resposta = await client.PostAsJsonAsync("/api/usuario/login", new { email, senha = "senha12345" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(resposta.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.Contains("NutriEduc.Auth"));

        var sessao = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.OK, sessao.StatusCode);
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_Retorna401()
    {
        var email = EmailUnico();
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/usuario/registrar", Payload(email));

        var resposta = await client.PostAsJsonAsync("/api/usuario/login", new { email, senha = "senhaerrada" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_Me_SemSessao_Retorna401()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/usuario/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
