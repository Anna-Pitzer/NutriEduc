using Backend.Tests.Fakes;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Ponto único de emissão (Spec 018, RF9): destinatários duplicados no mesmo evento geram
/// uma única notificação por destinatário.
/// </summary>
public class NotificacaoServiceTests
{
    private static readonly DateTime Utc = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NotificarVarios_DestinatariosDuplicados_EmiteUmaVezPorDestinatario()
    {
        var repositorio = new NotificacaoRepositoryFake();
        var servico = new NotificacaoService(repositorio, new UnidadeDeTrabalhoFake());

        servico.NotificarVarios(
            new[] { 5, 5, 7, 5 },
            TipoNotificacao.CardapioPublicado,
            "Cardápio publicado",
            "O cardápio da semana foi publicado.",
            "cardapio",
            1,
            Utc);

        var itens = repositorio.GetItens();

        Assert.Equal(2, itens.Count);
        Assert.Equal(new[] { 5, 7 }, itens.Select(item => item.DestinatarioId).OrderBy(id => id));
    }

    [Fact]
    public void NotificarSolicitacao_SolicitanteIgualAoDecisor_NaoDuplica()
    {
        var repositorio = new NotificacaoRepositoryFake();
        var servico = new NotificacaoService(repositorio, new UnidadeDeTrabalhoFake());

        servico.NotificarSolicitacao(
            solicitanteId: 3,
            decisorId: 3,
            solicitacaoId: 10,
            TipoSolicitacao.InclusaoAlergia,
            StatusSolicitacao.Concluida,
            Utc);

        Assert.Single(repositorio.GetItens());
    }
}
