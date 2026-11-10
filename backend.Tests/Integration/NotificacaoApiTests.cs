using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Caixa de notificações (Spec 018, RF10–RF12): listagem restrita ao dono, marcação de leitura
/// e isolamento entre usuários (404 em vez de 403).
/// </summary>
public class NotificacaoApiTests
{
    private static int SeedNotificacao(
        ApiFactory fabrica,
        int destinatarioId,
        TipoNotificacao tipo)
    {
        using var scope = fabrica.Services.CreateScope();
        var repositorio = scope.ServiceProvider.GetRequiredService<INotificacaoRepository>();
        var unidadeDeTrabalho = scope.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();

        var notificacao = new Notificacao(
            destinatarioId,
            tipo,
            "Título válido",
            "Mensagem válida.",
            "solicitacao",
            1,
            DateTime.UtcNow);

        unidadeDeTrabalho.ExecutarEmTransacao(() => repositorio.Add(notificacao));

        return notificacao.Id;
    }

    [Fact]
    public async Task Listar_SemNotificacoes_RetornaVazia()
    {
        await using var fabrica = new ApiFactory();
        var cliente = await fabrica.CreateAuthenticatedClientAsync(PapelUsuario.Nutricionista);

        var resposta = await cliente.GetAsync("/api/notificacoes");
        resposta.EnsureSuccessStatusCode();

        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, corpo.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task MarcarLida_DeOutroUsuario_Devolve404()
    {
        await using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();
        var (clienteA, usuarioA) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista, null, unidadeId);
        var clienteB = await fabrica.CreateAuthenticatedClientAsync(
            PapelUsuario.Merendeira, escolaId, unidadeId);

        var id = SeedNotificacao(fabrica, usuarioA, TipoNotificacao.SolicitacaoStatus);

        var resposta = await clienteB.PostAsync($"/api/notificacoes/{id}/ler", null);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task LerTodas_MarcaTodasComoLidas()
    {
        await using var fabrica = new ApiFactory();
        var (_, unidadeId) = fabrica.CriarEscolaComUnidade();
        var (cliente, usuarioId) = await fabrica.CriarClienteAutenticadoAsync(
            PapelUsuario.Nutricionista, null, unidadeId);

        SeedNotificacao(fabrica, usuarioId, TipoNotificacao.SolicitacaoStatus);
        SeedNotificacao(fabrica, usuarioId, TipoNotificacao.FeedbackRespondido);

        var resposta = await cliente.PostAsync("/api/notificacoes/ler-todas", null);
        resposta.EnsureSuccessStatusCode();

        var listagem = await cliente.GetAsync("/api/notificacoes?naoLidas=true");
        var corpo = await listagem.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, corpo.GetProperty("total").GetInt32());
    }
}
