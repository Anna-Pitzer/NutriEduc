using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

public class SolicitacaoAcessoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SolicitacaoAcessoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string EmailUnico() => $"acesso.{Guid.NewGuid():N}@nutrieduc.local";

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<JsonElement>());

    private async Task<int> RegistrarAsync(
        string tipo,
        int unidadeId,
        string? crn = null)
    {
        var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Solicitante Acesso",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1990-01-01",
            email = EmailUnico(),
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            tipo,
            unidadeId,
            crn
        });
        resposta.EnsureSuccessStatusCode();
        return (await Ler(resposta)).GetProperty("solicitacaoId").GetInt32();
    }

    private async Task<(int SolicitacaoId, int UsuarioId)> RegistrarERetornarIds(
        string tipo,
        int unidadeId,
        string? crn = null)
    {
        var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Solicitante Acesso",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1990-01-01",
            email = EmailUnico(),
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            tipo,
            unidadeId,
            crn
        });
        resposta.EnsureSuccessStatusCode();
        var corpo = await Ler(resposta);
        return (corpo.GetProperty("solicitacaoId").GetInt32(), corpo.GetProperty("usuarioId").GetInt32());
    }

    private async Task<int> CriarAlunoAsync(HttpClient admin, int escolaId)
    {
        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome = "Aluno Acesso",
            nascimento = "2015-05-10",
            escola = "Escola Acesso",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = "",
            restricoes = new[] { "Lactose" },
            anafilaxia = false,
            nomeResponsavel = "Responsavel Teste",
            telefoneResponsavel = "11988888888",
            escolaId
        });
        resposta.EnsureSuccessStatusCode();
        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Aprovar_Nutricionista_GravaPapelUnidadeECrn()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var solicitacaoId = await RegistrarAsync("Nutricionista", unidadeId, ApiFactory.GerarCrnUnico());

        var aprovar = await admin.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar", new { });
        Assert.Equal(HttpStatusCode.OK, aprovar.StatusCode);

        var detalhe = await Ler(aprovar);
        Assert.Equal("Aprovada", detalhe.GetProperty("status").GetString());
        Assert.Equal("Nutricionista", detalhe.GetProperty("papelConcedido").GetString());
    }

    [Fact]
    public async Task Aprovar_Responsavel_CriaVinculoNoMesmoAto()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAlunoAsync(admin, escolaId);

        var solicitacaoId = await RegistrarAsync("Responsavel", unidadeId);

        var aprovar = await admin.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar",
            new { alunosIds = new[] { alunoId } });
        Assert.Equal(HttpStatusCode.OK, aprovar.StatusCode);

        var responsaveis = await admin.GetAsync($"/api/aluno/{alunoId}/responsaveis");
        Assert.Equal(HttpStatusCode.OK, responsaveis.StatusCode);
        Assert.NotEmpty((await Ler(responsaveis)).EnumerateArray());
    }

    [Fact]
    public async Task Aprovar_Responsavel_AlunoDeOutraUnidade_404_NaoGravaNada()
    {
        var (_, unidadeA) = _factory.CriarEscolaComUnidade();
        var (escolaB, unidadeB) = _factory.CriarEscolaComUnidade();

        var adminA = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeA);
        var adminB = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeB);

        var alunoDeB = await CriarAlunoAsync(adminB, escolaB);

        var solicitacaoId = await RegistrarAsync("Responsavel", unidadeA);

        var aprovar = await adminA.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar",
            new { alunosIds = new[] { alunoDeB } });

        Assert.Equal(HttpStatusCode.NotFound, aprovar.StatusCode);

        // Nada foi gravado: a solicitação continua pendente.
        var detalhe = await adminA.GetAsync($"/api/solicitacoes-acesso/{solicitacaoId}");
        var corpo = await Ler(detalhe);
        Assert.Equal("Pendente", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DecisaoConcorrente_Uma200EUma409()
    {
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var adminA = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var adminB = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var solicitacaoId = await RegistrarAsync("Nutricionista", unidadeId, ApiFactory.GerarCrnUnico());

        var primeira = await adminA.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar", new { });
        var segunda = await adminB.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar", new { });

        var status = new[] { primeira.StatusCode, segunda.StatusCode };
        Assert.Contains(HttpStatusCode.OK, status);
        Assert.Contains(HttpStatusCode.Conflict, status);
    }

    [Fact]
    public async Task Fila_DeOutraUnidade_404()
    {
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var (_, outraUnidadeId) = _factory.CriarEscolaComUnidade();

        var adminOutra = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: outraUnidadeId);

        var solicitacaoId = await RegistrarAsync("Responsavel", unidadeId);

        var resposta = await adminOutra.GetAsync($"/api/solicitacoes-acesso/{solicitacaoId}");
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task DecisaoConcorrente_Responsavel_NaoDuplicaVinculo()
    {
        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();
        var adminA = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var adminB = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAlunoAsync(adminA, escolaId);

        var (solicitacaoId, _) = await RegistrarERetornarIds("Responsavel", unidadeId);

        var primeira = await adminA.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar", new { alunosIds = new[] { alunoId } });
        var segunda = await adminB.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/aprovar", new { alunosIds = new[] { alunoId } });

        Assert.Contains(HttpStatusCode.OK, new[] { primeira.StatusCode, segunda.StatusCode });
        Assert.Contains(HttpStatusCode.Conflict, new[] { primeira.StatusCode, segunda.StatusCode });

        // Ausência de efeito duplicado: exatamente um vínculo, não dois.
        var responsaveis = await adminA.GetAsync($"/api/aluno/{alunoId}/responsaveis");
        var lista = (await Ler(responsaveis)).EnumerateArray().ToList();
        Assert.Single(lista);
    }

    [Fact]
    public async Task Reconsideracao_CriaNovaSolicitacao_AposRecusa()
    {
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var (solicitacaoId, usuarioId) = await RegistrarERetornarIds("Responsavel", unidadeId);

        var recusar = await admin.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{solicitacaoId}/recusar", new { motivo = "Motivo válido." });
        Assert.Equal(HttpStatusCode.OK, recusar.StatusCode);

        // Reconsideração: nova solicitação para o mesmo usuário, criada pelo ADM.
        var reconsiderar = await admin.PostAsJsonAsync("/api/solicitacoes-acesso", new
        {
            usuarioId,
            tipo = "Responsavel",
            unidadeId,
            crn = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, reconsiderar.StatusCode);

        var nova = await Ler(reconsiderar);
        Assert.Equal("Pendente", nova.GetProperty("status").GetString());
        Assert.NotEqual(solicitacaoId, nova.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Reconsideracao_ComPendenteExistente_409()
    {
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var (_, usuarioId) = await RegistrarERetornarIds("Responsavel", unidadeId);

        var duplicada = await admin.PostAsJsonAsync("/api/solicitacoes-acesso", new
        {
            usuarioId,
            tipo = "Responsavel",
            unidadeId,
            crn = (string?)null
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
    }

    [Fact]
    public async Task DiagnosticoSemSolicitacao_Indisponivel()
    {
        // Conflito T13 × ADR-021/028 (PEND-046): a exposição global foi desabilitada até a
        // decisão do grupo. A rota não deve servir a listagem global (4xx de cliente).
        var (_, unidadeId) = _factory.CriarEscolaComUnidade();
        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var resposta = await admin.GetAsync("/api/usuarios/diagnostico-sem-solicitacao");

        Assert.NotEqual(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(
            (int)resposta.StatusCode >= 400 && (int)resposta.StatusCode < 500,
            $"Esperava erro de cliente (4xx), obteve {(int)resposta.StatusCode}");
    }
}
