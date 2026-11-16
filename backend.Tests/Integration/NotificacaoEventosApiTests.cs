using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Eventos reais que geram notificação (Spec 018 + Spec 033): nova solicitação de acesso,
/// decisão de acesso, solicitação de alergia/ingrediente e ocorrência registrada. Cada teste
/// usa uma fábrica isolada para não herdar notificações de outro cenário.
/// </summary>
public class NotificacaoEventosApiTests
{
    private static string EmailUnico(string prefixo) =>
        $"{prefixo}.{Guid.NewGuid():N}@nutrieduc.local";

    private static async Task<List<JsonElement>> ListarAsync(
        HttpClient cliente,
        bool apenasNaoLidas = false)
    {
        var url = apenasNaoLidas
            ? "/api/notificacoes?naoLidas=true&tamanho=100"
            : "/api/notificacoes?tamanho=100";

        var resposta = await cliente.GetAsync(url);
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("itens").EnumerateArray().ToList();
    }

    private static async Task<int> ContarNaoLidasAsync(HttpClient cliente)
    {
        var resposta = await cliente.GetAsync("/api/notificacoes?naoLidas=true&tamanho=1");
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("total").GetInt32();
    }

    /// <summary>Referência apontando para o recurso de origem da notificação.</summary>
    private static bool MesmaReferencia(JsonElement item, string referenciaTipo, int referenciaId) =>
        item.GetProperty("referenciaTipo").ValueKind == JsonValueKind.String &&
        item.GetProperty("referenciaTipo").GetString() == referenciaTipo &&
        item.GetProperty("referenciaId").ValueKind == JsonValueKind.Number &&
        item.GetProperty("referenciaId").GetInt32() == referenciaId;

    private sealed record Registro(int SolicitacaoId, int UsuarioId, string Email);

    private static async Task<Registro> RegistrarAcessoAsync(
        ApiFactory fabrica,
        string tipo,
        int unidadeId,
        string? crn = null)
    {
        var cliente = fabrica.CreateClient();
        var email = EmailUnico("acesso");

        var resposta = await cliente.PostAsJsonAsync("/api/usuario/registrar", new
        {
            nome = "Solicitante de Acesso",
            cpf = ApiFactory.GerarCpfUnico(),
            dataNascimento = "1990-01-01",
            email,
            senha = "senha12345",
            telefone = (string?)null,
            foto = (string?)null,
            tipo,
            unidadeId,
            crn
        });

        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return new Registro(
            corpo.GetProperty("solicitacaoId").GetInt32(),
            corpo.GetProperty("usuarioId").GetInt32(),
            email);
    }

