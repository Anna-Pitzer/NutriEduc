using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Spec 039 — Perfil de Merendeira.
///
/// T3 (conformidade): a unidade da merendeira é sempre derivada da escola nos três caminhos de
/// produção (criação administrativa, edição de papel e aprovação), nunca do payload.
/// T4 (isolamento): a merendeira lê e cria apenas no escopo da própria escola/unidade; escola de
/// outra unidade responde 404 e escola da mesma unidade responde 403.
///
/// Cada teste usa uma fábrica isolada para não herdar dados de outro cenário.
/// </summary>
public class MerendeiraEscopoTests
{
    private static string EmailUnico() => $"merendeira.{Guid.NewGuid():N}@nutrieduc.local";

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>();

    /// <summary>Lê o vínculo institucional persistido do usuário, direto do repositório.</summary>
    private static (int? EscolaId, int? UnidadeId) EscopoPersistido(
        ApiFactory fabrica,
        string email)
    {
        using var scope = fabrica.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var usuario = usuarios.GetByEmail(email)
            ?? throw new InvalidOperationException($"Usuário {email} não existe.");
        return (usuario.EscolaId, usuario.UnidadeId);
    }

    private static object PayloadCriacao(
        string email,
        PapelUsuario papel,
        int? escolaId = null,
        int? unidadeId = null,
        string? crn = null) => new
    {
        nome = "Usuario de Teste",
        cpf = ApiFactory.GerarCpfUnico(),
        dataNascimento = "1990-01-01",
        email,
        senha = "senha12345",
        telefone = (string?)null,
        foto = (string?)null,
        papel = papel.ToString(),
        escolaId,
        unidadeId,
        crn
    };

    // ------------------------------------------------------------------ T3 — conformidade

