using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Painel por papel (Spec 025): matriz de acesso, escopo por sessão, negativa uniforme do
/// relatório do filho e ausência de dado pessoal no painel da secretaria.
/// </summary>
public class PainelApiTests
{
    private static readonly DateOnly Dia = new(2026, 11, 2);

    private sealed record Cenario(
        int UnidadeId,
        int EscolaId,
        int AlunoComRestricaoId,
        int AlunoSemRestricaoId,
        int GeralId,
        string AlimentoComRestricao);

    private static int ObterRestricaoId(ApiFactory fabrica, string nome)
    {
        using var scope = fabrica.Services.CreateScope();
        var restricoes = scope.ServiceProvider.GetRequiredService<IRestricaoRepository>();
        return restricoes.Listar(null, false).First(restricao => restricao.Nome == nome).Id;
    }

    private static async Task<int> CriarAlimento(
        HttpClient client,
        string nome,
        IEnumerable<int>? restricaoIds = null)
    {
        var categorias = await client.GetFromJsonAsync<JsonElement>("/api/categorias-alimento");
        var categoriaId = categorias[0].GetProperty("id").GetInt32();

        var resposta = await client.PostAsJsonAsync("/api/alimentos", new
        {
            nome,
            categoriaId,
            valores = new
            {
                caloriasKcal = 130m,
                proteinasG = 2.7m,
                carboidratosG = 28.2m,
                gordurasG = 0.3m,
                fibrasG = 0.4m,
                sodioMg = 1m
            },
            restricaoIds = restricaoIds ?? Array.Empty<int>()
        });

        resposta.EnsureSuccessStatusCode();

        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();
    }

    private static int CriarAluno(
        ApiFactory fabrica,
        int unidadeId,
        int escolaId,
        string nome,
        string[] restricoes)
    {
        using var scope = fabrica.Services.CreateScope();
        var alunos = scope.ServiceProvider.GetRequiredService<IAlunoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var aluno = new Aluno(
            nome,
            new DateOnly(2015, 5, 20),
            new Turma("Escola", "5 ano"),
            "11999999999",
            null,
            restricoes,
            false,
            new Responsavel("Responsável de Teste", "11988888888"),
            escolaId);

        aluno.DefinirUnidade(unidadeId);

        unidadeDeTrabalho.ExecutarEmTransacao(() => alunos.Add(aluno));

        return aluno.Id;
    }

    private static void Vincular(ApiFactory fabrica, int alunoId, int responsavelUsuarioId)
    {
        using var scope = fabrica.Services.CreateScope();
        var vinculos = scope.ServiceProvider.GetRequiredService<IVinculoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        unidadeDeTrabalho.ExecutarEmTransacao(
            () => vinculos.Add(new VinculoResponsavelAluno(alunoId, responsavelUsuarioId)));
    }

    private static object PayloadCardapio(
        int unidadeId,
        int nutricionistaId,
        int alimentoId,
        int substitutoId) => new
    {
        nome = "Cardápio do painel",
        unidadeId,
        nutricionistaId,
        confirmarIncompatibilidades = true,
        periodo = new { inicio = "2026-11-02", fim = "2026-11-06" },
        refeicoes = new[]
        {
            new
            {
                nome = "Almoço",
                ordem = 1,
                itens = new[]
                {
                    new { alimentoId, gramas = 100m },
                    new { alimentoId = substitutoId, gramas = 50m }
                }
            }
        }
    };

    private static async Task<Cenario> PrepararAsync(
        ApiFactory fabrica,
        string[] restricoesDoAlunoComRestricao)
    {
        var (nutricionista, nutricionistaId, unidadeId) =
            await fabrica.CriarNutricionistaComUnidadeAsync();
        var escolaId = fabrica.CriarEscolaNaUnidade(unidadeId);
        var lactoseId = ObterRestricaoId(fabrica, "Lactose");

        var nomeDoLeite = $"Leite {Guid.NewGuid():N}";

        var leite = await CriarAlimento(
            nutricionista,
            nomeDoLeite,
            new[] { lactoseId });

        // A adaptação precisa de um substituto seguro para o conjunto do aluno; sem ele o
        // cardápio é recusado na criação (Spec 029).
        var substituto = await CriarAlimento(
            nutricionista,
            $"Bebida {Guid.NewGuid():N}");

        // A criação do cardápio gera a versão adaptada do conjunto de restrições dos alunos
        // já cadastrados; sem substituto configurado ela é recusada (Spec 029). Os alunos
        // entram depois da publicação para o cenário medir apenas a leitura do painel.
        var criacao = await nutricionista.PostAsJsonAsync(
            "/api/cardapios",
            PayloadCardapio(unidadeId, nutricionistaId, leite, substituto));

        if (!criacao.IsSuccessStatusCode)
        {
            throw new Xunit.Sdk.XunitException(await criacao.Content.ReadAsStringAsync());
        }

        var geralId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var publicacao = await nutricionista.PostAsync($"/api/cardapios/{geralId}/publicar", null);
        publicacao.EnsureSuccessStatusCode();

        var alunoComRestricaoId = CriarAluno(
            fabrica,
            unidadeId,
            escolaId,
            $"Aluno Com Restricao {Guid.NewGuid():N}",
            restricoesDoAlunoComRestricao);

        var alunoSemRestricaoId = CriarAluno(
            fabrica,
            unidadeId,
            escolaId,
            $"Aluno Sem Restricao {Guid.NewGuid():N}",
            Array.Empty<string>());

        return new Cenario(
            unidadeId,
            escolaId,
            alunoComRestricaoId,
            alunoSemRestricaoId,
            geralId,
            nomeDoLeite);
    }