    private static async Task<int> CriarAlunoAsync(HttpClient admin, int escolaId)
    {
        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome = $"Aluno Notificação {Guid.NewGuid():N}",
            nascimento = "2015-05-10",
            escola = "Escola Teste",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = string.Empty,
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Contato do Aluno",
            telefoneResponsavel = "11988888888",
            escolaId
        });

        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();
    }

    // ---------- Solicitação de acesso ----------

    [Fact]
    public async Task SolicitarAcesso_NotificaAdmDaUnidadeEPersiste()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var registro = await RegistrarAcessoAsync(
            fabrica, "Nutricionista", unidadeId, ApiFactory.GerarCrnUnico());

        var item = Assert.Single(
            await ListarAsync(admin),
            notificacao => MesmaReferencia(notificacao, "SolicitacaoAcesso", registro.SolicitacaoId));

        Assert.Equal("SolicitacaoAcesso", item.GetProperty("tipo").GetString());
        Assert.False(item.GetProperty("lida").GetBoolean());
        Assert.Contains("Nova solicitação", item.GetProperty("titulo").GetString());

        // Recarregar a lista (F5 ou novo login) mantém a notificação persistida.
        var segundaLeitura = await ListarAsync(admin);
        Assert.Contains(
            segundaLeitura,
            notificacao => notificacao.GetProperty("id").GetInt32() == item.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task SolicitarAcesso_NaoNotificaAdmDeOutraUnidade()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeA) = fabrica.CriarEscolaComUnidade();
        var (_, unidadeB) = fabrica.CriarEscolaComUnidade();

        var adminB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeB);

        var registro = await RegistrarAcessoAsync(
            fabrica, "Nutricionista", unidadeA, ApiFactory.GerarCrnUnico());

        Assert.DoesNotContain(
            await ListarAsync(adminB),
            notificacao => MesmaReferencia(notificacao, "SolicitacaoAcesso", registro.SolicitacaoId));
    }

    [Fact]
    public async Task AprovarAcesso_NotificaSolicitanteEAtualizaIndicador()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var registro = await RegistrarAcessoAsync(
            fabrica, "Nutricionista", unidadeId, ApiFactory.GerarCrnUnico());

        var aprovacao = await admin.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{registro.SolicitacaoId}/aprovar",
            new { });

        aprovacao.EnsureSuccessStatusCode();

        var solicitante = await fabrica.EntrarAsync(registro.Email);

        var item = Assert.Single(
            await ListarAsync(solicitante),
            notificacao => MesmaReferencia(notificacao, "SolicitacaoAcesso", registro.SolicitacaoId));

        Assert.Equal("SolicitacaoStatus", item.GetProperty("tipo").GetString());
        Assert.False(item.GetProperty("lida").GetBoolean());

        var antes = await ContarNaoLidasAsync(solicitante);
        Assert.True(antes >= 1);

        var leitura = await solicitante.PostAsync(
            $"/api/notificacoes/{item.GetProperty("id").GetInt32()}/ler",
            null);

        leitura.EnsureSuccessStatusCode();

        Assert.Equal(antes - 1, await ContarNaoLidasAsync(solicitante));
    }

    [Fact]
    public async Task RecusarAcesso_NotificaSolicitante()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var registro = await RegistrarAcessoAsync(
            fabrica, "Nutricionista", unidadeId, ApiFactory.GerarCrnUnico());

        var recusa = await admin.PostAsJsonAsync(
            $"/api/solicitacoes-acesso/{registro.SolicitacaoId}/recusar",
            new { motivo = "Documentação incompleta." });

        recusa.EnsureSuccessStatusCode();

        // A conta recusada continua sem papel e não autentica; a notificação é verificada
        // no destinatário real (o solicitante), direto na persistência.
        using var scope = fabrica.Services.CreateScope();
        var notificacoes = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();
        var (itens, _) = notificacoes.Listar(registro.UsuarioId, null, 1, 100);

        var item = Assert.Single(
            itens,
            notificacao => notificacao.ReferenciaTipo == "SolicitacaoAcesso" &&
                           notificacao.ReferenciaId == registro.SolicitacaoId);

        Assert.Equal(TipoNotificacao.SolicitacaoStatus, item.Tipo);
        Assert.False(item.Lida);
    }

    // ---------- Demais eventos ----------

    [Fact]
    public async Task SolicitacaoDeAlergia_NotificaAdministracao()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);
        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: unidadeId);

        var alunoId = await CriarAlunoAsync(admin, escolaId);
        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}",
            null);
        vinculo.EnsureSuccessStatusCode();

        var criacao = await responsavel.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "InclusaoAlergia",
            alunoId,
            restricaoNome = "Lactose",
            descricao = "Reação leve após o lanche."
        });

        criacao.EnsureSuccessStatusCode();
        var solicitacaoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var item = Assert.Single(
            await ListarAsync(admin),
            notificacao => MesmaReferencia(notificacao, "Solicitacao", solicitacaoId));

        Assert.Equal("NovaSolicitacao", item.GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task SolicitacaoDeIngrediente_NotificaNutricionista()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        var (nutricionista, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            unidadeId: unidadeId);
        var (merendeira, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Merendeira,
            escolaId: escolaId,
            unidadeId: unidadeId);

        var criacao = await merendeira.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "Ingrediente",
            ingrediente = "Aveia",
            descricao = "Solicito aveia para o cardápio da semana."
        });

        criacao.EnsureSuccessStatusCode();
        var solicitacaoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var item = Assert.Single(
            await ListarAsync(nutricionista),
            notificacao => MesmaReferencia(notificacao, "Solicitacao", solicitacaoId));

        Assert.Equal("NovaSolicitacao", item.GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task SolicitacaoCriadaPeloNutricionista_NaoNotificaOMesmoAutor()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();

        var (nutricionista, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            unidadeId: unidadeId);

        var criacao = await nutricionista.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "Ingrediente",
            ingrediente = "Arroz",
            descricao = "Solicito arroz para o estoque."
        });

        criacao.EnsureSuccessStatusCode();
        var solicitacaoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        Assert.DoesNotContain(
            await ListarAsync(nutricionista),
            notificacao => MesmaReferencia(notificacao, "Solicitacao", solicitacaoId));
    }

    [Fact]
    public async Task OcorrenciaRegistradaPeloResponsavel_NotificaNutricionista()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);
        var (nutricionista, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            unidadeId: unidadeId);
        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: unidadeId);

        var alunoId = await CriarAlunoAsync(admin, escolaId);
        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}",
            null);
        vinculo.EnsureSuccessStatusCode();

        var criacao = await responsavel.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Aluno",
            descricao = "Aluno relatou mal-estar após a refeição.",
            alunoId
        });

        criacao.EnsureSuccessStatusCode();
        var ocorrenciaId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var item = Assert.Single(
            await ListarAsync(nutricionista),
            notificacao => MesmaReferencia(notificacao, "Ocorrencia", ocorrenciaId));

        Assert.Equal("OcorrenciaRegistrada", item.GetProperty("tipo").GetString());
    }
}
