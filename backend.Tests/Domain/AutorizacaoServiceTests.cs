using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Regra de decisão do serviço de autorização (Spec 028, T3): matriz + fronteiras de
/// unidade, escola e vínculo. Sem HTTP e sem IO.
/// </summary>
public class AutorizacaoServiceTests
{
    private readonly AutorizacaoService _autorizacao = new();

    private static EscopoUsuario AdminSecretaria(int unidadeId = 12) =>
        new(1, PapelUsuario.AdminSecretaria, null, unidadeId);

    private static EscopoUsuario AdminEscola(int escolaId = 9, int unidadeId = 12) =>
        new(2, PapelUsuario.AdminEscola, escolaId, unidadeId);

    private static EscopoUsuario Nutricionista(int unidadeId = 12) =>
        new(3, PapelUsuario.Nutricionista, null, unidadeId);

    private static EscopoUsuario Responsavel(int usuarioId = 4) =>
        new(usuarioId, PapelUsuario.Responsavel, null, 12);

    [Fact]
    public void MatrizNegada_DevolveProibido()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            Nutricionista(),
            OperacaoDeAcesso.Criar,
            new AlvoDeEscopo(12, 9));

        Assert.False(decisao.Permitido);
        Assert.Equal(ResultadoStatus.Proibido, decisao.Status);
    }

    [Fact]
    public void AlvoDeOutraUnidade_DevolveNaoEncontrado()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            AdminSecretaria(),
            OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(13, 9));

        Assert.False(decisao.Permitido);
        Assert.Equal(ResultadoStatus.NaoEncontrado, decisao.Status);
    }

    [Fact]
    public void OutraEscolaDaMesmaUnidade_DevolveProibido()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            AdminEscola(),
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(12, 10));

        Assert.False(decisao.Permitido);
        Assert.Equal(ResultadoStatus.Proibido, decisao.Status);
    }

    [Fact]
    public void ResponsavelSemVinculo_DevolveProibido()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            Responsavel(),
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(12, 9, VinculadoAoResponsavel: false));

        Assert.False(decisao.Permitido);
        Assert.Equal(ResultadoStatus.Proibido, decisao.Status);
    }

    [Fact]
    public void ResponsavelComVinculo_LeAluno()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            Responsavel(),
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(12, 9, VinculadoAoResponsavel: true));

        Assert.True(decisao.Permitido);
    }

    [Fact]
    public void Solicitar_NaoPermiteEscritaDireta()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            Responsavel(),
            OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(12, 9, VinculadoAoResponsavel: true));

        Assert.False(decisao.Permitido);
        Assert.Equal(ResultadoStatus.Proibido, decisao.Status);
    }

    [Fact]
    public void AdminSecretaria_NaPropriaUnidade_EscreveAluno()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.Aluno,
            AdminSecretaria(),
            OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(12, 9));

        Assert.True(decisao.Permitido);
    }

    [Fact]
    public void CatalogoCompartilhado_LeituraEmQualquerEscopo()
    {
        var decisao = _autorizacao.Avaliar(
            Recurso.CatalogoDeAlimentos,
            AdminEscola(),
            OperacaoDeAcesso.Ler,
            AlvoDeEscopo.SemFronteira);

        Assert.True(decisao.Permitido);
    }
}
