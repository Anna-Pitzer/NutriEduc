using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Marcação de anafilaxia na análise de uma solicitação de inclusão de alergia
/// (Spec 017/023): grava no aluno, entra na trilha com a origem da solicitação e é
/// idempotente. Papéis sem decisão recebem 403 e tipos diferentes recebem 400.
/// </summary>
public class SolicitacaoAnafilaxiaApiTests
{
    private sealed record Cenario(
        HttpClient Admin,
        HttpClient Responsavel,
        int UnidadeId,
        int AlunoId,
        int SolicitacaoId);

    private static async Task<Cenario> PrepararAsync(ApiFactory fabrica)
    {
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria,
            unidadeId: unidadeId);
        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: unidadeId);

        var criacao = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome = $"Aluno Anafilaxia {Guid.NewGuid():N}",
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

        criacao.EnsureSuccessStatusCode();

        var alunoId = (await criacao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}",
            null);
        vinculo.EnsureSuccessStatusCode();

        var pedido = await responsavel.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "InclusaoAlergia",
            alunoId,
            restricaoNome = "Lactose",
            descricao = "Reação alérgica após o lanche."
        });

        pedido.EnsureSuccessStatusCode();

        var solicitacaoId = (await pedido.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        return new Cenario(admin, responsavel, unidadeId, alunoId, solicitacaoId);
    }

    private static List<HistoricoAluno> HistoricoDoAluno(ApiFactory fabrica, int alunoId)
    {
        using var scope = fabrica.Services.CreateScope();
        var historico = scope.ServiceProvider.GetRequiredService<IHistoricoAlunoRepository>();
        var (itens, _) = historico.Listar(alunoId, null, 1, 100);

        return itens;
    }

    [Fact]
    public async Task MarcarAnafilaxia_NaInclusaoDeAlergia_RegistraNoAlunoEHistorico()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var resposta = await cenario.Admin.PostAsync(
            $"/api/solicitacoes/{cenario.SolicitacaoId}/anafilaxia",
            null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var perfil = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/perfil/{cenario.AlunoId}");

        Assert.True(perfil.GetProperty("anafilaxia").GetBoolean());

        var historico = HistoricoDoAluno(fabrica, cenario.AlunoId);
        var evento = Assert.Single(historico, item => item.Campo == "Anafilaxia");

        Assert.Equal("Sim", evento.ValorNovo);
        Assert.Equal($"Solicitacao:{cenario.SolicitacaoId}", evento.Origem);

        // Idempotente: uma segunda marcação não duplica o evento na trilha.
        var repetida = await cenario.Admin.PostAsync(
            $"/api/solicitacoes/{cenario.SolicitacaoId}/anafilaxia",
            null);

        repetida.EnsureSuccessStatusCode();

        Assert.Single(
            HistoricoDoAluno(fabrica, cenario.AlunoId),
            item => item.Campo == "Anafilaxia");
    }

    [Fact]
    public async Task MarcarAnafilaxia_PorNutricionistaDaUnidade_Retorna200()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var (nutricionista, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            unidadeId: cenario.UnidadeId);

        var resposta = await nutricionista.PostAsync(
            $"/api/solicitacoes/{cenario.SolicitacaoId}/anafilaxia",
            null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var perfil = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/perfil/{cenario.AlunoId}");

        Assert.True(perfil.GetProperty("anafilaxia").GetBoolean());
    }

    [Fact]
    public async Task MarcarAnafilaxia_PorResponsavelSolicitante_Retorna200()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var resposta = await cenario.Responsavel.PostAsync(
            $"/api/solicitacoes/{cenario.SolicitacaoId}/anafilaxia",
            null);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var perfil = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/perfil/{cenario.AlunoId}");

        Assert.True(perfil.GetProperty("anafilaxia").GetBoolean());
    }

    [Fact]
    public async Task MarcarAnafilaxia_PorResponsavelSemVinculo_Retorna403()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var (outroResponsavel, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel,
            unidadeId: cenario.UnidadeId);

        var resposta = await outroResponsavel.PostAsync(
            $"/api/solicitacoes/{cenario.SolicitacaoId}/anafilaxia",
            null);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task MarcarAnafilaxia_EmTipoDiferente_Retorna400()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var (nutricionista, _) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista,
            unidadeId: fabrica.UnidadeIdPadrao);

        var pedido = await nutricionista.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "Ingrediente",
            ingrediente = "Arroz",
            descricao = "Solicito arroz para o estoque."
        });

        pedido.EnsureSuccessStatusCode();

        var solicitacaoId = (await pedido.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetInt32();

        var resposta = await cenario.Admin.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/anafilaxia",
            null);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task SolicitacaoComAnafilaxia_Aprovada_MarcaOAluno()
    {
        await using var fabrica = new ApiFactory();
        var cenario = await PrepararAsync(fabrica);

        var pedido = await cenario.Responsavel.PostAsJsonAsync("/api/solicitacoes", new
        {
            tipo = "InclusaoAlergia",
            alunoId = cenario.AlunoId,
            restricaoNome = "Amendoim",
            descricao = "Reação grave após o lanche.",
            anafilaxia = true
        });

        pedido.EnsureSuccessStatusCode();

        var corpo = await pedido.Content.ReadFromJsonAsync<JsonElement>();
        var solicitacaoId = corpo.GetProperty("id").GetInt32();

        Assert.True(corpo.GetProperty("anafilaxia").GetBoolean());

        var analise = await cenario.Admin.PostAsync(
            $"/api/solicitacoes/{solicitacaoId}/analise",
            null);
        analise.EnsureSuccessStatusCode();

        var aprovacao = await cenario.Admin.PostAsJsonAsync(
            $"/api/solicitacoes/{solicitacaoId}/aprovar",
            new { resposta = "Confirmado com a família." });
        aprovacao.EnsureSuccessStatusCode();

        var perfil = await cenario.Admin.GetFromJsonAsync<JsonElement>(
            $"/api/aluno/perfil/{cenario.AlunoId}");

        Assert.True(perfil.GetProperty("anafilaxia").GetBoolean());
    }
}
