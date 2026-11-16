using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

public class SolicitacaoDeAcessoTests
{
    private static DateTime Agora => DateTime.UtcNow;

    [Fact]
    public void Construtor_NutricionistaSemCrn_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new SolicitacaoDeAcesso(
                1, TipoCadastroSolicitado.Nutricionista, 3, null, Agora));
    }

    [Fact]
    public void Construtor_MerendeiraComCrn_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new SolicitacaoDeAcesso(
                1, TipoCadastroSolicitado.Merendeira, 3, "CRN-3-12345", Agora));
    }

    [Fact]
    public void Construtor_Nutricionista_NormalizaCrn()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Nutricionista, 3, "crn-3-12345", Agora);

        Assert.Equal("CRN-3-12345", solicitacao.CrnSolicitado);
        Assert.Equal(StatusSolicitacaoAcesso.Pendente, solicitacao.Status);
    }

    [Fact]
    public void Aprovar_PapelAdministrativo_Lanca()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Responsavel, 3, null, Agora);

        Assert.Throws<ArgumentException>(() =>
            solicitacao.Aprovar(PapelUsuario.AdminSecretaria, null, 2, "Adm", null, Agora));
    }

    [Fact]
    public void Aprovar_PapelCorrigidoSemJustificativa_Lanca()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Merendeira, 3, null, Agora);

        Assert.Throws<ArgumentException>(() =>
            solicitacao.Aprovar(PapelUsuario.Responsavel, null, 2, "Adm", null, Agora));
    }

    [Fact]
    public void Aprovar_PapelCorrigidoComJustificativa_Aceita()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Merendeira, 3, null, Agora);

        solicitacao.Aprovar(
            PapelUsuario.Responsavel, null, 2, "Adm", "Justificativa valida.", Agora);

        Assert.Equal(StatusSolicitacaoAcesso.Aprovada, solicitacao.Status);
        Assert.Equal(PapelUsuario.Responsavel, solicitacao.PapelConcedido);
        Assert.Equal("Justificativa valida.", solicitacao.JustificativaCorrecaoPapel);
    }

    [Fact]
    public void Aprovar_DepoisDeDecidido_LancaTransicao()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Responsavel, 3, null, Agora);

        solicitacao.Recusar("Motivo valido.", 2, "Adm", Agora);

        Assert.Throws<TransicaoInvalidaException>(() =>
            solicitacao.Aprovar(PapelUsuario.Responsavel, null, 2, "Adm", null, Agora));
    }

    [Fact]
    public void Recusar_SemMotivo_Lanca()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Responsavel, 3, null, Agora);

        Assert.Throws<ArgumentException>(() =>
            solicitacao.Recusar("", 2, "Adm", Agora));
    }

    [Fact]
    public void Recusar_ComMotivo_GravaEstado()
    {
        var solicitacao = new SolicitacaoDeAcesso(
            1, TipoCadastroSolicitado.Responsavel, 3, null, Agora);

        solicitacao.Recusar("Motivo valido.", 2, "Adm", Agora);

        Assert.Equal(StatusSolicitacaoAcesso.Recusada, solicitacao.Status);
        Assert.Equal("Motivo valido.", solicitacao.Motivo);
        Assert.Equal(2, solicitacao.DecididoPorId);
        Assert.Equal("Adm", solicitacao.DecididoPorNome);
    }
}
