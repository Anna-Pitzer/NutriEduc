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

    [Fact]
    public void FalhaNaAprovacao_DesfazPapelSolicitacaoVinculoTrilhaENotificacao()
    {
        using var fabrica = new ApiFactory();
        var (escolaId, unidadeId) = fabrica.CriarEscolaComUnidade();

        int usuarioId;
        using (var preparo = fabrica.Services.CreateScope())
        {
            var sp = preparo.ServiceProvider;
            var contexto = sp.GetRequiredService<ConnectionContext>();
            var uow = sp.GetRequiredService<IUnidadeDeTrabalho>();

            var usuario = new Usuario("Maria", "12345678901", new DateOnly(2000, 1, 1), "maria@x.com");
            uow.ExecutarEmTransacao(() => contexto.Usuarios.Add(usuario));
            usuarioId = usuario.Id;
        }

        using (var escopo = fabrica.Services.CreateScope())
        {
            var sp = escopo.ServiceProvider;
            var contexto = sp.GetRequiredService<ConnectionContext>();
            var uow = sp.GetRequiredService<IUnidadeDeTrabalho>();
            var usuario = contexto.Usuarios.First(u => u.Id == usuarioId);

            // Grava papel, solicitação, vínculo, trilha e notificação e então falha: nada pode
            // persistir (rollback transacional do caso de uso de aprovação).
            Assert.Throws<InvalidOperationException>(() =>
                uow.ExecutarEmTransacao(() =>
                {
                    usuario.DefinirPapel(PapelUsuario.Responsavel, null, unidadeId);
                    contexto.Usuarios.Update(usuario);

                    var solicitacao = new SolicitacaoDeAcesso(
                        usuarioId, TipoCadastroSolicitado.Responsavel, unidadeId, null, DateTime.UtcNow);
                    contexto.SolicitacoesDeAcesso.Add(solicitacao);

                    contexto.Vinculos.Add(new VinculoResponsavelAluno(alunoId: 1, responsavelUsuarioId: usuarioId));

                    contexto.TrilhaAcoes.Add(new TrilhaAcao(
                        "AcessoAprovado", 1, "Adm", "Usuario", usuarioId, "Responsavel", "Responsavel", DateTime.UtcNow));

                    contexto.Notificacoes.Add(new Notificacao(
                        usuarioId, TipoNotificacao.SolicitacaoStatus, "Solicitação aprovada", "Aprovada.",
                        "SolicitacaoAcesso", 1, DateTime.UtcNow));

                    throw new InvalidOperationException("Falha simulada dentro da transação.");
                }));
        }

        using (var verificacao = fabrica.Services.CreateScope())
        {
            var contexto = verificacao.ServiceProvider.GetRequiredService<ConnectionContext>();
            var usuario = contexto.Usuarios.First(u => u.Id == usuarioId);

            Assert.Null(usuario.Papel);
            Assert.Empty(contexto.SolicitacoesDeAcesso.Where(s => s.UsuarioId == usuarioId));
            Assert.Empty(contexto.Vinculos.Where(v => v.ResponsavelUsuarioId == usuarioId));
            Assert.Empty(contexto.TrilhaAcoes);
            Assert.Empty(contexto.Notificacoes);
        }
    }
}
