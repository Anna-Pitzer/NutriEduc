using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Contrato HTTP das ocorrências (Spec 016): criação por papel e vínculo, derivação de
/// escola, escopo por escola/unidade, transições de status, cancelamento e contrato de erro.
/// </summary>
public class OcorrenciaApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OcorrenciaApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Cenario(
        HttpClient Admin,
        HttpClient Nutricionista,
        HttpClient Merendeira,
        HttpClient AdminEscola,
        HttpClient Responsavel,
        int ResponsavelId,
        int AlunoId,
        int EscolaId,
        int UnidadeId);

    private async Task<Cenario> PrepararAsync()
    {
        _factory.GarantirCatalogo();

        var (escolaId, unidadeId) = _factory.CriarEscolaComUnidade();

        var admin = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);

        var (nutricionista, _) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista, unidadeId: unidadeId);

        var merendeira = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId: escolaId, unidadeId: unidadeId);

        var adminEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId: escolaId, unidadeId: unidadeId);

        var (responsavel, responsavelId) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel, unidadeId: unidadeId);

        var alunoId = await CriarAlunoAsync(admin, escolaId);

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}", null);
        Assert.True(vinculo.IsSuccessStatusCode);

        return new Cenario(
            admin, nutricionista, merendeira, adminEscola, responsavel,
            responsavelId, alunoId, escolaId, unidadeId);
    }

    private static async Task<int> CriarAlunoAsync(HttpClient admin, int escolaId, string? nome = null)
    {
        var criacao = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome = nome ?? $"Aluno {Guid.NewGuid():N}",
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
        return aluno.GetProperty("id").GetInt32();
    }

    private static async Task<int> CriarOcorrenciaAsync(
        HttpClient cliente,
        string tipo,
        string descricao = "Ocorrência de teste.",
        int? alunoId = null,
        int? cardapioId = null,
        int? refeicaoId = null,
        int? alimentoId = null)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo,
            descricao,
            alunoId,
            cardapioId,
            refeicaoId,
            alimentoId
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return corpo.GetProperty("id").GetInt32();
    }

    // ---------- Criação e derivação de escola ----------

    [Fact]
    public async Task Merendeira_RegistraOutro_ApareceNaListagemDaEscola()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Merendeira.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Outro",
            descricao = "Refeitório fechado por manutenção."
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(cenario.EscolaId, corpo.GetProperty("escolaId").GetInt32());
        Assert.Equal(cenario.UnidadeId, corpo.GetProperty("unidadeId").GetInt32());

        var listagem = await cenario.Merendeira.GetFromJsonAsync<JsonElement>(
            "/api/ocorrencias");

        Assert.Contains(
            listagem.GetProperty("itens").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == corpo.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Nutricionista_RegistraOutro_SemAluno_EscolaNula()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Nutricionista.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Outro",
            descricao = "Problema geral da unidade."
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, corpo.GetProperty("escolaId").ValueKind);
    }

    [Fact]
    public async Task Nutricionista_RegistraAluno_EscolaDerivadaDoAluno()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Nutricionista.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Aluno",
            descricao = "Aluno passou mal após o lanche.",
            alunoId = cenario.AlunoId
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(cenario.EscolaId, corpo.GetProperty("escolaId").GetInt32());
        Assert.Equal(cenario.AlunoId, corpo.GetProperty("alunoId").GetInt32());
    }

    [Fact]
    public async Task Pai_RegistraParaAlunoVinculado_201()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Responsavel.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Aluno",
            descricao = "Aluno relatou desconforto.",
            alunoId = cenario.AlunoId
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Pai_RegistraParaAlunoNaoVinculado_403()
    {
        var cenario = await PrepararAsync();

        var (semVinculo, _) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel, unidadeId: cenario.UnidadeId);

        var resposta = await semVinculo.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Aluno",
            descricao = "Tentativa indevida.",
            alunoId = cenario.AlunoId
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    // ---------- Escopo ----------

    [Fact]
    public async Task AdminEscola_NaoVeOcorrenciaDeOutraEscola()
    {
        var cenario = await PrepararAsync();

        var outraEscola = _factory.CriarEscolaNaUnidade(cenario.UnidadeId);

        var adminOutraEscola = await _factory.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminEscola, escolaId: outraEscola, unidadeId: cenario.UnidadeId);

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.AdminEscola, "Outro", "Ocorrência da escola A.");

        var detalhe = await adminOutraEscola.GetAsync($"/api/ocorrencias/{ocorrenciaId}");

        Assert.Equal(HttpStatusCode.Forbidden, detalhe.StatusCode);
    }

    [Fact]
    public async Task Nutricionista_NaoVeOcorrenciaDeOutraUnidade_404()
    {
        var cenario = await PrepararAsync();

        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();

        var (nutriOutra, _) = await _factory.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista, unidadeId: outraUnidade);

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Nutricionista, "Outro", "Ocorrência da unidade A.");

        var detalhe = await nutriOutra.GetAsync($"/api/ocorrencias/{ocorrenciaId}");

        Assert.Equal(HttpStatusCode.NotFound, detalhe.StatusCode);
    }

    [Fact]
    public async Task FiltroDeEscopoForaDoAlcance_403()
    {
        var cenario = await PrepararAsync();

        var (_, outraUnidade) = _factory.CriarEscolaComUnidade();

        var resposta = await cenario.Merendeira.GetAsync(
            $"/api/ocorrencias?unidadeId={outraUnidade}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    // ---------- Status e cancelamento ----------

    [Fact]
    public async Task MudarStatus_Nutricionista_GravaHistorico()
    {
        var cenario = await PrepararAsync();

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Merendeira, "Outro", "Ocorrência para análise.");

        var mudanca = await cenario.Nutricionista.PostAsJsonAsync(
            $"/api/ocorrencias/{ocorrenciaId}/status",
            new { status = "EmAnalise", observacao = "Encaminhado à nutrição." });

        Assert.Equal(HttpStatusCode.OK, mudanca.StatusCode);

        var corpo = await mudanca.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EmAnalise", corpo.GetProperty("status").GetString());
        Assert.Equal(2, corpo.GetProperty("historico").GetArrayLength());
    }

    [Fact]
    public async Task TransicaoInvalida_409()
    {
        var cenario = await PrepararAsync();

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Merendeira, "Outro", "Ocorrência aberta.");

        var resposta = await cenario.Nutricionista.PostAsJsonAsync(
            $"/api/ocorrencias/{ocorrenciaId}/status",
            new { status = "Resolvida", observacao = "Tentativa inválida." });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Cancelar_NoMesmoDia_PeloAutor_200()
    {
        var cenario = await PrepararAsync();

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Merendeira, "Outro", "Ocorrência cancelável.");

        var resposta = await cenario.Merendeira.PostAsync(
            $"/api/ocorrencias/{ocorrenciaId}/cancelar", null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelada", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cancelar_PorNaoAutor_403()
    {
        var cenario = await PrepararAsync();

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Merendeira, "Outro", "Ocorrência de terceiro.");

        var resposta = await cenario.Nutricionista.PostAsync(
            $"/api/ocorrencias/{ocorrenciaId}/cancelar", null);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    // ---------- Contrato de erro ----------

    [Fact]
    public async Task DescricaoCurta_400ComContratoErro()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Merendeira.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Outro",
            descricao = "abc"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corpo.TryGetProperty("erro", out var erro));
        Assert.False(string.IsNullOrWhiteSpace(erro.GetString()));
    }

    [Fact]
    public async Task VinculoIncoerenteComTipo_400()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Merendeira.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Outro",
            descricao = "Ocorrência com vínculo indevido.",
            alunoId = cenario.AlunoId
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_RegistraOcorrenciaDeRefeicao_ApareceNaListagem()
    {
        var cenario = await PrepararAsync();

        var (cardapioId, refeicaoId) = CriarCardapioComRefeicao(cenario.UnidadeId);

        var resposta = await cenario.Merendeira.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Refeicao",
            descricao = "Refeição servida com atraso.",
            cardapioId,
            refeicaoId
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(cardapioId, corpo.GetProperty("cardapioId").GetInt32());
        Assert.Equal(refeicaoId, corpo.GetProperty("refeicaoId").GetInt32());
        Assert.Equal("Almoço", corpo.GetProperty("refeicao").GetString());

        var listagem = await cenario.Merendeira.GetFromJsonAsync<JsonElement>(
            "/api/ocorrencias");

        Assert.Contains(
            listagem.GetProperty("itens").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == corpo.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task RefeicaoDeOutroCardapio_400()
    {
        var cenario = await PrepararAsync();

        var (_, refeicaoId) = CriarCardapioComRefeicao(cenario.UnidadeId);
        var (outroCardapioId, _) = CriarCardapioComRefeicao(cenario.UnidadeId);

        var resposta = await cenario.Merendeira.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Refeicao",
            descricao = "Refeição de outro cardápio.",
            cardapioId = outroCardapioId,
            refeicaoId
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task AlunoExcluido_HistoricoPreservadoEVinculoAusente()
    {
        var cenario = await PrepararAsync();

        // Aluno sem vínculo, para que a exclusão seja permitida.
        var nomeOriginal = $"Aluno Snapshot {Guid.NewGuid():N}";
        var novoAlunoId = await CriarAlunoAsync(cenario.Admin, cenario.EscolaId, nomeOriginal);

        var criacao = await cenario.Nutricionista.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Aluno",
            descricao = "Aluno passou mal após o lanche.",
            alunoId = novoAlunoId
        });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);

        var corpoCriacao = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var ocorrenciaId = corpoCriacao.GetProperty("id").GetInt32();

        var exclusao = await cenario.Admin.DeleteAsync($"/api/aluno/{novoAlunoId}");
        Assert.True(exclusao.IsSuccessStatusCode);

        var detalhe = await cenario.Nutricionista.GetFromJsonAsync<JsonElement>(
            $"/api/ocorrencias/{ocorrenciaId}");

        // FK vira nula (vínculo ausente), mas o nome é preservado (snapshot RNF3/ADR-009).
        Assert.Equal(JsonValueKind.Null, detalhe.GetProperty("alunoId").ValueKind);
        Assert.Equal(nomeOriginal, detalhe.GetProperty("aluno").GetString());

        var historico = detalhe.GetProperty("historico");
        Assert.Equal(1, historico.GetArrayLength());

        var evento = historico[0];
        Assert.Equal("Aberta", evento.GetProperty("status").GetString());
        Assert.Equal("Criação", evento.GetProperty("observacao").GetString());
    }

    [Fact]
    public async Task AlunoRenomeado_DetalhePreservaNomeAntigo()
    {
        var cenario = await PrepararAsync();

        var nomeOriginal = $"Aluno Original {Guid.NewGuid():N}";
        var alunoId = await CriarAlunoAsync(cenario.Admin, cenario.EscolaId, nomeOriginal);

        var criacao = await cenario.Nutricionista.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Aluno",
            descricao = "Aluno passou mal após o lanche.",
            alunoId
        });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var ocorrenciaId = corpo.GetProperty("id").GetInt32();

        var renomeacao = await cenario.Admin.PutAsJsonAsync($"/api/aluno/{alunoId}", new
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

        var detalhe = await cenario.Nutricionista.GetFromJsonAsync<JsonElement>(
            $"/api/ocorrencias/{ocorrenciaId}");

        Assert.Equal(nomeOriginal, detalhe.GetProperty("aluno").GetString());
        Assert.Equal(alunoId, detalhe.GetProperty("alunoId").GetInt32());
    }

    [Fact]
    public async Task AdminSecretaria_NaoRegistraOcorrencia_403()
    {
        var cenario = await PrepararAsync();

        var resposta = await cenario.Admin.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Outro",
            descricao = "Tentativa indevida do ADM."
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_NaoMudaStatus_403()
    {
        var cenario = await PrepararAsync();

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Merendeira, "Outro", "Ocorrência da merendeira.");

        var resposta = await cenario.Merendeira.PostAsJsonAsync(
            $"/api/ocorrencias/{ocorrenciaId}/status",
            new { status = "EmAnalise", observacao = "Tentativa indevida." });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Responsavel_NaoMudaStatus_403()
    {
        var cenario = await PrepararAsync();

        var ocorrenciaId = await CriarOcorrenciaAsync(
            cenario.Responsavel, "Aluno", "Ocorrência do responsável.", alunoId: cenario.AlunoId);

        var resposta = await cenario.Responsavel.PostAsJsonAsync(
            $"/api/ocorrencias/{ocorrenciaId}/status",
            new { status = "EmAnalise", observacao = "Tentativa indevida." });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Cancelar_NoDiaSeguinte_409()
    {
        using var fabrica = new ApiFactoryComRelogio();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        var (_, email) = await fabrica.RegistrarAsync();
        fabrica.DefinirPapel(email, PapelUsuario.Merendeira, escolaId, unidadeId);
        var merendeira = await fabrica.EntrarAsync(email);

        var criacao = await merendeira.PostAsJsonAsync("/api/ocorrencias", new
        {
            tipo = "Outro",
            descricao = "Ocorrência criada no dia de hoje."
        });

        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        var corpo = await criacao.Content.ReadFromJsonAsync<JsonElement>();
        var ocorrenciaId = corpo.GetProperty("id").GetInt32();

        // Um dia inteiro depois: mesmo usuário (reautenticado), mas outro dia local.
        fabrica.RelogioFalso.Avancar(TimeSpan.FromDays(1));
        var merendeiraDepois = await fabrica.EntrarAsync(email);

        var resposta = await merendeiraDepois.PostAsync(
            $"/api/ocorrencias/{ocorrenciaId}/cancelar", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    private (int CardapioId, int RefeicaoId) CriarCardapioComRefeicao(int unidadeId)
    {
        using var scope = _factory.Services.CreateScope();
        var cardapios = scope.ServiceProvider.GetRequiredService<ICardapioRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var cardapio = new Cardapio(
            $"Semana {Guid.NewGuid():N}",
            unidadeId,
            new PeriodoVigencia(hoje, hoje.AddDays(6)),
            new[] { new Refeicao("Almoço", 1, Array.Empty<ItemRefeicao>()) });

        unidadeDeTrabalho.ExecutarEmTransacao(() => cardapios.Add(cardapio));

        return (cardapio.Id, cardapio.Refeicoes[0].Id);
    }
}
