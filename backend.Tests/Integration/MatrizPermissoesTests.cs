using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Conformidade HTTP da matriz (Spec 028, T8/RNF3). Cobre as células exercitáveis pelos
/// recursos já implementados; recursos ainda sem endpoint são cobertos pela suíte pura
/// de <c>MatrizDePermissoesTests</c>.
/// </summary>
public class MatrizPermissoesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public MatrizPermissoesTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object PayloadAluno(string nome, int escolaId) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Rótulo",
        serie = "3o ano",
        telefone = "11999999999",
        observacao = "",
        restricoes = Array.Empty<string>(),
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888",
        escolaId
    };

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static async Task<int> CriarEscola(HttpClient admin, int unidadeId)
    {
        var resposta = await admin.PostAsJsonAsync("/api/escolas", new
        {
            nome = $"Escola {Guid.NewGuid():N}",
            unidadeId
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
        var email = $"matriz.{Guid.NewGuid():N}@nutrieduc.local";

        var resposta = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Usuario Matriz",
            cpf = ApiFactory.GerarCpfUnico(),
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

    [Fact]
    public async Task Nutricionista_NaoCriaAluno()
    {
        var nutricionista = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Nutricionista);

        var resposta = await nutricionista.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno($"Aluno Nutri {Guid.NewGuid():N}", _factory.EscolaIdPadrao));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task AdminSecretaria_CriaAlunoNaPropriaUnidade()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var resposta = await admin.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno($"Aluno ADM {Guid.NewGuid():N}", _factory.EscolaIdPadrao));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task AdminEscola_NaoCriaVinculo()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin, _factory.UnidadeIdPadrao);

        var aluno = await admin.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno($"Aluno Vinculo {Guid.NewGuid():N}", escolaId));
        aluno.EnsureSuccessStatusCode();
        var alunoId = (await Ler(aluno)).GetProperty("id").GetInt32();

        var (responsavelId, _) = await CriarUsuario(
            admin, PapelUsuario.Responsavel, unidadeId: _factory.UnidadeIdPadrao);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId);

        var resposta = await adminEscola.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}", null);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task AdminSecretaria_NaoAtribuiPapelEmOutraUnidade()
    {
        var (escolaB, unidadeB) = _factory.CriarEscolaComUnidade();
        var adminB = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeB);

        var (usuarioId, email) = await CriarUsuario(
            adminB, PapelUsuario.Responsavel, unidadeId: unidadeB);

        var adminA = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var resposta = await adminA.PutAsJsonAsync($"/api/usuarios/{usuarioId}", new
        {
            nome = "Usuario Matriz",
            email,
            papel = nameof(PapelUsuario.Merendeira),
            escolaId = escolaB,
            unidadeId = unidadeB
        });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Usuario_NaoAlteraOProprioPapel()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var perfil = await Ler(await admin.GetAsync("/api/usuario/me"));
        var meuId = perfil.GetProperty("id").GetInt32();

        var resposta = await admin.PutAsJsonAsync($"/api/usuarios/{meuId}", new
        {
            nome = perfil.GetProperty("nome").GetString(),
            email = perfil.GetProperty("email").GetString(),
            papel = nameof(PapelUsuario.Merendeira),
            escolaId = _factory.EscolaIdPadrao,
            unidadeId = _factory.UnidadeIdPadrao
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Vinculo_Leitura_RespeitaMatriz()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscola(admin, _factory.UnidadeIdPadrao);

        var aluno = await admin.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno($"Aluno Leitura Vinculo {Guid.NewGuid():N}", escolaId));
        aluno.EnsureSuccessStatusCode();
        var alunoId = (await Ler(aluno)).GetProperty("id").GetInt32();

        var (responsavelId, email) = await CriarUsuario(
            admin, PapelUsuario.Responsavel, unidadeId: _factory.UnidadeIdPadrao);

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}", null);
        Assert.Equal(HttpStatusCode.NoContent, vinculo.StatusCode);

        var responsavel = await _factory.EntrarAsync(email);
        var comoResponsavel = await responsavel.GetAsync(
            $"/api/aluno/{alunoId}/responsaveis");
        Assert.Equal(HttpStatusCode.OK, comoResponsavel.StatusCode);

        var nutricionista = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Nutricionista);
        var comoNutricionista = await nutricionista.GetAsync(
            $"/api/aluno/{alunoId}/responsaveis");
        Assert.Equal(HttpStatusCode.Forbidden, comoNutricionista.StatusCode);
    }

    [Fact]
    public async Task FiltroDeConsultaForaDoEscopo_Retorna403()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();

        var porUnidade = await admin.GetAsync($"/api/usuarios?unidadeId={outraUnidade}");
        Assert.Equal(HttpStatusCode.Forbidden, porUnidade.StatusCode);

        var escolaExterna = await CriarEscola(
            await _factory.CreateAuthenticatedClientAsync(
                PapelUsuario.AdminSecretaria, unidadeId: outraUnidade),
            outraUnidade);

        var porEscola = await admin.GetAsync($"/api/usuarios?escolaId={escolaExterna}");
        Assert.Equal(HttpStatusCode.Forbidden, porEscola.StatusCode);
    }
}
