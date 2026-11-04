using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Database;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Token temporário do painel (Spec 022 v2): 30 minutos, uso único, preso à unidade de
/// quem gerou. Cada teste cria a própria fábrica para isolar o relógio falso — a
/// expiração é exercitada avançando o tempo, não esperando.
/// </summary>
public class BootstrapTokenApiTests
{
    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static string EmailUnico() =>
        $"token.{Guid.NewGuid():N}@nutrieduc.local";

    private static HttpClient ClienteComCookie(ApiFactory fabrica) =>
        fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

    private static object AdminPayload(string token, string email) => new
    {
        token,
        nome = "Administrador por token",
        cpf = "12345678901",
        dataNascimento = "1990-01-01",
        email,
        senha = "senha12345"
    };

    [Fact]
    public async Task GerarToken_ComoSecretariaDaUnidade_RetornaTokenDeTrintaMinutos()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var resposta = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await Ler(resposta);
        var token = corpo.GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(token!.Length >= 32);

        var expiraEm = corpo.GetProperty("expiraEm").GetDateTime();
        var agora = fabrica.RelogioFalso.GetUtcNow().UtcDateTime;

        Assert.Equal(30, (expiraEm - agora).TotalMinutes);
    }

    [Fact]
    public async Task GerarToken_ComTokenAtivo_Retorna409()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var primeiro = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task ConsultarToken_SemToken_InformaInativo()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var resposta = await secretaria.GetAsync("/api/bootstrap/token");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.False(corpo.GetProperty("ativo").GetBoolean());
    }

    [Fact]
    public async Task ConsultarToken_ComTokenAtivo_InformaExpiracao()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });

        var resposta = await secretaria.GetAsync("/api/bootstrap/token");
        var corpo = await Ler(resposta);

        Assert.True(corpo.GetProperty("ativo").GetBoolean());
        Assert.True(corpo.GetProperty("expiraEm").GetDateTime() >
                    fabrica.RelogioFalso.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task CriarAdmin_ComTokenTemporario_CriaNaUnidadeDeQuemGerou()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var geracao = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });
        var token = (await Ler(geracao)).GetProperty("token").GetString()!;

        var email = EmailUnico();
        var cliente = ClienteComCookie(fabrica);

        var criacao = await cliente.PostAsJsonAsync(
            "/api/bootstrap/admin",
            AdminPayload(token, email));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var login = await cliente.PostAsJsonAsync(
            "/api/usuario/login",
            new { email, senha = "senha12345" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var sessao = await cliente.GetAsync("/api/usuario/me");
        var corpo = await Ler(sessao);

        Assert.Equal("AdminSecretaria", corpo.GetProperty("papel").GetString());
        Assert.Equal(unidadeId, corpo.GetProperty("unidadeId").GetInt32());
    }

    [Fact]
    public async Task CriarAdmin_ComTokenTemporarioJaUsado_Retorna403()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var geracao = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });
        var token = (await Ler(geracao)).GetProperty("token").GetString()!;
        var cliente = fabrica.CreateClient();

        var primeiro = await cliente.PostAsJsonAsync(
            "/api/bootstrap/admin",
            AdminPayload(token, EmailUnico()));
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await cliente.PostAsJsonAsync(
            "/api/bootstrap/admin",
            AdminPayload(token, EmailUnico()));

        Assert.Equal(HttpStatusCode.Forbidden, segundo.StatusCode);
    }

    [Fact]
    public async Task CriarAdmin_ComTokenTemporarioExpirado_Retorna403EOLiberaNovo()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var secretaria = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var geracao = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });
        var token = (await Ler(geracao)).GetProperty("token").GetString()!;

        fabrica.RelogioFalso.Avancar(TimeSpan.FromMinutes(31));

        var cliente = fabrica.CreateClient();
        var criacao = await cliente.PostAsJsonAsync(
            "/api/bootstrap/admin",
            AdminPayload(token, EmailUnico()));

        Assert.Equal(HttpStatusCode.Forbidden, criacao.StatusCode);

        // O painel volta a permitir gerar: o botão deixa de ficar bloqueado.
        var consulta = await secretaria.GetAsync("/api/bootstrap/token");
        var estado = await Ler(consulta);
        Assert.False(estado.GetProperty("ativo").GetBoolean());

        var novaGeracao = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });
        Assert.Equal(HttpStatusCode.Created, novaGeracao.StatusCode);
    }

    [Fact]
    public async Task GerarToken_SemSessao_Retorna401()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var cliente = fabrica.CreateClient();

        var resposta = await cliente.PostAsJsonAsync("/api/bootstrap/token", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task GerarToken_ComPapelNaoAdministrativo_Retorna403()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var nutricionista = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Nutricionista);

        var resposta = await nutricionista.PostAsJsonAsync("/api/bootstrap/token", new { });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task GerarToken_SemUnidadeNaConta_Retorna403()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (_, email) = await fabrica.RegistrarAsync();
        fabrica.DefinirPapel(email, PapelUsuario.AdminSecretaria, unidadeId: fabrica.UnidadeIdPadrao);

        // Simula registro legado gravado antes da Spec 027, sem vínculo de unidade.
        using (var scope = fabrica.Services.CreateScope())
        {
            var banco = scope.ServiceProvider.GetRequiredService<ConnectionContext>();
            await banco.Database.ExecuteSqlRawAsync(
                "UPDATE Usuarios SET UnidadeId = NULL WHERE Email = {0}", email);
        }

        var secretaria = await fabrica.EntrarAsync(email);

        var resposta = await secretaria.PostAsJsonAsync("/api/bootstrap/token", new { });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.Contains("unidade", corpo.GetProperty("erro").GetString()!);
    }
}
