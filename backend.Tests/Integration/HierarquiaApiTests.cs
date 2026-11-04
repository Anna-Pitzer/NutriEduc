using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Database;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Matriz de escopo da hierarquia unidade → escola → aluno (Spec 027, RF9/RF10/RF14).
/// 200 no próprio escopo, 403 na fronteira de escola/vínculo, 404 na fronteira de unidade,
/// 401 sem sessão. Cada teste cria a própria cadeia para não depender de ordem.
/// </summary>
public class HierarquiaApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HierarquiaApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object PayloadAluno(string nome, int escolaId) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Rótulo de exibição",
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

    private static async Task<int> CriarAluno(
        HttpClient client,
        string nome,
        int escolaId)
    {
        var resposta = await client.PostAsJsonAsync(
            "/api/aluno/registrar", PayloadAluno(nome, escolaId));
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarEscolaNaUnidade(HttpClient admin, int unidadeId)
    {
        var resposta = await admin.PostAsJsonAsync("/api/escolas", new
        {
            nome = $"Escola {Guid.NewGuid():N}",
            unidadeId
        });
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Post_Escola_SemUnidade_Retorna400ENaoGrava()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var nome = $"Escola sem unidade {Guid.NewGuid():N}";

        var resposta = await admin.PostAsJsonAsync("/api/escolas", new { nome });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var escolas = await admin.GetFromJsonAsync<List<JsonElement>>("/api/escolas");
        Assert.DoesNotContain(escolas!, escola =>
            escola.GetProperty("nome").GetString() == nome);
    }

    [Fact]
    public async Task Post_Aluno_SemEscola_Retorna400ENaoGrava()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var nome = $"Aluno sem escola {Guid.NewGuid():N}";

        var payload = new
        {
            nome,
            nascimento = "2015-05-10",
            escola = "Rótulo de exibição",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = "",
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Maria Teste",
            telefoneResponsavel = "11988888888"
        };

        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", payload);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var alunos = await admin.GetFromJsonAsync<List<JsonElement>>("/api/aluno");
        Assert.DoesNotContain(alunos!, aluno => aluno.GetProperty("nome").GetString() == nome);
    }

    [Fact]
    public async Task Post_Aluno_ComEscola_DerivaUnidadeDaEscola()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var resposta = await admin.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno($"Aluno derivado {Guid.NewGuid():N}", escolaId));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.Equal(escolaId, corpo.GetProperty("escolaId").GetInt32());
        Assert.Equal(unidadeId, corpo.GetProperty("unidadeId").GetInt32());
    }

    [Fact]
    public async Task Get_AdminEscola_VeSomenteAlunosDaPropriaEscola()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var escolaB = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);

        var alunoA = await CriarAluno(admin, $"Aluno A {Guid.NewGuid():N}", escolaA);
        await CriarAluno(admin, $"Aluno B {Guid.NewGuid():N}", escolaB);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaA);

        var alunos = await adminEscola.GetFromJsonAsync<List<JsonElement>>("/api/aluno");

        Assert.NotNull(alunos);
        Assert.Contains(alunos!, aluno => aluno.GetProperty("id").GetInt32() == alunoA);
        Assert.All(alunos!, aluno => Assert.Equal(escolaA, aluno.GetProperty("escolaId").GetInt32()));
    }

    [Fact]
    public async Task Get_AdminSecretaria_VeSomenteAlunosDaPropriaUnidade()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoProprio = await CriarAluno(
            admin, $"Aluno próprio {Guid.NewGuid():N}", _factory.EscolaIdPadrao);

        var (outraEscola, outraUnidade) = _factory.CriarEscolaComUnidade();
        var adminDaOutraUnidade = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: outraUnidade);
        var alunoDeOutraUnidade = await CriarAluno(
            adminDaOutraUnidade, $"Aluno externo {Guid.NewGuid():N}", outraEscola);

        var alunos = await admin.GetFromJsonAsync<List<JsonElement>>("/api/aluno");

        Assert.NotNull(alunos);
        Assert.Contains(alunos!, aluno => aluno.GetProperty("id").GetInt32() == alunoProprio);
        Assert.DoesNotContain(alunos!, aluno =>
            aluno.GetProperty("id").GetInt32() == alunoDeOutraUnidade);

        var perfil = await admin.GetAsync($"/api/aluno/perfil/{alunoDeOutraUnidade}");
        Assert.Equal(HttpStatusCode.NotFound, perfil.StatusCode);
    }

    [Fact]
    public async Task Get_Merendeira_VeSomenteAlunosDaPropriaEscola()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var escolaB = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);

        var alunoA = await CriarAluno(admin, $"Aluno Merenda A {Guid.NewGuid():N}", escolaA);
        await CriarAluno(admin, $"Aluno Merenda B {Guid.NewGuid():N}", escolaB);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA);

        var alunos = await merendeira.GetFromJsonAsync<List<JsonElement>>("/api/aluno");

        Assert.NotNull(alunos);
        Assert.Contains(alunos!, aluno => aluno.GetProperty("id").GetInt32() == alunoA);
        Assert.All(alunos!, aluno => Assert.Equal(escolaA, aluno.GetProperty("escolaId").GetInt32()));
    }

    [Fact]
    public async Task Get_Perfil_ResponsavelSemVinculo_Retorna403()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAluno(admin, $"Aluno sem vínculo {Guid.NewGuid():N}", escolaId);

        var responsavel = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Responsavel, unidadeId: unidadeId);

        var resposta = await responsavel.GetAsync($"/api/aluno/perfil/{alunoId}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Get_SemSessao_Retorna401()
    {
        var cliente = _factory.CreateClient();

        var resposta = await cliente.GetAsync("/api/aluno");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Diagnostico_Hierarquia_ListaLegado()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(
            admin, $"Aluno legado {Guid.NewGuid():N}", _factory.EscolaIdPadrao);

        // Simula registro legado gravado antes da Spec 027, sem escola.
        using (var scope = _factory.Services.CreateScope())
        {
            var banco = scope.ServiceProvider.GetRequiredService<ConnectionContext>();
            await banco.Database.ExecuteSqlRawAsync(
                "UPDATE Alunos SET EscolaId = NULL WHERE Id = {0}", alunoId);
        }

        var resposta = await admin.GetAsync("/api/diagnostico/hierarquia");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.True(corpo.GetProperty("alunosSemEscola").GetInt32() >= 1);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, _factory.EscolaIdPadrao);

        var negado = await adminEscola.GetAsync("/api/diagnostico/hierarquia");
        Assert.Equal(HttpStatusCode.Forbidden, negado.StatusCode);
    }

    [Fact]
    public async Task Post_Aluno_ComUnidadeDivergente_IgnoraUnidadeDoPayload()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();

        var payload = new
        {
            nome = $"Aluno divergente {Guid.NewGuid():N}",
            nascimento = "2015-05-10",
            escola = "Rótulo de exibição",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = "",
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Maria Teste",
            telefoneResponsavel = "11988888888",
            escolaId,
            unidadeId = outraUnidade
        };

        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", payload);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.Equal(escolaId, corpo.GetProperty("escolaId").GetInt32());
        Assert.Equal(unidadeId, corpo.GetProperty("unidadeId").GetInt32());
    }

    [Fact]
    public async Task Put_EscolaComAlunos_NaoMudaDeUnidade()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        await CriarAluno(admin, $"Aluno bloqueante {Guid.NewGuid():N}", escolaId);

        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();

        var resposta = await admin.PutAsJsonAsync($"/api/escolas/{escolaId}", new
        {
            nome = $"Escola renomeada {Guid.NewGuid():N}",
            unidadeId = outraUnidade
        });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        var escolas = await admin.GetFromJsonAsync<List<JsonElement>>("/api/escolas");
        var escola = Assert.Single(escolas!, item =>
            item.GetProperty("id").GetInt32() == escolaId);
        Assert.Equal(_factory.UnidadeIdPadrao, escola.GetProperty("unidadeId").GetInt32());
    }

    [Fact]
    public async Task Get_Aluno_ComFiltroDeEscola_EstreitaSemVazar()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var escolaB = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);

        var alunoA = await CriarAluno(admin, $"Aluno filtro A {Guid.NewGuid():N}", escolaA);
        await CriarAluno(admin, $"Aluno filtro B {Guid.NewGuid():N}", escolaB);

        var porEscola = await admin.GetFromJsonAsync<List<JsonElement>>(
            $"/api/aluno?escolaId={escolaA}");
        var aluno = Assert.Single(porEscola!);
        Assert.Equal(alunoA, aluno.GetProperty("id").GetInt32());

        // Escola de outra unidade não pode ser alcançada pelo filtro: 403.
        var (escolaExterna, _) = _factory.CriarEscolaComUnidade();
        var externa = await admin.GetAsync($"/api/aluno?escolaId={escolaExterna}");
        Assert.Equal(HttpStatusCode.Forbidden, externa.StatusCode);
    }

    [Fact]
    public async Task Get_Aluno_ComFiltroDeUnidade_EstreitaSemVazar()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(
            admin, $"Aluno unidade própria {Guid.NewGuid():N}", _factory.EscolaIdPadrao);

        var propria = await admin.GetFromJsonAsync<List<JsonElement>>(
            $"/api/aluno?unidadeId={_factory.UnidadeIdPadrao}");
        Assert.Contains(propria!, aluno => aluno.GetProperty("id").GetInt32() == alunoId);

        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();
        var externa = await admin.GetAsync($"/api/aluno?unidadeId={outraUnidade}");
        Assert.Equal(HttpStatusCode.Forbidden, externa.StatusCode);
    }

    [Fact]
    public async Task Get_Escolas_ComFiltroDeUnidade_EstreitaSemVazar()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaId = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);

        var propria = await admin.GetFromJsonAsync<List<JsonElement>>(
            $"/api/escolas?unidadeId={_factory.UnidadeIdPadrao}");
        Assert.Contains(propria!, escola => escola.GetProperty("id").GetInt32() == escolaId);

        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();
        var externa = await admin.GetAsync($"/api/escolas?unidadeId={outraUnidade}");
        Assert.Equal(HttpStatusCode.Forbidden, externa.StatusCode);
    }

    [Fact]
    public async Task TransferirEscola_PorAdminEscola_Retorna403()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var escolaB = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var alunoId = await CriarAluno(admin, $"Aluno transferência {Guid.NewGuid():N}", escolaA);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaA);

        var resposta = await adminEscola.PostAsJsonAsync(
            $"/api/aluno/{alunoId}/transferir-escola", new { escolaId = escolaB });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Sessao_MudancaDePapel_ÉInvalidaNaRequisicaoSeguinte()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var escolaB = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);

        var (_, email) = await _factory.RegistrarAsync();
        _factory.DefinirPapel(email, PapelUsuario.AdminEscola, escolaId: escolaA);
        var cliente = await _factory.EntrarAsync(email);

        var antes = await cliente.GetAsync("/api/aluno");
        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);

        // RNF5: papel e escola mudam no banco; a claims antiga não pode continuar valendo.
        _factory.DefinirPapel(email, PapelUsuario.Merendeira, escolaId: escolaB);

        var depois = await cliente.GetAsync("/api/aluno");
        Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);
    }

    [Fact]
    public async Task Put_Aluno_ComEscolaDeOutraUnidade_Retorna400ENaoMove()
    {
        var admin = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var escolaA = await CriarEscolaNaUnidade(admin, _factory.UnidadeIdPadrao);
        var nome = $"Aluno preso à unidade {Guid.NewGuid():N}";
        var alunoId = await CriarAluno(admin, nome, escolaA);

        var (escolaExterna, _) = _factory.CriarEscolaComUnidade();

        var resposta = await admin.PutAsJsonAsync(
            $"/api/aluno/{alunoId}", PayloadAluno(nome, escolaExterna));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var perfil = await admin.GetFromJsonAsync<JsonElement>($"/api/aluno/perfil/{alunoId}");
        Assert.Equal(escolaA, perfil.GetProperty("escolaId").GetInt32());
    }
}