    [Fact]
    public async Task CriarMerendeiraPorAdministracao_DerivaUnidadeDaEscola()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var (_, unidadeB) = fabrica.CriarEscolaComUnidade();

        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeA);

        var email = EmailUnico();

        // O `unidadeId` do payload aponta para OUTRA unidade; deve ser ignorado e a unidade
        // derivada da escola (ResolverEscopoInstitucional).
        var criacao = await admin.PostAsJsonAsync(
            "/api/usuarios",
            PayloadCriacao(email, PapelUsuario.Merendeira, escolaId: escolaA, unidadeId: unidadeB));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var (escola, unidade) = EscopoPersistido(fabrica, email);
        Assert.Equal(escolaA, escola);
        Assert.Equal(unidadeA, unidade);
    }

    [Fact]
    public async Task AtualizarPapelParaMerendeira_DerivaUnidadeDaEscola()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();

        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeA);

        var email = EmailUnico();

        var criacao = await admin.PostAsJsonAsync(
            "/api/usuarios",
            PayloadCriacao(email, PapelUsuario.Nutricionista, unidadeId: unidadeA, crn: ApiFactory.GerarCrnUnico()));
        criacao.EnsureSuccessStatusCode();
        var usuarioId = (await Ler(criacao)).GetProperty("id").GetInt32();

        // Muda o papel para Merendeira; `unidadeId` do payload é inválido e deve ser ignorado.
        var atualizacao = await admin.PutAsJsonAsync($"/api/usuarios/{usuarioId}", new
        {
            nome = "Usuario de Teste",
            email,
            papel = nameof(PapelUsuario.Merendeira),
            escolaId = escolaA,
            unidadeId = 999_999
        });

        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);

        var (escola, unidade) = EscopoPersistido(fabrica, email);
        Assert.Equal(escolaA, escola);
        Assert.Equal(unidadeA, unidade);
    }

    [Fact]
    public async Task AprovarMerendeira_DerivaUnidadeDaEscola()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();

        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeA);

        // Cadastro público do tipo Merendeira, sem papel; gera uma solicitação de acesso.
        var email = EmailUnico();
        var cliente = fabrica.CreateClient();
        var registro = await cliente.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Usuario de Teste",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            tipo = "Merendeira",
            unidadeId = unidadeA,
            crn = (string?)null
        });
        registro.EnsureSuccessStatusCode();
        var solicitacaoId = (await Ler(registro)).GetProperty("solicitacaoId").GetInt32();

        var aprovacao = await admin.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar",
            new { escolaId = escolaA });

        Assert.Equal(HttpStatusCode.OK, aprovacao.StatusCode);

        var (escola, unidade) = EscopoPersistido(fabrica, email);
        Assert.Equal(escolaA, escola);
        Assert.Equal(unidadeA, unidade);
    }

    // ------------------------------------------------------------------ T4 — isolamento

    private static object PayloadSolicitacao(string ingrediente, string descricao) => new
    {
        tipo = "Ingrediente",
        ingrediente,
        descricao
    };

    [Fact]
    public async Task Merendeira_ListaSolicitacoes_SomenteDaPropriaEscola()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var escolaB = fabrica.CriarEscolaNaUnidade(unidadeA);

        var merendeiraA = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);
        var merendeiraB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaB, unidadeA);

        var criacaoA = await merendeiraA.PostAsJsonAsync(
            "/api/solicitacoes", PayloadSolicitacao("Arroz", "Solicito arroz para a escola A."));
        criacaoA.EnsureSuccessStatusCode();
        var idA = (await Ler(criacaoA)).GetProperty("id").GetInt32();

        var criacaoB = await merendeiraB.PostAsJsonAsync(
            "/api/solicitacoes", PayloadSolicitacao("Feijão", "Solicito feijão para a escola B."));
        criacaoB.EnsureSuccessStatusCode();
        var idB = (await Ler(criacaoB)).GetProperty("id").GetInt32();

        var lista = await merendeiraA.GetAsync("/api/solicitacoes");
        lista.EnsureSuccessStatusCode();
        var ids = (await Ler(lista)).GetProperty("itens").EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToList();

        Assert.Contains(idA, ids);
        Assert.DoesNotContain(idB, ids);
    }

    [Fact]
    public async Task Merendeira_NaoLeSolicitacaoDeOutraEscolaDaMesmaUnidade_403()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var escolaB = fabrica.CriarEscolaNaUnidade(unidadeA);

        var merendeiraA = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);
        var merendeiraB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaB, unidadeA);

        var criacaoB = await merendeiraB.PostAsJsonAsync(
            "/api/solicitacoes", PayloadSolicitacao("Feijão", "Solicito feijão para a escola B."));
        criacaoB.EnsureSuccessStatusCode();
        var idB = (await Ler(criacaoB)).GetProperty("id").GetInt32();

        var resposta = await merendeiraA.GetAsync($"/api/solicitacoes/{idB}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_NaoLeSolicitacaoDeOutraUnidade_404()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var (escolaB, unidadeB) = fabrica.CriarEscolaComUnidade();

        var merendeiraA = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);
        var merendeiraB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaB, unidadeB);

        var criacaoB = await merendeiraB.PostAsJsonAsync(
            "/api/solicitacoes", PayloadSolicitacao("Feijão", "Solicito feijão para a escola B."));
        criacaoB.EnsureSuccessStatusCode();
        var idB = (await Ler(criacaoB)).GetProperty("id").GetInt32();

        var resposta = await merendeiraA.GetAsync($"/api/solicitacoes/{idB}");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_CriaSolicitacao_EscolaForjadaNoPayloadEhIgnorada()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var escolaB = fabrica.CriarEscolaNaUnidade(unidadeA);

        var merendeiraA = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);

        // `CriarSolicitacaoDTO` não expõe escola/unidade: campos forjados são ignorados pelo
        // model binder e a escola gravada é sempre a da sessão.
        var criacao = await merendeiraA.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "Ingrediente",
            ingrediente = "Aveia",
            descricao = "Solicito aveia para a escola A.",
            escolaId = escolaB,
            unidadeId = 999_999
        });

        criacao.EnsureSuccessStatusCode();
        var corpo = await Ler(criacao);
        Assert.Equal(escolaA, corpo.GetProperty("escolaId").GetInt32());
    }

    [Fact]
    public async Task Merendeira_ListaFeedbacks_SomenteDaPropriaEscola()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var escolaB = fabrica.CriarEscolaNaUnidade(unidadeA);

        var merendeiraA = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);
        var merendeiraB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaB, unidadeA);

        var fbA = await merendeiraA.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Feedback da escola A",
            mensagem = "Comentário da escola A."
        });
        fbA.EnsureSuccessStatusCode();
        var idA = (await Ler(fbA)).GetProperty("id").GetInt32();

        var fbB = await merendeiraB.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Feedback da escola B",
            mensagem = "Comentário da escola B."
        });
        fbB.EnsureSuccessStatusCode();
        var idB = (await Ler(fbB)).GetProperty("id").GetInt32();

        var lista = await merendeiraA.GetAsync("/api/feedbacks");
        lista.EnsureSuccessStatusCode();
        var ids = (await Ler(lista)).GetProperty("itens").EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToList();

        Assert.Contains(idA, ids);
        Assert.DoesNotContain(idB, ids);
    }
}