    // ---------- Matriz de acesso ----------

    [Fact]
    public async Task Secretaria_SemSessao_Retorna401()
    {
        await using var fabrica = new ApiFactory();
        var client = fabrica.CreateClient();

        var resposta = await client.GetAsync("/api/painel/secretaria");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Secretaria_ComoNutricionista_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var client = await fabrica.CreateAuthenticatedClientAsync(PapelUsuario.Nutricionista);

        var resposta = await client.GetAsync("/api/painel/secretaria");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_ComoSecretaria_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var client = await fabrica.CreateAuthenticatedClientAsync(PapelUsuario.AdminSecretaria);

        var resposta = await client.GetAsync("/api/painel/merendeira");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Familia_ComoMerendeira_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var client = await fabrica.CreateAuthenticatedClientAsync(PapelUsuario.Merendeira);

        var resposta = await client.GetAsync("/api/painel/familia");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    // ---------- Secretaria ----------

    [Fact]
    public async Task Secretaria_DevolveContagensDaUnidadeSemDadoPessoal()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, new[] { "Lactose" });

        var client = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: cenario.UnidadeId);

        var resposta = await client.GetAsync("/api/painel/secretaria");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;

        Assert.Equal(2, raiz.GetProperty("alunos").GetInt32());
        Assert.Equal(1, raiz.GetProperty("alunosPorEscola").GetArrayLength());
        Assert.Equal(1, raiz.GetProperty("cardapiosPublicadosUltimosSeteDias").GetInt32());
        Assert.True(raiz.GetProperty("usuariosAtivosPorPapel").GetArrayLength() > 0);

        Assert.DoesNotContain("Aluno Com Restricao", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Aluno Sem Restricao", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("cpf", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("telefone", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Secretaria_IgnoraUnidadeIdDaRequisicao()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, Array.Empty<string>());

        var (_, _, outraUnidadeId) = await fabrica.CriarNutricionistaComUnidadeAsync();

        var client = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: cenario.UnidadeId);

        var corpo = await client.GetFromJsonAsync<JsonElement>(
            $"/api/painel/secretaria?unidadeId={outraUnidadeId}");

        Assert.Equal(2, corpo.GetProperty("alunos").GetInt32());
        Assert.Equal(1, corpo.GetProperty("alunosPorEscola").GetArrayLength());
    }

    // ---------- Merendeira ----------

    [Fact]
    public async Task Merendeira_SemUnidade_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var escolaId = fabrica.CriarEscolaNaUnidade(fabrica.UnidadeIdPadrao);

        var client = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: escolaId);

        var resposta = await client.GetAsync("/api/painel/merendeira");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Merendeira_VeOCardapioDaPropriaUnidadeComContagemSemNomes()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, new[] { "Lactose" });

        var client = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: cenario.EscolaId,
            unidadeId: cenario.UnidadeId);

        var resposta = await client.GetAsync(
            $"/api/painel/merendeira?data={Dia:yyyy-MM-dd}");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;

        var cardapio = raiz.GetProperty("cardapio");
        Assert.Equal(JsonValueKind.Object, cardapio.ValueKind);

        var itens = cardapio.GetProperty("refeicoes")[0].GetProperty("itens");

        var comRestricao = itens
            .EnumerateArray()
            .Single(item =>
                item.GetProperty("alimento").GetString() == cenario.AlimentoComRestricao);

        var semRestricao = itens
            .EnumerateArray()
            .Where(item =>
                item.GetProperty("alimento").GetString() != cenario.AlimentoComRestricao)
            .ToList();

        Assert.Equal(1, comRestricao.GetProperty("alunosComRestricao").GetInt32());
        Assert.All(semRestricao, item =>
            Assert.Equal(0, item.GetProperty("alunosComRestricao").GetInt32()));
        Assert.DoesNotContain("Aluno Com Restricao", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Aluno Sem Restricao", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Merendeira_DeOutraUnidade_NaoVeOCardapioAlheio()
    {
        await using var fabrica = new ApiFactory();
        await PrepararAsync(fabrica, new[] { "Lactose" });

        var (_, _, outraUnidadeId) = await fabrica.CriarNutricionistaComUnidadeAsync();
        var outraEscolaId = fabrica.CriarEscolaNaUnidade(outraUnidadeId);

        var client = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira,
            escolaId: outraEscolaId,
            unidadeId: outraUnidadeId);

        var resposta = await client.GetAsync(
            $"/api/painel/merendeira?data={Dia:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var raiz = documento.RootElement;

        Assert.Equal(JsonValueKind.Null, raiz.GetProperty("cardapio").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(raiz.GetProperty("motivo").GetString()));
    }

    // ---------- Família ----------

    [Fact]
    public async Task Familia_ListaSomenteOsFilhosVinculados()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, Array.Empty<string>());

        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        Vincular(fabrica, cenario.AlunoComRestricaoId, responsavelId);

        var resposta = await responsavel.GetAsync(
            $"/api/painel/familia?data={Dia:yyyy-MM-dd}");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(corpo);
        var filhos = documento.RootElement.GetProperty("filhos");

        Assert.Equal(1, filhos.GetArrayLength());
        Assert.Equal(cenario.AlunoComRestricaoId, filhos[0].GetProperty("id").GetInt32());
        Assert.DoesNotContain("Aluno Sem Restricao", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Familia_FilhoSemRestricao_RecebeOCardapioDoDia()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, Array.Empty<string>());

        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        Vincular(fabrica, cenario.AlunoSemRestricaoId, responsavelId);

        var resposta = await responsavel.GetAsync(
            $"/api/painel/familia?data={Dia:yyyy-MM-dd}");

        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var filho = documento.RootElement.GetProperty("filhos")[0];

        Assert.False(filho.GetProperty("requerAdaptacao").GetBoolean());
        Assert.Equal(JsonValueKind.Object, filho.GetProperty("cardapio").ValueKind);
        Assert.Equal(
            1,
            filho.GetProperty("cardapio").GetProperty("refeicoes").GetArrayLength());
    }

    [Fact]
    public async Task Familia_FilhoComRestricao_NaoRecebeOCardapioGeral()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, new[] { "Lactose" });

        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        Vincular(fabrica, cenario.AlunoComRestricaoId, responsavelId);

        var resposta = await responsavel.GetAsync(
            $"/api/painel/familia?data={Dia:yyyy-MM-dd}");

        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var filho = documento.RootElement.GetProperty("filhos")[0];

        Assert.True(filho.GetProperty("requerAdaptacao").GetBoolean());
        Assert.Equal(JsonValueKind.Null, filho.GetProperty("cardapio").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(filho.GetProperty("motivo").GetString()));
        Assert.Equal(1, filho.GetProperty("restricoes").GetArrayLength());
    }

    [Fact]
    public async Task Relatorio_DeAlunoNaoVinculado_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, new[] { "Lactose" });

        var (responsavel, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        var resposta = await responsavel.GetAsync(
            $"/api/painel/familia/{cenario.AlunoComRestricaoId}/relatorio");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Relatorio_DeIdInexistente_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, Array.Empty<string>());

        var (responsavel, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        var resposta = await responsavel.GetAsync("/api/painel/familia/999999/relatorio");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Relatorio_DoFilhoVinculado_DevolveApenasOsDadosDele()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, new[] { "Lactose" });

        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        Vincular(fabrica, cenario.AlunoComRestricaoId, responsavelId);

        var resposta = await responsavel.GetAsync(
            $"/api/painel/familia/{cenario.AlunoComRestricaoId}/relatorio" +
            $"?de={Dia:yyyy-MM-dd}&ate={Dia.AddDays(5):yyyy-MM-dd}");
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        using var documento = JsonDocument.Parse(corpo);
        var raiz = documento.RootElement;

        Assert.Equal(cenario.AlunoComRestricaoId, raiz.GetProperty("alunoId").GetInt32());
        Assert.Equal(1, raiz.GetProperty("restricoes").GetArrayLength());
        Assert.DoesNotContain("Aluno Sem Restricao", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Relatorio_ComPeriodoInvertido_Retorna400()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica, Array.Empty<string>());

        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        Vincular(fabrica, cenario.AlunoSemRestricaoId, responsavelId);

        var resposta = await responsavel.GetAsync(
            $"/api/painel/familia/{cenario.AlunoSemRestricaoId}/relatorio" +
            $"?de={Dia.AddDays(5):yyyy-MM-dd}&ate={Dia:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
