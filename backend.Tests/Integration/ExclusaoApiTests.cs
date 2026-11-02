using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Exclusão definitiva com bloqueio por referência (decisão de 2026-10-31). Cada caso
/// verifica um vínculo que impede a exclusão e o caminho livre (`204`).
/// </summary>
public class ExclusaoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ExclusaoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string Nome(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";

    private static async Task<int> CriarEscola(HttpClient admin)
    {
        var resposta = await admin.PostAsJsonAsync("/api/escolas", new { nome = Nome("Escola") });
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    /// <summary>Unidade nasce no bootstrap; aqui ela é criada direto no repositório.</summary>
    private int CriarUnidade() => _factory.CriarEscolaComUnidade().UnidadeId;

    private static async Task<(int Id, string Email)> CriarUsuario(
        HttpClient admin,
        PapelUsuario papel,
        int? escolaId = null,
        int? unidadeId = null)
    {
        var email = $"exclusao.{Guid.NewGuid():N}@nutrieduc.local";

        var resposta = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Conta Exclusão",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            papel = papel.ToString(),
            escolaId,
            unidadeId
        });
        resposta.EnsureSuccessStatusCode();

        return ((await Ler(resposta)).GetProperty("id").GetInt32(), email);
    }

    private static async Task<int> CriarAluno(HttpClient admin, int unidadeId)
    {
        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome = Nome("Aluno"),
            nascimento = "2015-05-10",
            escola = "Escola Teste",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = "",
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Maria Teste",
            telefoneResponsavel = "11988888888",
            unidadeId
        });
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta) =>
        JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Escola_SemReferencias_Exclui204()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin);

        var resposta = await admin.DeleteAsync($"/api/escolas/{escolaId}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    [Fact]
    public async Task Escola_ComUsuarioVinculado_Retorna409()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin);
        await CriarUsuario(admin, PapelUsuario.Merendeira, escolaId);

        var resposta = await admin.DeleteAsync($"/api/escolas/{escolaId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("usuários", (await Ler(resposta)).GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Unidade_SemReferencias_Exclui204()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var unidadeId = CriarUnidade();

        var resposta = await admin.DeleteAsync($"/api/unidades/{unidadeId}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    [Fact]
    public async Task Unidade_ComUsuarioVinculado_Retorna409()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin);
        var unidadeId = CriarUnidade();
        await CriarUsuario(admin, PapelUsuario.Merendeira, escolaId, unidadeId);

        var resposta = await admin.DeleteAsync($"/api/unidades/{unidadeId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("usuários", (await Ler(resposta)).GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Unidade_ComAlunoVinculado_Retorna409()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var unidadeId = CriarUnidade();
        await CriarAluno(admin, unidadeId);

        var resposta = await admin.DeleteAsync($"/api/unidades/{unidadeId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("alunos", (await Ler(resposta)).GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Usuario_SemReferencias_Exclui204()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin);
        var (id, _) = await CriarUsuario(admin, PapelUsuario.Merendeira, escolaId);

        var resposta = await admin.DeleteAsync($"/api/usuarios/{id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    [Fact]
    public async Task Usuario_PropriaConta_Retorna409()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var perfil = await Ler(await admin.GetAsync("/api/usuario/me"));
        var meuId = perfil.GetProperty("id").GetInt32();

        var resposta = await admin.DeleteAsync($"/api/usuarios/{meuId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("própria conta", (await Ler(resposta)).GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Usuario_DeOutraEscola_PorAdminEscola_Retorna403()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscola(secretaria);
        var escolaB = await CriarEscola(secretaria);
        var (idB, _) = await CriarUsuario(secretaria, PapelUsuario.Merendeira, escolaB);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaA);

        var resposta = await adminEscola.DeleteAsync($"/api/usuarios/{idB}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Escola_PorPapelNaoAdministrativo_Retorna403()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(secretaria);

        var nutricionista = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.Nutricionista);

        var resposta = await nutricionista.DeleteAsync($"/api/escolas/{escolaId}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
