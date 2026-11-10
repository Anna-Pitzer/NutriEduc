using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
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

    private async Task<int> CriarEscola(HttpClient admin)
    {
        var resposta = await admin.PostAsJsonAsync("/api/escolas", new
        {
            nome = Nome("Escola"),
            unidadeId = _factory.UnidadeIdPadrao
        });
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

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

    private static async Task<int> CriarAluno(HttpClient admin, int escolaId)
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
            escolaId
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
    public async Task Unidade_ComAdminDaPropriaUnidade_Retorna409()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var perfil = await Ler(await admin.GetAsync("/api/usuario/me"));
        var unidadeId = perfil.GetProperty("unidadeId").GetInt32();

        var resposta = await admin.DeleteAsync($"/api/unidades/{unidadeId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Unidade_ComUsuarioVinculado_Retorna409()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var perfil = await Ler(await admin.GetAsync("/api/usuario/me"));
        var unidadeId = perfil.GetProperty("unidadeId").GetInt32();
        var escolaId = await CriarEscola(admin);
        await CriarUsuario(admin, PapelUsuario.Merendeira, escolaId, unidadeId);

        var resposta = await admin.DeleteAsync($"/api/unidades/{unidadeId}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("usuários", (await Ler(resposta)).GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Unidade_ComAlunoVinculado_Retorna409()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        await CriarAluno(admin, escolaId);

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

    [Fact]
    public async Task Usuario_ComNotificacao_Retorna409_EPreserva()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin);
        var (id, _) = await CriarUsuario(admin, PapelUsuario.Merendeira, escolaId);

        using (var escopo = _factory.Services.CreateScope())
        {
            var notificacoes = escopo.ServiceProvider.GetRequiredService<INotificacaoRepository>();
            var unidadeDeTrabalho = escopo.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

            unidadeDeTrabalho.ExecutarEmTransacao(() => notificacoes.Add(new Notificacao(
                id,
                TipoNotificacao.SolicitacaoStatus,
                "Título válido",
                "Mensagem válida.",
                "solicitacao",
                1,
                DateTime.UtcNow)));
        }

        var resposta = await admin.DeleteAsync($"/api/usuarios/{id}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("notifica", (await Ler(resposta)).GetProperty("erro").GetString()!);

        using (var escopo = _factory.Services.CreateScope())
        {
            var usuarios = escopo.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            var notificacoes = escopo.ServiceProvider.GetRequiredService<INotificacaoRepository>();

            Assert.NotNull(usuarios.GetById(id));
            Assert.True(notificacoes.ExistePorDestinatario(id));
        }
    }
}
