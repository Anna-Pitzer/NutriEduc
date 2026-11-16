using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Fluxo HTTP de feedback (Spec 018): criação por papel, resposta pela nutrição, encerramento
/// pelo autor e isolamento entre unidades/escolas.
/// </summary>
public class FeedbackApiTests
{
    private static async Task<int> CriarAlunoAsync(
        HttpClient admin,
        int escolaId,
        string nome)
    {
        var resposta = await admin.PostAsJsonAsync("/api/aluno/registrar", new
        {
            nome,
            nascimento = "2015-05-10",
            escola = "Escola Teste",
            serie = "3o ano",
            telefone = "11999999999",
            observacao = "",
            restricoes = Array.Empty<string>(),
            anafilaxia = false,
            nomeResponsavel = "Contato",
            telefoneResponsavel = "11988888888",
            escolaId
        });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Merendeira_CriaFeedback_RetornaCreated()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();
        var cliente = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId, unidadeId);

        var resposta = await cliente.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Solicito revisão do cardápio",
            mensagem = "A refeição de terça não atendeu as crianças."
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Aberto", corpo.GetProperty("status").GetString());
        Assert.True(corpo.GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task Nutricionista_NaoPodeCriarFeedback()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var cliente = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Nutricionista, null, unidadeId);

        var resposta = await cliente.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Solicito revisão do cardápio",
            mensagem = "A refeição de terça não atendeu as crianças."
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Nutricionista_Responde_AutorEncerra()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();
        var merendeira = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId, unidadeId);
        var nutricionista = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Nutricionista, null, unidadeId);

        var criado = await merendeira.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Solicito revisão do cardápio",
            mensagem = "A refeição de terça não atendeu as crianças."
        });
        criado.EnsureSuccessStatusCode();
        var feedbackId = (await criado.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var respondido = await nutricionista.PostAsJsonAsync(
            $"/api/feedbacks/{feedbackId}/responder",
            new { resposta = "Ajuste realizado para a próxima semana." });
        respondido.EnsureSuccessStatusCode();
        Assert.Equal("Respondido",
            (await respondido.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("status").GetString());

        var encerrado = await merendeira.PostAsync(
            $"/api/feedbacks/{feedbackId}/encerrar", null);
        encerrado.EnsureSuccessStatusCode();
        Assert.Equal("Encerrado",
            (await encerrado.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("status").GetString());

        // Spec 035, RF23: a listagem também expõe a resposta e o autor da resposta.
        var lista = await merendeira.GetAsync("/api/feedbacks");
        lista.EnsureSuccessStatusCode();
        var item = (await lista.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("itens")[0];
        Assert.Equal("Ajuste realizado para a próxima semana.",
            item.GetProperty("resposta").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            item.GetProperty("respondidoPor").GetString()));
    }

    [Fact]
    public async Task Feedback_DeOutraUnidade_Devolve404()
    {
        await using var fabrica = new ApiFactory();
        var (escolaA, unidadeA) = fabrica.CriarEscolaComUnidade();
        var (_, unidadeB) = fabrica.CriarEscolaComUnidade();
        var merendeira = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaA, unidadeA);
        var nutricionistaB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Nutricionista, null, unidadeB);

        var criado = await merendeira.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Solicito revisão do cardápio",
            mensagem = "A refeição de terça não atendeu as crianças."
        });
        var feedbackId = (await criado.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        var resposta = await nutricionistaB.PostAsJsonAsync(
            $"/api/feedbacks/{feedbackId}/responder",
            new { resposta = "Resposta que não deveria acontecer." });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Responsavel_CriaFeedbackComAluno_PersisteAssociacao()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();
        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoId = await CriarAlunoAsync(admin, escolaId, "Aluno Feedback");
        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel, unidadeId: unidadeId);

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoId}/responsaveis/{responsavelId}", null);
        vinculo.EnsureSuccessStatusCode();

        var resposta = await responsavel.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Alimentação do meu filho",
            mensagem = "Gostaria de elogiar a refeição desta semana.",
            alunoId
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(alunoId, corpo.GetProperty("alunoId").GetInt32());
        Assert.Equal("Aluno Feedback", corpo.GetProperty("aluno").GetString());

        var lista = await responsavel.GetAsync($"/api/feedbacks?alunoId={alunoId}");
        lista.EnsureSuccessStatusCode();
        var pagina = await lista.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
        var item = pagina.GetProperty("itens")[0];
        Assert.Equal(alunoId, item.GetProperty("alunoId").GetInt32());
        Assert.Equal("Aluno Feedback", item.GetProperty("aluno").GetString());
    }

    [Fact]
    public async Task Responsavel_FeedbackComAlunoNaoVinculado_403()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();
        var admin = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.AdminSecretaria, unidadeId: unidadeId);
        var alunoVinculado = await CriarAlunoAsync(admin, escolaId, "Aluno Vinculado");
        var alunoAlheio = await CriarAlunoAsync(admin, escolaId, "Aluno Alheio");
        var (responsavel, responsavelId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Responsavel, unidadeId: unidadeId);

        var vinculo = await admin.PostAsync(
            $"/api/aluno/{alunoVinculado}/responsaveis/{responsavelId}", null);
        vinculo.EnsureSuccessStatusCode();

        var resposta = await responsavel.PostAsJsonAsync("/api/feedbacks", new
        {
            assunto = "Alimentação do meu filho",
            mensagem = "Gostaria de elogiar a refeição desta semana.",
            alunoId = alunoAlheio
        });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}
