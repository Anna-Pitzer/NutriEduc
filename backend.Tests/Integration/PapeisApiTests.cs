using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Matriz de acesso da Spec 009: 401 sem sessão, 403 fora do escopo, 200 dentro dele.
/// Cada teste cria a própria escola/unidade para não depender de ordem de execução.
/// </summary>
public class PapeisApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PapeisApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.Clone();
    }

    private static string NomeUnico(string prefixo) =>
        $"{prefixo} {Guid.NewGuid():N}";

    private static string EmailUnico() =>
        $"papel.{Guid.NewGuid():N}@nutrieduc.local";

    private static object PayloadAluno(string nome, int escolaId) => new
    {
        nome,
        nascimento = "2015-05-10",
        escola = "Escola Teste",
        serie = "3o ano",
        telefone = "11999999999",
        observacao = "",
        restricoes = new[] { "Lactose" },
        anafilaxia = false,
        nomeResponsavel = "Maria Teste",
        telefoneResponsavel = "11988888888",
        escolaId
    };

    private async Task<(int EscolaId, int UnidadeId)> CriarEscolaComUnidade(
        HttpClient admin)
    {
        var escola = await admin.PostAsJsonAsync("/api/escolas", new
        {
            nome = NomeUnico("Escola"),
            unidadeId = _factory.UnidadeIdPadrao
        });
        escola.EnsureSuccessStatusCode();

        var corpo = await Ler(escola);

        return (corpo.GetProperty("id").GetInt32(),
                corpo.GetProperty("unidadeId").GetInt32());
    }

    private async Task<(int Id, string Email)> CriarUsuario(
        HttpClient admin,
        PapelUsuario papel,
        int? escolaId = null,
        int? unidadeId = null,
        IEnumerable<int>? alunosIds = null)
    {
        var email = EmailUnico();

        var resposta = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Usuario de Papel",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            papel = papel.ToString(),
            escolaId,
            unidadeId,
            alunosIds
        });
        resposta.EnsureSuccessStatusCode();

        var id = (await Ler(resposta)).GetProperty("id").GetInt32();
        return (id, email);
    }

    private async Task<int> CriarAluno(HttpClient cliente, string nome, int escolaId)
    {
        var resposta = await cliente.PostAsJsonAsync(
            "/api/aluno/registrar", PayloadAluno(nome, escolaId));
        resposta.EnsureSuccessStatusCode();

        return (await Ler(resposta)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task AdminEscola_NaoCriaEscola()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, _) = await CriarEscolaComUnidade(secretaria);
        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId);

        var resposta = await adminEscola.PostAsJsonAsync("/api/escolas", new
        {
            nome = NomeUnico("Escola Proibida")
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task AdminEscola_NaoCriaAdminSecretaria()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, _) = await CriarEscolaComUnidade(secretaria);
        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId);

        var resposta = await adminEscola.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Novo Admin",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email = EmailUnico(),
            senha = "senha12345",
            papel = nameof(PapelUsuario.AdminSecretaria)
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task AdminEscola_NaoCriaMerendeira()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = await CriarEscolaComUnidade(secretaria);
        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId);

        var resposta = await adminEscola.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Merendeira Bloqueada",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email = EmailUnico(),
            senha = "senha12345",
            papel = nameof(PapelUsuario.Merendeira),
            escolaId,
            unidadeId
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task AdminEscola_NaoCriaUsuarioEmOutraEscola()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, _) = await CriarEscolaComUnidade(secretaria);
        var (outraEscolaId, outraUnidadeId) = await CriarEscolaComUnidade(secretaria);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId);

        var resposta = await adminEscola.PostAsJsonAsync("/api/usuarios", new
        {
            nome = "Merendeira Alheia",
            cpf = "12345678901",
            dataNascimento = "1990-01-01",
            email = EmailUnico(),
            senha = "senha12345",
            papel = nameof(PapelUsuario.Merendeira),
            escolaId = outraEscolaId,
            unidadeId = outraUnidadeId
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_ListaSomenteAlunosDaPropriaUnidade()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeA) = await CriarEscolaComUnidade(secretaria);

        var (escolaB, _) = await CriarEscolaComUnidade(secretaria);

        await CriarAluno(secretaria, "Aluno Unidade A", escolaId);
        await CriarAluno(secretaria, "Aluno Unidade B", escolaB);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId, unidadeA);

        var resposta = await merendeira.GetAsync("/api/aluno");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var alunos = await Ler(resposta);
        var nomes = alunos.EnumerateArray()
            .Select(aluno => aluno.GetProperty("nome").GetString())
            .ToList();

        Assert.Contains("Aluno Unidade A", nomes);
        Assert.DoesNotContain("Aluno Unidade B", nomes);

        // RF8: leitura de aluno não expõe dados de conta do responsável.
        var texto = await resposta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"cpf\"", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"email\"", texto, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"foto\"", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Responsavel_ForaDoVinculo_Recebe403_EDepoisDoVinculo_Recebe200()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = await CriarEscolaComUnidade(secretaria);

        var alunoVinculado = await CriarAluno(secretaria, "Aluno Vinculado", escolaId);
        var alunoAlvo = await CriarAluno(secretaria, "Aluno Fora do Vinculo", escolaId);
        var (responsavelId, responsavelEmail) = await CriarUsuario(
            secretaria, PapelUsuario.Responsavel, unidadeId: unidadeId,
            alunosIds: new[] { alunoVinculado });

        var responsavel = await _factory.EntrarAsync(responsavelEmail);

        var antes = await responsavel.GetAsync($"/api/aluno/perfil/{alunoAlvo}");
        Assert.Equal(HttpStatusCode.Forbidden, antes.StatusCode);

        // O vínculo é feito pela secretaria: unidade é da rede e não define escola do aluno.
        var vinculo = await secretaria.PostAsync(
            $"/api/aluno/{alunoAlvo}/responsaveis/{responsavelId}", null);
        Assert.Equal(HttpStatusCode.NoContent, vinculo.StatusCode);

        var depois = await responsavel.GetAsync($"/api/aluno/perfil/{alunoAlvo}");
        Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
    }

    [Fact]
    public async Task Vinculo_Duplicado_Retorna409()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = await CriarEscolaComUnidade(secretaria);
        var alunoVinculado = await CriarAluno(secretaria, "Aluno Ja Vinculado", escolaId);
        var alunoAlvo = await CriarAluno(secretaria, "Aluno Duplicado", escolaId);
        var (responsavelId, _) = await CriarUsuario(
            secretaria, PapelUsuario.Responsavel, unidadeId: unidadeId,
            alunosIds: new[] { alunoVinculado });

        var primeiro = await secretaria.PostAsync(
            $"/api/aluno/{alunoAlvo}/responsaveis/{responsavelId}", null);
        Assert.Equal(HttpStatusCode.NoContent, primeiro.StatusCode);

        var segundo = await secretaria.PostAsync(
            $"/api/aluno/{alunoAlvo}/responsaveis/{responsavelId}", null);
        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);

        var corpo = await Ler(segundo);
        Assert.Contains("vinculado", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task Login_ContaSemPapel_Retorna401ComMensagem()
    {
        var (client, email) = await _factory.RegistrarAsync();

        var resposta = await client.PostAsJsonAsync("/api/usuario/login", new
        {
            email,
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);

        var corpo = await Ler(resposta);
        // RF5: mensagem genérica, sem confirmar a existência do e-mail.
        Assert.Equal("Email ou senha incorretos.", corpo.GetProperty("erro").GetString()!);
    }

    [Fact]
    public async Task UsuarioDesativado_PerderSessaoNaProximaRequisicao()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = await CriarEscolaComUnidade(secretaria);

        var (merendeiraId, merendeiraEmail) = await CriarUsuario(
            secretaria, PapelUsuario.Merendeira, escolaId, unidadeId);

        var merendeira = await _factory.EntrarAsync(merendeiraEmail);

        var antes = await merendeira.GetAsync("/api/aluno");
        Assert.Equal(HttpStatusCode.OK, antes.StatusCode);

        var desativar = await secretaria.PostAsync(
            $"/api/usuarios/{merendeiraId}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var depois = await merendeira.GetAsync("/api/aluno");
        Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);

        var login = await merendeira.PostAsJsonAsync("/api/usuario/login", new
        {
            email = merendeiraEmail,
            senha = "senha12345"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Me_DevolvePapelEEscopo()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = await CriarEscolaComUnidade(secretaria);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId, unidadeId);

        var resposta = await merendeira.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await Ler(resposta);
        Assert.Equal("Merendeira", corpo.GetProperty("papel").GetString());
        Assert.Equal(escolaId, corpo.GetProperty("escolaId").GetInt32());
        Assert.Equal(unidadeId, corpo.GetProperty("unidadeId").GetInt32());
    }

    [Fact]
    public async Task Escola_ComNomeDuplicado_Retorna409()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var nome = NomeUnico("Escola Repetida");

        var primeiro = await secretaria.PostAsJsonAsync("/api/escolas", new
        {
            nome,
            unidadeId = _factory.UnidadeIdPadrao
        });
        primeiro.EnsureSuccessStatusCode();

        var segundo = await secretaria.PostAsJsonAsync("/api/escolas", new
        {
            nome,
            unidadeId = _factory.UnidadeIdPadrao
        });

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task Listar_ComFiltroAtivo_SeparaAtivasDeDesativadas()
    {
        var secretaria = await _factory.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);
        var (escolaId, unidadeId) = await CriarEscolaComUnidade(secretaria);

        var (ativaId, _) = await CriarUsuario(
            secretaria, PapelUsuario.Merendeira, escolaId, unidadeId);
        var (inativaId, _) = await CriarUsuario(
            secretaria, PapelUsuario.Merendeira, escolaId, unidadeId);

        var desativar = await secretaria.PostAsync($"/api/usuarios/{inativaId}/desativar", null);
        Assert.Equal(HttpStatusCode.OK, desativar.StatusCode);

        var ativas = await secretaria.GetAsync("/api/usuarios?ativo=true");
        var desativadas = await secretaria.GetAsync("/api/usuarios?ativo=false");

        Assert.Equal(HttpStatusCode.OK, ativas.StatusCode);
        Assert.Equal(HttpStatusCode.OK, desativadas.StatusCode);

        var idsAtivas = await Ids(ativas);
        var idsDesativadas = await Ids(desativadas);

        Assert.Contains(ativaId, idsAtivas);
        Assert.DoesNotContain(inativaId, idsAtivas);
        Assert.Contains(inativaId, idsDesativadas);
        Assert.DoesNotContain(ativaId, idsDesativadas);
    }

    private static async Task<List<int>> Ids(HttpResponseMessage resposta)
    {
        var corpo = await Ler(resposta);

        return corpo.EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToList();
    }
}
