using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Spec 034: o responsável só existe com vínculo e o ADM encontra o aluno por busca.
/// </summary>
public class VinculoResponsavelApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public VinculoResponsavelApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string EmailUnico() => $"responsavel.{Guid.NewGuid():N}@nutrieduc.local";

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<int> CriarAlunoAsync(
        HttpClient admin,
        int escolaId,
        string nome,
        string serie = "3o ano")
    {
        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome,
            nascimento = "2015-05-10",
            escola = "Escola Teste",
            serie,
            telefone = "11999999999",
            observacao = "",
            restricoes = new[] { "Lactose" },
            anafilaxia = false,
            nomeResponsavel = "Contato do Aluno",
            telefoneResponsavel = "11988888888",
            escolaId
        });

        resposta.EnsureSuccessStatusCode();
        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    private async Task<HttpResponseMessage> CriarResponsavelAsync(
        HttpClient admin,
        string email,
        IEnumerable<int>? alunosIds)
    {
        return await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Responsável de Teste",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1985-01-01",
            email,
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            papel = "Responsavel",
            escolaId = (int?)null,
            unidadeId = (int?)null,
            crn = (string?)null,
            alunosIds
        });
    }

    [Fact]
    public async Task CriarResponsavel_SemAlunos_Retorna400()
    {
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var resposta = await CriarResponsavelAsync(admin, EmailUnico(), alunosIds: null);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CriarResponsavel_ComAluno_CriaVinculoNoMesmoAto()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAlunoAsync(admin, escolaId, "Aluno Vinculado");
        var email = EmailUnico();

        var resposta = await CriarResponsavelAsync(admin, email, new[] { alunoId });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var responsaveis = await admin.GetAsync($"/api/aluno/{alunoId}/responsaveis");
        Assert.Equal(HttpStatusCode.OK, responsaveis.StatusCode);

        var itens = (await Ler(responsaveis)).EnumerateArray().ToList();
        Assert.Contains(itens, item => item.GetProperty("email").GetString() == email);
    }

    [Fact]
    public async Task CriarResponsavel_AlunoDeOutraUnidade_Retorna404()
    {
        var (_, unidadeA) = _factory.CriarEscolaComUnidade();
        var (escolaB, unidadeB) = _factory.CriarEscolaComUnidade();

        var adminA = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeA);
        var adminB = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeB);

        var alunoDeB = await CriarAlunoAsync(adminB, escolaB, "Aluno de Outra Unidade");
        var email = EmailUnico();

        var resposta = await CriarResponsavelAsync(adminA, email, new[] { alunoDeB });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        // A validação roda antes de qualquer mutação: nenhuma conta é criada.
        using var scope = _factory.Services.CreateScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        Assert.Null(usuarios.GetByEmail(email));
    }

    [Fact]
    public async Task BuscarAlunos_FiltraPorNomeEscolaESerie()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        await CriarAlunoAsync(admin, escolaId, "Ana Busca", "3o ano");
        await CriarAlunoAsync(admin, escolaId, "Beto Busca", "5o ano");

        var porNome = await admin.GetAsync("/api/aluno?busca=Ana");
        Assert.Equal(HttpStatusCode.OK, porNome.StatusCode);
        var nomes = (await Ler(porNome)).EnumerateArray()
            .Select(item => item.GetProperty("nome").GetString())
            .ToList();
        Assert.Contains("Ana Busca", nomes);
        Assert.DoesNotContain("Beto Busca", nomes);

        var porSerie = await admin.GetAsync("/api/aluno?serie=5o");
        var nomesSerie = (await Ler(porSerie)).EnumerateArray()
            .Select(item => item.GetProperty("nome").GetString())
            .ToList();
        Assert.Contains("Beto Busca", nomesSerie);
        Assert.DoesNotContain("Ana Busca", nomesSerie);

        var porEscola = await admin.GetAsync($"/api/aluno?busca=Busca&escolaId={escolaId}");
        Assert.Equal(HttpStatusCode.OK, porEscola.StatusCode);
        Assert.Equal(2, (await Ler(porEscola)).GetArrayLength());
    }

    [Fact]
    public async Task DefinirPapelResponsavel_SemAlunos_Retorna400_ComAluno_Vincula()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAlunoAsync(admin, escolaId, "Aluno Promovido");
        var email = EmailUnico();

        var criacao = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Merendeira Promovida",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1988-03-04",
            email,
            senha = "senha12345",
            papel = "Merendeira",
            escolaId,
            unidadeId = (int?)null,
            crn = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var usuarioId = (await Ler(criacao)).GetProperty("id").GetInt32();

        var semAlunos = await admin.PutAsJsonAsync($"/api/usuarios/{usuarioId}", new
        {
            nome = "Responsável Promovida",
            email,
            telefone = (string?)null,
            papel = "Responsavel",
            escolaId = (int?)null,
            unidadeId = (int?)null,
            crn = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, semAlunos.StatusCode);

        var comAluno = await admin.PutAsJsonAsync($"/api/usuarios/{usuarioId}", new
        {
            nome = "Responsável Promovida",
            email,
            telefone = (string?)null,
            papel = "Responsavel",
            escolaId = (int?)null,
            unidadeId = (int?)null,
            crn = (string?)null,
            alunosIds = new[] { alunoId }
        });
        Assert.Equal(HttpStatusCode.OK, comAluno.StatusCode);

        var responsaveis = await admin.GetAsync($"/api/aluno/{alunoId}/responsaveis");
        Assert.Equal(HttpStatusCode.OK, responsaveis.StatusCode);
        Assert.Contains(
            (await Ler(responsaveis)).EnumerateArray(),
            item => item.GetProperty("email").GetString() == email);
    }

    [Fact]
    public async Task VincularResponsavel_Duplicado_Retorna409()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAlunoAsync(admin, escolaId, "Aluno Vínculo Duplicado");

        var criacao = await CriarResponsavelAsync(admin, EmailUnico(), new[] { alunoId });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var usuarioId = (await Ler(criacao)).GetProperty("id").GetInt32();

        var resposta = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{usuarioId}",
            null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task VincularResponsavel_UsuarioDesativado_Retorna409SemGravar()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoVinculado = await CriarAlunoAsync(admin, escolaId, "Aluno do Vínculo");
        var outroAluno = await CriarAlunoAsync(admin, escolaId, "Aluno Sem Vínculo");
        var email = EmailUnico();

        var criacao = await CriarResponsavelAsync(admin, email, new[] { alunoVinculado });
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var usuarioId = (await Ler(criacao)).GetProperty("id").GetInt32();

        var desativacao = await admin.PostAsync($"/api/usuarios/{usuarioId}/desativar", null);
        desativacao.EnsureSuccessStatusCode();

        var resposta = await admin.PostAsync(
            $"/api/aluno/{outroAluno}/responsaveis/{usuarioId}",
            null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        var listagem = await admin.GetAsync($"/api/aluno/{outroAluno}/responsaveis");
        Assert.DoesNotContain(
            (await Ler(listagem)).EnumerateArray(),
            item => item.GetProperty("email").GetString() == email);
    }
}
