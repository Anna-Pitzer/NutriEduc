using Microsoft.Extensions.DependencyInjection;
using Ntc.Database;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Integration;

/// <summary>
/// Garante a atomicidade da emissão (Spec 018, RNF2): uma falha dentro da transação desfaz
/// tanto a alteração de origem quanto todas as notificações, verificado por um segundo
/// contexto do banco.
/// </summary>
public class UnidadeDeTrabalhoRollbackTests
{
    [Fact]
    public void FalhaNaTransacao_DesfazOrigemENotificacao()
    {
        using var fabrica = new ApiFactory();

        using (var escopo = fabrica.Services.CreateScope())
        {
            var unidadeDeTrabalho = escopo.ServiceProvider.GetRequiredService<IUnidadeDeTrabalho>();
            var contexto = escopo.ServiceProvider.GetRequiredService<ConnectionContext>();

            Assert.Throws<InvalidOperationException>(() =>
                unidadeDeTrabalho.ExecutarEmTransacao(() =>
                {
                    contexto.Feedbacks.Add(new Feedback(
                        "Assunto do feedback",
                        "Mensagem do feedback.",
                        1,
                        "Merendeira",
                        PapelUsuario.Merendeira,
                        1,
                        1,
                        DateTime.UtcNow));

                    contexto.Notificacoes.Add(new Notificacao(
                        2,
                        TipoNotificacao.FeedbackRespondido,
                        "Feedback respondido",
                        "Seu feedback recebeu resposta.",
                        "feedback",
                        1,
                        DateTime.UtcNow));

                    throw new InvalidOperationException("Falha simulada dentro da transação.");
                }));
        }

        using (var verificacao = fabrica.Services.CreateScope())
        {
            var contexto = verificacao.ServiceProvider.GetRequiredService<ConnectionContext>();

            Assert.Equal(0, contexto.Feedbacks.Count());
            Assert.Equal(0, contexto.Notificacoes.Count());
        }
    }
}
