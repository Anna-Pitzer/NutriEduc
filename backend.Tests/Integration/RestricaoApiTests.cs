using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP da Spec 010: catálogo editável só pela nutrição (RF2), validação do
/// catálogo na gravação do aluno (RF4), histórico com eventos (RF5) e matriz de acesso do
/// histórico e do catálogo (RF6, RF7).
/// </summary>
public class RestricaoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RestricaoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object PayloadAluno(
        string nome,
        int? escolaId = null,
        params string[] restricoes) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Escola Teste",
        serie = "3o ano",
        telefone = "11999999999",
        observacao = "",
        restricoes,
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888",
        escolaId
    };

    private static object NovaRestricao(string nome, CategoriaRestricao categoria) =>
        new { nome, categoria = categoria.ToString() };

    [Fact]
    public async Task Catalogo_ComNomeDuplicado_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var primeiro = await client.PostAsJsonAsync(
            "/api/restricoes",
            NovaRestricao("Sulfito", CategoriaRestricao.Restricao));
        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        // Mesma categoria, caixa e acento diferentes: o índice normalizado recusa.
        var duplicado = await client.PostAsJsonAsync(
            "/api/restricoes",
            NovaRestricao(" sulfíto ", CategoriaRestricao.Restricao));

        Assert.Equal(HttpStatusCode.BadRequest, duplicado.StatusCode);

        using var corpo = JsonDocument.Parse(await duplicado.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(corpo.RootElement.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Catalogo_MesmoNomeEmCategoriasDiferentes_EhPermitido()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var alergia = await client.PostAsJsonAsync(
            "/api/restricoes",
            NovaRestricao("Corante", CategoriaRestricao.Alergia));
        var restricao = await client.PostAsJsonAsync(
            "/api/restricoes",
            NovaRestricao("Corante", CategoriaRestricao.Restricao));

        Assert.Equal(HttpStatusCode.Created, alergia.StatusCode);
        Assert.Equal(HttpStatusCode.Created, restricao.StatusCode);
    }

    [Fact]
    public async Task Catalogo_AdminEscola_NaoPodeCriar()
    {
        var (escolaId, _) = _factory.CriarEscolaComUnidade();
        var client = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola,
            escolaId: escolaId);

        var resposta = await client.PostAsJsonAsync(
            "/api/restricoes",
            NovaRestricao("Avelã", CategoriaRestricao.Alergia));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Catalogo_IncluirInativas_RestritoANutricao()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: escolaId,
            unidadeId: unidadeId);

        var resposta = await merendeira.GetAsync("/api/restricoes?incluirInativas=true");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Catalogo_SemSessao_Retorna401()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/api/restricoes");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Aluno_ComRestricaoForaDoCatalogo_Retorna400ENaoGrava()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var resposta = await client.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno("Aluno Sem Catalogo", null, "Produto Quimico Xyz"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(
            "Restrição desconhecida: Produto Quimico Xyz.",
            corpo.RootElement.GetProperty("erro").GetString());

        var alunos = await client.GetFromJsonAsync<List<JsonElement>>("/api/aluno");
        Assert.DoesNotContain(alunos!, aluno =>
            aluno.GetProperty("nome").GetString() == "Aluno Sem Catalogo");
    }

    [Fact]
    public async Task Aluno_ComDuasRestricoes_GravaDoisEventosAdicionada()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var criacao = await client.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno("Aluno Com Duas", _factory.EscolaIdPadrao, "Lactose", "Gluten"));
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var alunoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var historico = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/restricoes/historico");

        Assert.Equal(2, historico.GetProperty("total").GetInt32());
        Assert.All(
            historico.GetProperty("itens").EnumerateArray(),
            item => Assert.Equal("Adicionada", item.GetProperty("acao").GetString()));
    }

    [Fact]
    public async Task Aluno_TrocandoRestricao_GravaAdicaoERemocao()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var criacao = await client.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno("Aluno Troca", _factory.EscolaIdPadrao, "Lactose"));
        var alunoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var atualizacao = await client.PutAsJsonAsync(
            $"/api/aluno/{alunoId}",
            PayloadAluno("Aluno Troca", _factory.EscolaIdPadrao, "Gluten"));
        Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);

        var historico = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/restricoes/historico");

        var acoes = historico.GetProperty("itens").EnumerateArray()
            .Select(item => (item.GetProperty("acao").GetString(),
                             item.GetProperty("restricao").GetString()))
            .ToList();

        Assert.Contains(("Removida", "Lactose"), acoes);
        Assert.Contains(("Adicionada", "Gluten"), acoes);
    }

    [Fact]
    public async Task Aluno_SemMudarRestricao_NaoGravaEvento()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var criacao = await client.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno("Aluno Sem Mudanca", _factory.EscolaIdPadrao, "Lactose"));
        var alunoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        await client.PutAsJsonAsync(
            $"/api/aluno/{alunoId}",
            PayloadAluno("Aluno Sem Mudanca", _factory.EscolaIdPadrao, "Lactose"));

        var historico = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/restricoes/historico");

        Assert.Equal(1, historico.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Historico_MerendeiraDeOutraUnidade_Retorna403()
    {
        var (escolaDoAluno, unidadeDoAluno) = _factory.CriarEscolaComUnidade();
        var administracao = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeDoAluno);
        var (escolaDaMerendeira, outraUnidade) = _factory.CriarEscolaComUnidade();

        var criacao = await administracao.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno("Aluno Da Unidade", escolaDoAluno, "Lactose"));
        var alunoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: escolaDaMerendeira,
            unidadeId: outraUnidade);

        var resposta = await merendeira.GetAsync(
            $"/api/aluno/{alunoId}/restricoes/historico");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Historico_ResponsavelNaoVinculado_Retorna403()
    {
        var (escolaDoAluno, unidade) = _factory.CriarEscolaComUnidade();
        var administracao = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidade);

        var criacao = await administracao.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno("Aluno Nao Vinculado", escolaDoAluno, "Lactose"));
        var alunoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var responsavel = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Responsavel,
            unidadeId: unidade);

        var resposta = await responsavel.GetAsync(
            $"/api/aluno/{alunoId}/restricoes/historico");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Historico_AlunoInexistente_Retorna404ParaEscopoGlobal()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/aluno/999999/restricoes/historico");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Historico_TamanhoAcimaDoLimite_Retorna400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/aluno/1/restricoes/historico?tamanho=101");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
