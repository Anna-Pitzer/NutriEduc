using Backend.Tests.Fakes;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Services;

public class AprovarAcessoServiceTests
{
    private sealed record Cenario(
        UsuarioRepositoryFake Usuarios,
        EscolaRepositoryFake Escolas,
        SolicitacaoDeAcessoRepositoryFake Solicitacoes,
        VinculoRepositoryFake Vinculos,
        TrilhaAcaoRepositoryFake Trilha,
        NotificacaoRepositoryFake Notificacoes,
        AprovarAcessoService Servico,
        int UnidadeId,
        int AdmId);

    private static Cenario CriarCenario()
    {
        var usuarios = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var solicitacoes = new SolicitacaoDeAcessoRepositoryFake();
        var vinculos = new VinculoRepositoryFake();
        var trilha = new TrilhaAcaoRepositoryFake();
        var notificacoes = new NotificacaoRepositoryFake();
        var uow = new UnidadeDeTrabalhoFake();
        var notificacaoService = new NotificacaoService(notificacoes, uow);

        var servico = new AprovarAcessoService(
            solicitacoes, usuarios, escolas, new AlunoRepositoryFake(), vinculos,
            uow, trilha, notificacaoService,
            new NotificacaoDeAcessoService(usuarios, notificacaoService));

        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var adm = new Usuario(
            "Adm", "99999999999", new DateOnly(1990, 1, 1), "adm@x.com",
            PapelUsuario.AdminSecretaria, unidadeId: unidade.Id);
        usuarios.Add(adm);

        return new Cenario(
            usuarios, escolas, solicitacoes, vinculos, trilha, notificacoes,
            servico, unidade.Id, adm.Id);
    }

    private static EscopoUsuario Adm(Cenario cenario) =>
        new(cenario.AdmId, PapelUsuario.AdminSecretaria, null, cenario.UnidadeId);

    private static (Usuario Solicitante, SolicitacaoDeAcesso Solicitacao) CriarSolicitacao(
        Cenario cenario,
        TipoCadastroSolicitado tipo,
        string? crn = null)
    {
        var solicitante = new Usuario(
            "Solicitante", "12345678901", new DateOnly(2000, 1, 1), "solicitante@x.com");
        cenario.Usuarios.Add(solicitante);

        var solicitacao = cenario.Solicitacoes.Add(new SolicitacaoDeAcesso(
            solicitante.Id, tipo, cenario.UnidadeId, crn, DateTime.UtcNow));

        return (solicitante, solicitacao);
    }

    [Fact]
    public void Aprovar_Nutricionista_GravaPapelUnidadeECrn()
    {
        var cenario = CriarCenario();
        var (solicitante, solicitacao) = CriarSolicitacao(
            cenario, TipoCadastroSolicitado.Nutricionista, "CRN-3-12345");

        var resultado = cenario.Servico.Aprovar(
            solicitacao.Id, new AprovarAcessoDTO(), Adm(cenario));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal(PapelUsuario.Nutricionista, solicitante.Papel);
        Assert.Equal(cenario.UnidadeId, solicitante.UnidadeId);
        Assert.Equal("CRN-3-12345", solicitante.Crn);
        Assert.Equal(StatusSolicitacaoAcesso.Aprovada, solicitacao.Status);
        Assert.NotEmpty(cenario.Trilha.GetAcoes());
        Assert.NotEmpty(cenario.Notificacoes.GetItens());
    }

    [Fact]
    public void Aprovar_Merendeira_EscolaDeOutraUnidade_Retorna400()
    {
        var cenario = CriarCenario();
        var (_, solicitacao) = CriarSolicitacao(cenario, TipoCadastroSolicitado.Merendeira);

        var outraUnidade = cenario.Escolas.AddUnidade(new UnidadeEscolar("Unidade B"));
        var escola = cenario.Escolas.Add(new Escola("Escola B", outraUnidade.Id));

        var resultado = cenario.Servico.Aprovar(
            solicitacao.Id, new AprovarAcessoDTO { EscolaId = escola.Id }, Adm(cenario));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Aprovar_PapelAdministrativo_RetornaProibido()
    {
        var cenario = CriarCenario();
        var (_, solicitacao) = CriarSolicitacao(cenario, TipoCadastroSolicitado.Responsavel);

        var resultado = cenario.Servico.Aprovar(
            solicitacao.Id,
            new AprovarAcessoDTO { PapelConcedido = PapelUsuario.AdminSecretaria },
            Adm(cenario));

        Assert.Equal(ResultadoStatus.Proibido, resultado.Status);
    }

    [Fact]
    public void Aprovar_PapelCorrigidoSemJustificativa_Retorna400()
    {
        var cenario = CriarCenario();
        var (_, solicitacao) = CriarSolicitacao(cenario, TipoCadastroSolicitado.Merendeira);

        var resultado = cenario.Servico.Aprovar(
            solicitacao.Id,
            new AprovarAcessoDTO { PapelConcedido = PapelUsuario.Responsavel },
            Adm(cenario));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void DecisaoRepetida_Retorna409()
    {
        var cenario = CriarCenario();
        var (_, solicitacao) = CriarSolicitacao(
            cenario, TipoCadastroSolicitado.Nutricionista, "CRN-3-12345");

        var primeira = cenario.Servico.Aprovar(
            solicitacao.Id, new AprovarAcessoDTO(), Adm(cenario));
        var segunda = cenario.Servico.Aprovar(
            solicitacao.Id, new AprovarAcessoDTO(), Adm(cenario));

        Assert.Equal(ResultadoStatus.Ok, primeira.Status);
        Assert.Equal(ResultadoStatus.Conflito, segunda.Status);
    }

    [Fact]
    public void Recusar_SemMotivo_Retorna400()
    {
        var cenario = CriarCenario();
        var (_, solicitacao) = CriarSolicitacao(cenario, TipoCadastroSolicitado.Responsavel);

        var resultado = cenario.Servico.Recusar(
            solicitacao.Id, new RecusarAcessoDTO { Motivo = "" }, Adm(cenario));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Recusar_ComMotivo_MantemSemAcesso()
    {
        var cenario = CriarCenario();
        var (solicitante, solicitacao) = CriarSolicitacao(
            cenario, TipoCadastroSolicitado.Responsavel);

        var resultado = cenario.Servico.Recusar(
            solicitacao.Id, new RecusarAcessoDTO { Motivo = "Motivo valido." }, Adm(cenario));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal(StatusSolicitacaoAcesso.Recusada, solicitacao.Status);
        Assert.Equal("Motivo valido.", solicitacao.Motivo);
        Assert.Null(solicitante.Papel);
    }

    [Fact]
    public void Fila_DeOutraUnidade_Retorna404()
    {
        var cenario = CriarCenario();
        var (_, solicitacao) = CriarSolicitacao(cenario, TipoCadastroSolicitado.Responsavel);

        var outraUnidade = cenario.Escolas.AddUnidade(new UnidadeEscolar("Unidade C"));
        var admDeOutra = new EscopoUsuario(
            cenario.AdmId, PapelUsuario.AdminSecretaria, null, outraUnidade.Id);

        var resultado = cenario.Servico.Obter(solicitacao.Id, admDeOutra);

        Assert.Equal(ResultadoStatus.NaoEncontrado, resultado.Status);
    }
}
