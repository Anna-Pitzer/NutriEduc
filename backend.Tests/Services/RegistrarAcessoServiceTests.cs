using Backend.Tests.Fakes;
using Ntc.Application;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Services;

public class RegistrarAcessoServiceTests
{
    private static RegistrarAcessoService CriarServico(
        IUsuarioRepository usuarios,
        IEscolaRepository escolas,
        ISolicitacaoDeAcessoRepository solicitacoes,
        NotificacaoDeAcessoService? notificacoesDeAcesso = null)
    {
        var uow = new UnidadeDeTrabalhoFake();
        var notificacaoService = new NotificacaoService(new NotificacaoRepositoryFake(), uow);

        return new(
            usuarios,
            escolas,
            solicitacoes,
            uow,
            notificacoesDeAcesso ?? new NotificacaoDeAcessoService(usuarios, notificacaoService));
    }

    private static CadastroEntrada Dto(
        string email = "maria@exemplo.com",
        string cpf = "12345678901",
        TipoCadastroSolicitado? tipo = TipoCadastroSolicitado.Responsavel,
        int? unidadeId = 1,
        string? crn = null) =>
        new(
            "Maria Silva",
            cpf,
            new DateOnly(2000, 1, 1),
            email,
            "12345678",
            null,
            null,
            tipo,
            unidadeId,
            crn);

    private static UnidadeEscolar Unidade(IEscolaRepository escolas) =>
        escolas.AddUnidade(new UnidadeEscolar("Unidade A"));

    [Fact]
    public void Registrar_ComDadosValidos_CriaUsuarioSemPapelESolicitacaoPendente()
    {
        var usuarios = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var solicitacoes = new SolicitacaoDeAcessoRepositoryFake();
        var unidade = Unidade(escolas);
        var servico = CriarServico(usuarios, escolas, solicitacoes);

        var resultado = servico.Registrar(Dto(unidadeId: unidade.Id));

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal("Pendente", resultado.Valor!.Status);

        var usuario = usuarios.GetByEmail("maria@exemplo.com");
        Assert.NotNull(usuario);
        Assert.Null(usuario!.Papel);

        var pendente = solicitacoes.ObterPendenteDoUsuario(usuario.Id);
        Assert.NotNull(pendente);
        Assert.Equal(TipoCadastroSolicitado.Responsavel, pendente!.TipoSolicitado);
        Assert.Equal(unidade.Id, pendente.UnidadeId);
    }

    [Fact]
    public void Registrar_SemTipo_Retorna400()
    {
        var servico = CriarServico(
            new UsuarioRepositoryFake(), new EscolaRepositoryFake(),
            new SolicitacaoDeAcessoRepositoryFake());

        var resultado = servico.Registrar(Dto(tipo: null));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Registrar_NutricionistaSemCrn_Retorna400()
    {
        var escolas = new EscolaRepositoryFake();
        var unidade = Unidade(escolas);
        var servico = CriarServico(
            new UsuarioRepositoryFake(), escolas, new SolicitacaoDeAcessoRepositoryFake());

        var resultado = servico.Registrar(Dto(
            tipo: TipoCadastroSolicitado.Nutricionista, unidadeId: unidade.Id, crn: null));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Registrar_MerendeiraComCrn_Retorna400()
    {
        var escolas = new EscolaRepositoryFake();
        var unidade = Unidade(escolas);
        var servico = CriarServico(
            new UsuarioRepositoryFake(), escolas, new SolicitacaoDeAcessoRepositoryFake());

        var resultado = servico.Registrar(Dto(
            tipo: TipoCadastroSolicitado.Merendeira, unidadeId: unidade.Id, crn: "CRN-3-12345"));

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
    }

    [Fact]
    public void Registrar_EmailDuplicado_Retorna409()
    {
        var usuarios = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var solicitacoes = new SolicitacaoDeAcessoRepositoryFake();
        var unidade = Unidade(escolas);
        var servico = CriarServico(usuarios, escolas, solicitacoes);

        servico.Registrar(Dto(unidadeId: unidade.Id));
        var duplicado = servico.Registrar(Dto(cpf: "22222222222", unidadeId: unidade.Id));

        Assert.Equal(ResultadoStatus.Conflito, duplicado.Status);
        Assert.Equal("E-mail já cadastrado.", duplicado.Erro);
    }

    [Fact]
    public void Registrar_CpfDuplicado_Retorna409()
    {
        var usuarios = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var solicitacoes = new SolicitacaoDeAcessoRepositoryFake();
        var unidade = Unidade(escolas);
        var servico = CriarServico(usuarios, escolas, solicitacoes);

        servico.Registrar(Dto(unidadeId: unidade.Id));
        var duplicado = servico.Registrar(Dto(
            email: "outra@exemplo.com", unidadeId: unidade.Id));

        Assert.Equal(ResultadoStatus.Conflito, duplicado.Status);
        Assert.Equal("CPF já cadastrado.", duplicado.Erro);
    }

    [Fact]
    public void Registrar_UnidadeInexistente_Retorna404()
    {
        var servico = CriarServico(
            new UsuarioRepositoryFake(), new EscolaRepositoryFake(),
            new SolicitacaoDeAcessoRepositoryFake());

        var resultado = servico.Registrar(Dto(unidadeId: 999));

        Assert.Equal(ResultadoStatus.NaoEncontrado, resultado.Status);
    }
}
