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
/// Contrato HTTP das solicitações (Spec 017): criação pelo responsável vinculado, decisão
/// separada por tipo, efeito da restrição no aluno e recusas de escopo/estado.
/// </summary>
public class SolicitacaoApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SolicitacaoApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Cenario(
        HttpClient Admin,
        HttpClient Responsavel,
        HttpClient Nutricionista,
        HttpClient AdminEscola,
        int AlunoId,
        int EscolaId,
        int UnidadeId);

    private async Task<Cenario> PrepararAsync(string nomeAluno)
    {
        _factory.GarantirCatalogo();

        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();

        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);

        var (responsavel, responsavelId) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: unidadeId);

        var (nutricionista, _) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            unidadeId: unidadeId);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola,
            escolaId: escolaId,
            unidadeId: unidadeId);

        var criacao = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome = nomeAluno,
            nascimento = "2015-05-10",
            escola = "Escola Teste",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = string.Empty,
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Maria Teste",
            telefoneResponsavel = "11988888888",
            escolaId
        });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var aluno = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var alunoId = aluno.GetProperty("id").GetInt32();

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}",
            null);

        Assert.True(vinculo.IsSuccessStatusCode);

        return new Cenario(
            admin,
            responsavel,
            nutricionista,
            adminEscola,
            alunoId,
            escolaId,
            unidadeId);
    }

    private static object PedidoRestricao(int alunoId, string restricao = "Lactose") => new
    {
        tipo = "InclusaoAlergia",
        alunoId,
        restricaoNome = restricao,
        descricao = "Ana apresentou reação leve após o lanche."
    };

    private static async Task<int> CriarSolicitacao(
        Cenario cenario,
        string restricao = "Lactose")
    {
        var resposta = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoRestricao(cenario.AlunoId, restricao));

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Criar_InclusaoAlergiaDuplicada_Retorna409ComIdExistente()
    {
        var cenario = await PrepararAsync($"Duplicada {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        var segunda = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoRestricao(cenario.AlunoId));

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

        var corpo = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains($"#{solicitacaoId}", corpo.GetProperty("erro").GetString());

        // Restrição diferente continua permitida para o mesmo aluno.
        var outra = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoRestricao(cenario.AlunoId, "Amendoim"));

        Assert.Equal(HttpStatusCode.Created, outra.StatusCode);

        // Cancelado o primeiro, o mesmo par aluno/restrição pode ser pedido de novo.
        var cancelamento = await cenario.Responsavel.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/cancelar",
            null);
        cancelamento.EnsureSuccessStatusCode();

        var terceira = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoRestricao(cenario.AlunoId));

        Assert.Equal(HttpStatusCode.Created, terceira.StatusCode);
    }

    [Fact]
    public async Task Fluxo_AprovarRestricao_AplicaNoAlunoEGravaTrilha()
    {
        var cenario = await PrepararAsync($"Ana Solicitação {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        var analise = await cenario.Admin.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/analise",
            null);

        Assert.Equal(HttpStatusCode.OK, analise.StatusCode);

        var aprovacao = await cenario.Admin.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado com a família." });

        Assert.Equal(HttpStatusCode.OK, aprovacao.StatusCode);

        var detalhe = await aprovacao.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Concluida", detalhe.GetProperty("status").GetString());
        Assert.Equal(4, detalhe.GetProperty("historico").GetArrayLength());

        var aluno = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/perfil/{cenario.AlunoId}");

        Assert.Contains(
            aluno.GetProperty("restricoes").EnumerateArray(),
            restricao => restricao.GetString() == "Lactose");

        var historico = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{cenario.AlunoId}/historico");

        var evento = historico.GetProperty("itens").EnumerateArray()
            .First(item => item.GetProperty("tipo").GetString() == "RestricaoAdicionada");

        Assert.Equal("Lactose", evento.GetProperty("valorNovo").GetString());
        Assert.Equal("Usuario de Teste", evento.GetProperty("autor").GetString());
    }

    [Fact]
    public async Task Criar_PorResponsavelSemVinculo_Retorna403()
    {
        var cenario = await PrepararAsync($"Bia Solicitação {Guid.NewGuid():N}");

        var (semVinculo, _) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        var resposta = await semVinculo.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoRestricao(cenario.AlunoId));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_DadosCadastrais_PorAdminEscola_Retorna403()
    {
        var cenario = await PrepararAsync($"Carla Solicitação {Guid.NewGuid():N}");

        var resposta = await cenario.AdminEscola.PostAsJsonAsync(
            "/api/solicitacoes",
            new
            {
                tipo = "DadosCadastrais",
                alunoId = cenario.AlunoId,
                descricao = "Corrigir o nome do aluno."
            });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Aprovar_Restricao_ComoNutricionista_Retorna403()
    {
        var cenario = await PrepararAsync($"Dora Solicitação {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        await cenario.Admin.PostAsync($"/api/solicitacoes/{solicitacaoId}/analise", null);

        var resposta = await cenario.Nutricionista.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado." });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Aprovar_SemAnalise_Retorna409()
    {
        var cenario = await PrepararAsync($"Eva Solicitação {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        var resposta = await cenario.Admin.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado." });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Recusar_SemResposta_Retorna400()
    {
        var cenario = await PrepararAsync($"Fabi Solicitação {Guid.NewGuid():N}");

        // "Gluten" é restrição informativa; o teste usa "Lactose" e depois recusa.
        var solicitacaoId = await CriarSolicitacao(cenario);

        await cenario.Admin.PostAsync($"/api/solicitacoes/{solicitacaoId}/analise", null);

        var resposta = await cenario.Admin.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/recusar",
            new { resposta = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var detalhe = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/solicitacoes/{solicitacaoId}");

        Assert.Equal("EmAnalise", detalhe.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cancelar_PeloResponsavel_Retorna200()
    {
        var cenario = await PrepararAsync($"Gabi Solicitação {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        var resposta = await cenario.Responsavel.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/cancelar",
            null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var detalhe = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelada", detalhe.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cancelar_PelaMerendeira_Retorna200()
    {
        var cenario = await PrepararAsync($"Merendeira Cancel {Guid.NewGuid():N}");
        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: cenario.EscolaId,
            unidadeId: cenario.UnidadeId);

        var criacao = await merendeira.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "FaltaIngrediente",
            ingrediente = "Arroz",
            descricao = "Falta arroz no estoque da escola."
        });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var solicitacaoId = corpo.GetProperty("id").GetInt32();

        var cancelamento = await merendeira.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/cancelar",
            null);

        Assert.Equal(HttpStatusCode.OK, cancelamento.StatusCode);
    }

    [Fact]
    public async Task Cancelar_PorTerceiro_Retorna403()
    {
        var cenario = await PrepararAsync($"Terceiro Cancel {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: cenario.EscolaId,
            unidadeId: cenario.UnidadeId);

        var resposta = await merendeira.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/cancelar",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Aprovar_ComoMerendeira_Retorna403()
    {
        var cenario = await PrepararAsync($"Merendeira Decide {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        await cenario.Admin.PostAsync($"/api/solicitacoes/{solicitacaoId}/analise", null);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: cenario.EscolaId,
            unidadeId: cenario.UnidadeId);

        var resposta = await merendeira.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado." });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Aprovar_ComoResponsavel_Retorna403()
    {
        var cenario = await PrepararAsync($"Responsável Decide {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);

        await cenario.Admin.PostAsync($"/api/solicitacoes/{solicitacaoId}/analise", null);

        var resposta = await cenario.Responsavel.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado." });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    private static object PedidoDados(
        int alunoId,
        params (string Campo, string Valor)[] alteracoes) => new
    {
        tipo = "DadosCadastrais",
        alunoId,
        justificativa = "Correção solicitada pela família.",
        alteracoes = alteracoes.Select(alteracao => new
        {
            campo = alteracao.Campo,
            valorSolicitado = alteracao.Valor
        })
    };

    [Fact]
    public async Task DadosCadastrais_Aprovar_AplicaCamposEGravaOrigem()
    {
        var cenario = await PrepararAsync($"Hana Dados {Guid.NewGuid():N}");

        var criacao = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoDados(
                cenario.AlunoId,
                ("nome", "Hana Maria"),
                ("telefoneResponsavel", "11977776666")));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var solicitacaoId = corpo.GetProperty("id").GetInt32();
        var alteracoes = corpo.GetProperty("alteracoes").EnumerateArray().ToList();

        Assert.Equal(2, alteracoes.Count);
        Assert.Contains(
            alteracoes,
            alteracao =>
                alteracao.GetProperty("campo").GetString() == "Nome" &&
                alteracao.GetProperty("valorSolicitado").GetString() == "Hana Maria");

        await cenario.Admin.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/analise",
            null);

        var aprovacao = await cenario.Admin.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado com a família." });

        Assert.Equal(HttpStatusCode.OK, aprovacao.StatusCode);

        var aluno = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/perfil/{cenario.AlunoId}");

        Assert.Equal("Hana Maria", aluno.GetProperty("nome").GetString());
        Assert.Equal("11977776666", aluno.GetProperty("telefoneResponsavel").GetString());

        var historico = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/{cenario.AlunoId}/historico");

        var evento = historico.GetProperty("itens").EnumerateArray()
            .First(item => item.GetProperty("campo").GetString() == "Nome");

        Assert.Equal($"Solicitacao:{solicitacaoId}", evento.GetProperty("origem").GetString());
    }

    [Fact]
    public async Task DadosCadastrais_SegundoPedidoMesmoCampo_Retorna409()
    {
        var cenario = await PrepararAsync($"Iara Dados {Guid.NewGuid():N}");

        var primeiro = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoDados(cenario.AlunoId, ("nome", "Iara Maria")));

        Assert.Equal(HttpStatusCode.Created, primeiro.StatusCode);

        var segundo = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoDados(cenario.AlunoId, ("nome", "Iara Paula")));

        Assert.Equal(HttpStatusCode.Conflict, segundo.StatusCode);
    }

    [Fact]
    public async Task DadosCadastrais_NutricionistaNaoVe_AdminVe()
    {
        var cenario = await PrepararAsync($"Joana Dados {Guid.NewGuid():N}");

        var criacao = await cenario.Responsavel.PostAsJsonAsync(
            "/api/solicitacoes",
            PedidoDados(cenario.AlunoId, ("observacao", "Sem lactose no lanche.")));

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var daNutricao = await cenario.Nutricionista.GetFromJsonAsync<JsonElement>(
            "/api/solicitacoes?tipo=DadosCadastrais");

        Assert.Equal(0, daNutricao.GetProperty("total").GetInt32());

        var doAdmin = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            "/api/solicitacoes?tipo=DadosCadastrais");

        Assert.Equal(1, doAdmin.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task AlunoRenomeado_DetalhePreservaNomeAntigo()
    {
        var nomeOriginal = $"Aluno Original {Guid.NewGuid():N}";
        var cenario = await PrepararAsync(nomeOriginal);
        var solicitacaoId = await CriarSolicitacao(cenario);

        var renomeacao = await cenario.Admin.PutAsJsonAsync($"/api/aluno/{cenario.AlunoId}", new
        {
            nome = $"Aluno Renomeado {Guid.NewGuid():N}",
            nascimento = "2015-05-10",
            escola = "Escola Teste",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = string.Empty,
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Maria Teste",
            telefoneResponsavel = "11988888888",
            escolaId = cenario.EscolaId
        });

        Assert.True(renomeacao.IsSuccessStatusCode);

        var detalhe = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/solicitacoes/{solicitacaoId}");

        // Snapshot preserva o nome original; o vínculo continua íntegro.
        Assert.Equal(nomeOriginal, detalhe.GetProperty("aluno").GetString());
        Assert.Equal(cenario.AlunoId, detalhe.GetProperty("alunoId").GetInt32());
    }

    [Fact]
    public async Task AlunoExcluido_DetalhePreservaNomeEVinculoAusente()
    {
        var nomeOriginal = $"Aluno Snapshot {Guid.NewGuid():N}";
        var cenario = await PrepararAsync(nomeOriginal);
        var solicitacaoId = await CriarSolicitacao(cenario);

        var exclusao = await cenario.Admin.DeleteAsync($"/api/aluno/{cenario.AlunoId}");
        Assert.True(exclusao.IsSuccessStatusCode);

        var detalhe = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/solicitacoes/{solicitacaoId}");

        // FK vira nula (vínculo ausente), mas o snapshot preserva o nome (RNF3/ADR-009).
        Assert.Equal(JsonValueKind.Null, detalhe.GetProperty("alunoId").ValueKind);
        Assert.Equal(nomeOriginal, detalhe.GetProperty("aluno").GetString());
    }

    [Fact]
    public async Task DecisaoConcorrente_UmaPersisteEOutraGeraConflito()
    {
        var cenario = await PrepararAsync($"Concorrência {Guid.NewGuid():N}");
        var solicitacaoId = await CriarSolicitacao(cenario);
        await cenario.Admin.PostAsync($"/api/solicitacoes/{solicitacaoId}/analise", null);

        // Dois contextos independentes (mesma conexão) leem a MESMA versão EmAnalise.
        using var scopeA = _factory.Services.CreateScope();
        var ctxA = scopeA.ServiceProvider.GetRequiredService<ConnectionContext>();
        using var scopeB = _factory.Services.CreateScope();
        var ctxB = scopeB.ServiceProvider.GetRequiredService<ConnectionContext>();

        var solA = ctxA.Solicitacoes.First(solicitacao => solicitacao.Id == solicitacaoId);
        var solB = ctxB.Solicitacoes.First(solicitacao => solicitacao.Id == solicitacaoId);

        Assert.Equal(StatusSolicitacao.EmAnalise, solA.Status);
        Assert.Equal(StatusSolicitacao.EmAnalise, solB.Status);

        solA.Aprovar("Confirmado.", "Maria", DateTime.UtcNow);
        solB.Aprovar("Confirmado.", "Maria", DateTime.UtcNow);

        ctxA.SaveChanges();

        // A segunda gravação usa `WHERE Status = 'EmAnalise'` (concurrency token) e não
        // encontra linha: DbUpdateConcurrencyException → 409 pelo middleware, nunca 500.
        Assert.Throws<DbUpdateConcurrencyException>(() => ctxB.SaveChanges());
    }
}
