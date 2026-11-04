using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP da Spec 023: trilha completa de alterações do aluno (RF6), formato do
/// evento, paginação, matriz de acesso e compatibilidade da rota antiga de restrições (RF7).
/// </summary>
public class HistoricoAlunoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HistoricoAlunoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object PayloadAluno(
        string nome,
        string telefone = "11999999999",
        int? escolaId = null,
        params string[] restricoes) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Escola Teste",
        serie = "3o ano",
        telefone,
        observacao = "",
        restricoes,
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888",
        escolaId
    };

    private async Task<int> CriarAluno(
        HttpClient client,
        string nome,
        string telefone = "11999999999",
        int? escolaId = null)
    {
        var resposta = await client.PostAsJsonAsync(
            "/api/aluno/registrar",
            PayloadAluno(nome, telefone, escolaId ?? _factory.EscolaIdPadrao, "Lactose"));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    private static JsonElement[] Itens(JsonElement pagina) =>
        pagina.GetProperty("itens").EnumerateArray().ToArray();

    [Fact]
    public async Task Historico_AlunoCriado_RegistraCriacaoERestricao()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(client, "Aluno Trilha Criacao");

        var historia = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/historico");

        var itens = Itens(historia);

        Assert.Equal(2, historia.GetProperty("total").GetInt32());
        Assert.Contains(itens, item => item.GetProperty("tipo").GetString() == "Criado");
        Assert.Contains(itens, item =>
            item.GetProperty("tipo").GetString() == "RestricaoAdicionada" &&
            item.GetProperty("valorNovo").GetString() == "Lactose");
    }

    [Fact]
    public async Task Historico_AlteracaoDeTelefone_RegistraCampoComValores()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(client, "Aluno Trilha Telefone");

        await client.PutAsJsonAsync(
            $"/api/aluno/{alunoId}",
            PayloadAluno("Aluno Trilha Telefone", "11888887777", _factory.EscolaIdPadrao, "Lactose"));

        var historia = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/historico");

        var evento = Itens(historia).Single(item =>
            item.GetProperty("tipo").GetString() == "Alterado");

        Assert.Equal("Telefone", evento.GetProperty("campo").GetString());
        Assert.Equal("11999999999", evento.GetProperty("valorAnterior").GetString());
        Assert.Equal("11888887777", evento.GetProperty("valorNovo").GetString());
        Assert.False(string.IsNullOrWhiteSpace(evento.GetProperty("autor").GetString()));
    }

    [Fact]
    public async Task Historico_AtualizacaoSemMudanca_NaoRegistraEvento()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(client, "Aluno Trilha Sem Mudanca");

        var antes = (await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/historico")).GetProperty("total").GetInt32();

        await client.PutAsJsonAsync(
            $"/api/aluno/{alunoId}",
            PayloadAluno("Aluno Trilha Sem Mudanca", "11999999999", _factory.EscolaIdPadrao, "Lactose"));

        var depois = (await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/historico")).GetProperty("total").GetInt32();

        Assert.Equal(antes, depois);
    }

    [Fact]
    public async Task Historico_Paginado_DevolveMetadados()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(client, "Aluno Trilha Paginada");

        var pagina = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/historico?pagina=1&tamanho=1");

        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(1, pagina.GetProperty("tamanho").GetInt32());
        Assert.Single(Itens(pagina));
        Assert.True(pagina.GetProperty("total").GetInt32() >= 2);
    }

    [Fact]
    public async Task Historico_MerendeiraDeOutraUnidade_Retorna403()
    {
        var (escolaDoAluno, unidadeDoAluno) = _factory.CriarEscolaComUnidade();
        var secretaria = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeDoAluno);
        var (escolaDaMerendeira, outraUnidade) = _factory.CriarEscolaComUnidade();

        var alunoId = await CriarAluno(
            secretaria, "Aluno Trilha Unidade", escolaId: escolaDoAluno);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: escolaDaMerendeira,
            unidadeId: outraUnidade);

        var resposta = await merendeira.GetAsync($"/api/aluno/{alunoId}/historico");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Historico_AlunoInexistente_Retorna404ParaEscopoGlobal()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var resposta = await client.GetAsync("/api/aluno/999999/historico");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaAntiga_ContinuaDevolvendoFormatoDaSpec010()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var alunoId = await CriarAluno(client, "Aluno Trilha Compatibilidade");

        var pagina = await client.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{alunoId}/restricoes/historico");

        var evento = Assert.Single(Itens(pagina));

        Assert.Equal("Lactose", evento.GetProperty("restricao").GetString());
        Assert.Equal("Adicionada", evento.GetProperty("acao").GetString());
        Assert.True(evento.TryGetProperty("autor", out _));
        Assert.True(evento.TryGetProperty("criadoEm", out _));
    }
}
