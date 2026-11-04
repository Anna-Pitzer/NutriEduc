using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Bootstrap v2 — token de ambiente (Spec 022 v2). O mesmo token cria um administrador
/// por unidade, uma única vez por unidade; a unidade é informada pelo nome exato.
/// </summary>
public class BootstrapApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public BootstrapApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static string EmailUnico() =>
        $"bootstrap.{Guid.NewGuid():N}@nutrieduc.local";

    private static string NomeUnico(string prefixo) =>
        $"{prefixo} {Guid.NewGuid():N}";

    private static object Payload(
        string email,
        string? unidade,
        string token = ApiFactory.TokenBootstrap) => new
    {
        token,
        unidade,
        nome = "Administrador Bootstrap",
        cpf = "12345678901",
        dataNascimento = "1990-01-01",
        email,
        senha = "senha12345"
    };

    [Fact]
    public async Task Status_ComTokenDeAmbiente_InformaDisponivel()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/bootstrap/status");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.True(corpo.GetProperty("disponivel").GetBoolean());
        Assert.Equal("ok", corpo.GetProperty("motivo").GetString());
    }

    [Fact]
    public async Task CriarAdmin_ComTokenValido_CriaAdminDaUnidadeEPermiteLogin()
    {
        var unidade = NomeUnico("Unidade");
        var (_, unidadeId) = _factory.CriarEscolaComUnidade(unidade);
        var email = EmailUnico();

        var client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

        var criacao = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(email, unidade));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/usuario/login",
            new { email, senha = "senha12345" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var sessao = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.OK, sessao.StatusCode);

        var corpo = await Ler(sessao);
        Assert.Equal("AdminSecretaria", corpo.GetProperty("papel").GetString());
        Assert.Equal(unidadeId, corpo.GetProperty("unidadeId").GetInt32());
    }

    [Fact]
    public async Task CriarAdmin_NaMesmaUnidadeDuasVezes_Retorna409()
    {
        var unidade = NomeUnico("Unidade repetida");
        _factory.CriarEscolaComUnidade(unidade);
        var client = _factory.CreateClient();

        var primeiro = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), unidade));
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), unidade));

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);

        var corpo = await Ler(segundo);
        Assert.Contains("já usou", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task CriarAdmin_ComMesmoTokenEmOutraUnidade_Cria()
    {
        var primeira = NomeUnico("Unidade A");
        var segunda = NomeUnico("Unidade B");
        _factory.CriarEscolaComUnidade(primeira);
        _factory.CriarEscolaComUnidade(segunda);
        var client = _factory.CreateClient();

        var a = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), primeira));
        var b = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), segunda));

        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(HttpStatusCode.Created, b.StatusCode);
    }

    [Fact]
    public async Task CriarAdmin_ComUnidadeNova_CriaUnidadeJuntoComOAdmin()
    {
        var client = _factory.CreateClient();
        var nomeUnidade = NomeUnico("Unidade Nova");

        var resposta = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), nomeUnidade));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        // A unidade nasce no bootstrap e fica visível apenas para o administrador dela.
        using var scope = _factory.Services.CreateScope();
        var escolas = scope.ServiceProvider.GetRequiredService<IEscolaRepository>();

        Assert.Contains(
            escolas.ListarUnidades(),
            unidade => unidade.Nome == nomeUnidade);
    }

    [Fact]
    public async Task CriarAdmin_ComUnidadeDesativada_Retorna400()
    {
        var nome = NomeUnico("Unidade desativada");
        var (_, unidadeId) = _factory.CriarEscolaComUnidade(nome);

        using (var scope = _factory.Services.CreateScope())
        {
            var escolas = scope.ServiceProvider.GetRequiredService<IEscolaRepository>();
            var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var unidade = escolas.GetUnidade(unidadeId)!;
            unidade.Desativar();
            unidadeDeTrabalho.ExecutarEmTransacao(() => escolas.UpdateUnidade(unidade));
        }

        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            "/api/bootstrap/admin", Payload(EmailUnico(), nome));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarAdmin_SemUnidadeComTokenDeAmbiente_Retorna400()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), null));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.Contains("unidade", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task CriarAdmin_ComTokenInvalido_Retorna403()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(EmailUnico(), "Qualquer", token: "token-errado"));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.Contains("Token inválido", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task CriarAdmin_ComEmailRepetido_Retorna409()
    {
        var primeira = NomeUnico("Unidade C");
        var segunda = NomeUnico("Unidade D");
        _factory.CriarEscolaComUnidade(primeira);
        _factory.CriarEscolaComUnidade(segunda);

        var client = _factory.CreateClient();
        var email = EmailUnico();

        var primeiro = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(email, primeira));
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await client.PostAsJsonAsync(
            "/api/bootstrap/admin",
            Payload(email, segunda));

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task CriarAdmin_SemDataNascimento_Retorna400()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/bootstrap/admin", new
        {
            token = ApiFactory.TokenBootstrap,
            unidade = NomeUnico("Unidade sem data"),
            nome = "Administrador Bootstrap",
            cpf = "12345678901",
            email = EmailUnico(),
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarAdmin_SemToken_Retorna400DeValidacao()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/bootstrap/admin", new
        {
            token = "",
            unidade = NomeUnico("Unidade sem token"),
            nome = "Administrador Bootstrap",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email = EmailUnico(),
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}

/// <summary>Sem token de ambiente e sem token temporário: a rota se comporta como inexistente.</summary>
public class BootstrapSemTokenApiTests : IClassFixture<ApiFactorySemBootstrap>
{
    private readonly ApiFactorySemBootstrap _factory;

    public BootstrapSemTokenApiTests(ApiFactorySemBootstrap factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Status_SemToken_InformaIndisponivel()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/bootstrap/status");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var corpo = documento.RootElement;

        Assert.False(corpo.GetProperty("disponivel").GetBoolean());
        Assert.Equal("sem-token", corpo.GetProperty("motivo").GetString());
    }

    [Fact]
    public async Task CriarAdmin_SemTokenConfigurado_Retorna404()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/bootstrap/admin", new
        {
            token = "qualquer-token-com-mais-de-32-caracteres-aqui",
            unidade = "A",
            nome = "Administrador Bootstrap",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email = $"bootstrap.{Guid.NewGuid():N}@nutrieduc.local",
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}

/// <summary>Trava dura de ambiente: nega mesmo com token válido.</summary>
public class BootstrapNegadoApiTests : IClassFixture<ApiFactoryBootstrapNegado>
{
    private readonly ApiFactoryBootstrapNegado _factory;

    public BootstrapNegadoApiTests(ApiFactoryBootstrapNegado factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Status_ComCriacaoNegada_InformaMotivo()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/bootstrap/status");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var corpo = documento.RootElement;

        Assert.False(corpo.GetProperty("disponivel").GetBoolean());
        Assert.Equal("desativado-ambiente", corpo.GetProperty("motivo").GetString());
    }

    [Fact]
    public async Task CriarAdmin_ComCriacaoNegadaPorAmbiente_Retorna403()
    {
        var client = _factory.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/bootstrap/admin", new
        {
            token = ApiFactory.TokenBootstrap,
            unidade = "Unidade qualquer",
            nome = "Administrador Bootstrap",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email = $"bootstrap.{Guid.NewGuid():N}@nutrieduc.local",
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
