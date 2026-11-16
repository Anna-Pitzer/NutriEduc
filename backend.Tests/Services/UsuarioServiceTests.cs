using Backend.Tests.Fakes;
using Backend.Tests.Support;
using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Xunit;

namespace Backend.Tests.Services;

public class UsuarioServiceTests
{
    /// <summary>
    /// Monta o serviço com o repositório de escolas fake. A verificação de existência de
    /// escola só acontece na criação administrativa (Spec 009); o fake mantém estes
    /// testes focados nas regras de usuário.
    /// </summary>
    private static UsuarioService CriarServico(IUsuarioRepository repositorio) =>
        new(
            repositorio,
            new EscolaRepositoryFake(),
            new UnidadeDeTrabalhoFake(),
            new AutorizacaoService(),
            new TrilhaAcaoRepositoryFake(),
            new AlunoRepositoryFake(),
            new VinculoRepositoryFake());

    /// <summary>Autenticação é caso de uso próprio desde a Spec 024 (F9).</summary>
    private static AutenticacaoService Autenticacao(IUsuarioRepository repositorio) =>
        new(repositorio, new UnidadeDeTrabalhoFake());

    private static CriarUsuarioEntrada DtoUsuario(
        string email = "maria@exemplo.com",
        string senha = "12345678")
    {
        return new CriarUsuarioEntrada(
            Nome: "Maria Silva",
            Cpf: "12345678901",
            DataNascimento: new DateOnly(2000, 1, 1),
            Email: email,
            Senha: senha,
            Telefone: null,
            Foto: null);
    }

    [Fact]
    public void CriarUsuario_ComDadosValidos_Persiste()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);

        var resultado = servico.CriarUsuario(DtoUsuario());

        Assert.Equal(ResultadoStatus.Ok, resultado.Status);
        Assert.Equal(1, resultado.Valor);

        var persistido = repositorio.GetByEmail("maria@exemplo.com");

        Assert.NotNull(persistido);
        Assert.Equal(1, persistido!.Id);
        Assert.Equal("Maria Silva", persistido.Nome);
        Assert.StartsWith("pbkdf2-sha256$", persistido.Senha);
        Assert.True(persistido.SenhaConfere("12345678"));
        Assert.Null(persistido.Papel);
    }

    [Fact]
    public void CriarUsuario_ComEmailDuplicado_RetornaInvalidoComMensagem()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var resultado = servico.CriarUsuario(DtoUsuario());

        Assert.Equal(ResultadoStatus.Invalido, resultado.Status);
        Assert.Equal("E-mail já cadastrado.", resultado.Erro);
    }

    [Fact]
    public void CriarUsuario_ComSenhaCurta_LancaENaoPersiste()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);

        Assert.Throws<ArgumentException>(() =>
        {
            servico.CriarUsuario(DtoUsuario(senha: "1234567"));
        });

        Assert.Null(repositorio.GetByEmail("maria@exemplo.com"));
    }

    [Fact]
    public void VerificarLogin_ComCredenciaisCorretas_RetornaTrue()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var valido = Autenticacao(repositorio).VerificarLogin(new LoginUsuarioDTO
        {
            Email = "maria@exemplo.com",
            Senha = "12345678"
        });

        Assert.True(valido);
    }

    [Fact]
    public void VerificarLogin_ComSenhaErrada_RetornaFalse()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var valido = Autenticacao(repositorio).VerificarLogin(new LoginUsuarioDTO
        {
            Email = "maria@exemplo.com",
            Senha = "87654321"
        });

        Assert.False(valido);
    }

    [Fact]
    public void VerificarLogin_ComEmailInexistente_RetornaFalse()
    {
        var repositorio = new UsuarioRepositoryFake();

        var valido = Autenticacao(repositorio).VerificarLogin(new LoginUsuarioDTO
        {
            Email = "ninguem@exemplo.com",
            Senha = "12345678"
        });

        Assert.False(valido);
    }

    [Fact]
    public void Autenticar_ComUsuarioSemPapel_RetornaSemPapel()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var resultado = Autenticacao(repositorio).Autenticar(
            new LoginUsuarioDTO { Email = "maria@exemplo.com", Senha = "12345678" },
            out var usuario);

        Assert.Equal(ResultadoLogin.SemPapel, resultado);
        Assert.Null(usuario);
    }

    [Fact]
    public void Autenticar_ComUsuarioDesativado_RetornaInativo()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var usuario = repositorio.GetByEmail("maria@exemplo.com")!;
        usuario.DefinirCrn("CRN-3 12345");
        usuario.DefinirPapel(PapelUsuario.Nutricionista, null, 1);
        usuario.Desativar();
        repositorio.Update(usuario);

        var resultado = Autenticacao(repositorio).Autenticar(
            new LoginUsuarioDTO { Email = "maria@exemplo.com", Senha = "12345678" },
            out _);

        Assert.Equal(ResultadoLogin.Inativo, resultado);
    }

    [Fact]
    public void Autenticar_ComPapelDefinido_RetornaOk()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var usuario = repositorio.GetByEmail("maria@exemplo.com")!;
        usuario.DefinirCrn("CRN-3 12345");
        usuario.DefinirPapel(PapelUsuario.Nutricionista, null, 1);
        repositorio.Update(usuario);

        var resultado = Autenticacao(repositorio).Autenticar(
            new LoginUsuarioDTO { Email = "maria@exemplo.com", Senha = "12345678" },
            out var autenticado);

        Assert.Equal(ResultadoLogin.Ok, resultado);
        Assert.NotNull(autenticado);
    }

    [Fact]
    public void AtualizarPerfil_Existente_AtualizaDados()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());
        var id = repositorio.GetByEmail("maria@exemplo.com")!.Id;

        var atualizado = servico.AtualizarPerfil(id, new AtualizarPerfilDTO
        {
            Nome = "João Souza",
            Email = "joao@exemplo.com",
            Telefone = "(11) 99999-9999"
        });

        Assert.NotNull(atualizado);
        Assert.Equal("João Souza", atualizado!.Nome);
        Assert.Equal("joao@exemplo.com", atualizado.Email);
        Assert.Equal("11999999999", atualizado.Telefone);
    }

    [Fact]
    public void AtualizarPerfil_Inexistente_DevolveNull()
    {
        var servico = CriarServico(new UsuarioRepositoryFake());

        var atualizado = servico.AtualizarPerfil(99, new AtualizarPerfilDTO
        {
            Nome = "João Souza",
            Email = "joao@exemplo.com"
        });

        Assert.Null(atualizado);
    }

    [Fact]
    public void AtualizarPerfil_ComEmailDeOutroUsuario_Lanca()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());
        servico.CriarUsuario(DtoUsuario(email: "outro@exemplo.com"));
        var id = repositorio.GetByEmail("maria@exemplo.com")!.Id;

        Assert.Throws<ArgumentException>(() =>
        {
            servico.AtualizarPerfil(id, new AtualizarPerfilDTO
            {
                Nome = "Maria Silva",
                Email = "outro@exemplo.com"
            });
        });

        var inalterado = repositorio.GetById(id);

        Assert.NotNull(inalterado);
        Assert.Equal("maria@exemplo.com", inalterado!.Email);
    }

    [Fact]
    public void CriarUsuario_SemSenha_LancaENaoPersiste()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        var dto = DtoUsuario() with { Senha = null };

        Assert.Throws<ArgumentException>(() =>
        {
            servico.CriarUsuario(dto);
        });

        Assert.Null(repositorio.GetByEmail("maria@exemplo.com"));
    }

    [Fact]
    public void VerificarLogin_ComSenhaLegada_AutenticaEMigra()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var usuario = repositorio.GetByEmail("maria@exemplo.com")!;
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "senhaantiga");

        var valido = Autenticacao(repositorio).VerificarLogin(new LoginUsuarioDTO
        {
            Email = "maria@exemplo.com",
            Senha = "senhaantiga"
        });

        Assert.True(valido);
        Assert.StartsWith("pbkdf2-sha256$", usuario.Senha);
        Assert.True(usuario.SenhaConfere("senhaantiga"));
    }

    [Fact]
    public void VerificarLogin_ComSenhaLegadaIncorreta_NaoMigra()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = CriarServico(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var usuario = repositorio.GetByEmail("maria@exemplo.com")!;
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "senhaantiga");

        var valido = Autenticacao(repositorio).VerificarLogin(new LoginUsuarioDTO
        {
            Email = "maria@exemplo.com",
            Senha = "outrasenha"
        });

        Assert.False(valido);
        Assert.Equal("senhaantiga", usuario.Senha);
    }

    // ------------------------------------------------------------------ CRN (Spec 031)

    private static UsuarioService CriarServico(
        IUsuarioRepository repositorio,
        IEscolaRepository escolas,
        TrilhaAcaoRepositoryFake? trilha = null) =>
        new(
            repositorio,
            escolas,
            new UnidadeDeTrabalhoFake(),
            new AutorizacaoService(),
            trilha ?? new TrilhaAcaoRepositoryFake(),
            new AlunoRepositoryFake(),
            new VinculoRepositoryFake());

    private static EscopoUsuario AdminDaUnidade(int unidadeId) =>
        new(1, PapelUsuario.AdminSecretaria, null, unidadeId);

    private static CriarUsuarioDTO DtoNutricionista(string email, string? crn, int unidadeId) =>
        new()
        {
            Nome = "Nutri Teste",
            Cpf = "12345678901",
            DataNascimento = new DateOnly(1990, 1, 1),
            Email = email,
            Senha = "12345678",
            Papel = PapelUsuario.Nutricionista,
            UnidadeId = unidadeId,
            Crn = crn
        };

    [Fact]
    public void CriarUsuarioAdministrado_CrnDuplicado_Retorna409()
    {        var repositorio = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var servico = CriarServico(repositorio, escolas);
        var escopo = AdminDaUnidade(unidade.Id);

        var primeiro = servico.CriarUsuarioAdministrado(
            DtoNutricionista("a@teste.com", "CRN-3 12345", unidade.Id), escopo);
        var duplicado = servico.CriarUsuarioAdministrado(
            DtoNutricionista("b@teste.com", "CRN-3-12345", unidade.Id), escopo);

        Assert.Equal(ResultadoStatus.Ok, primeiro.Status);
        Assert.Equal(ResultadoStatus.Conflito, duplicado.Status);
        Assert.Equal("Este CRN já está vinculado a outro usuário.", duplicado.Erro);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("CRN-12-12345")]
    [InlineData("CRN-3-12")]
    public void CriarUsuarioAdministrado_NutricionistaSemCrnValido_LancaENaoPersiste(
        string? crn)
    {
        var repositorio = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var servico = CriarServico(repositorio, escolas);

        Assert.Throws<ArgumentException>(() => servico.CriarUsuarioAdministrado(
            DtoNutricionista("invalido@teste.com", crn, unidade.Id),
            AdminDaUnidade(unidade.Id)));

        Assert.Null(repositorio.GetByEmail("invalido@teste.com"));
    }

    [Fact]
    public void CriarUsuarioAdministrado_CpfDuplicado_Retorna409()
    {
        var repositorio = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var servico = CriarServico(repositorio, escolas);
        var escopo = AdminDaUnidade(unidade.Id);

        var primeiro = servico.CriarUsuarioAdministrado(
            DtoNutricionista("a@teste.com", "CRN-3 12345", unidade.Id), escopo);
        var duplicado = servico.CriarUsuarioAdministrado(
            DtoNutricionista("b@teste.com", "CRN-3 54321", unidade.Id), escopo);

        Assert.Equal(ResultadoStatus.Ok, primeiro.Status);
        Assert.Equal(ResultadoStatus.Conflito, duplicado.Status);
        Assert.Equal("CPF já cadastrado.", duplicado.Erro);
    }

    [Fact]
    public void DiagnosticarCrn_ListaNutricionistaSemCrn()
    {
        var repositorio = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var servico = CriarServico(repositorio, escolas);

        repositorio.Add(new Usuario(
            "Com CRN", "11111111111", new DateOnly(1990, 1, 1), "com@teste.com",
            PapelUsuario.Nutricionista, null, unidade.Id, "CRN-3 12345"));

        var semCrn = new Usuario(
            "Sem CRN", "22222222222", new DateOnly(1990, 1, 1), "sem@teste.com",
            PapelUsuario.Nutricionista, null, unidade.Id, "CRN-3 12345");
        PropriedadesPrivadas.Definir(semCrn, nameof(Usuario.Crn), null);
        repositorio.Add(semCrn);

        var diagnostico = servico.DiagnosticarCrn(AdminDaUnidade(unidade.Id));

        var alvo = Assert.Single(diagnostico);
        Assert.Equal(semCrn.Id, alvo.Id);
        Assert.Equal("Sem CRN", alvo.Motivo);
    }

    [Fact]
    public void ListarNutricionistas_ExcluiSemCrnValido()
    {
        var repositorio = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var servico = CriarServico(repositorio, escolas);

        repositorio.Add(new Usuario(
            "Com CRN", "11111111111", new DateOnly(1990, 1, 1), "com@teste.com",
            PapelUsuario.Nutricionista, null, unidade.Id, "CRN-3 12345"));

        var semCrn = new Usuario(
            "Sem CRN", "22222222222", new DateOnly(1990, 1, 1), "sem@teste.com",
            PapelUsuario.Nutricionista, null, unidade.Id, "CRN-3 12345");
        PropriedadesPrivadas.Definir(semCrn, nameof(Usuario.Crn), null);
        repositorio.Add(semCrn);

        var lista = servico.ListarNutricionistas(AdminDaUnidade(unidade.Id));

        var alvo = Assert.Single(lista);
        Assert.True(alvo.CrnValido);
        Assert.Equal("CRN-3-12345", alvo.Crn);
    }

    [Fact]
    public void AtualizarUsuario_CrnAlterado_GravaTrilhaComAnteriorENovo()
    {
        var repositorio = new UsuarioRepositoryFake();
        var escolas = new EscolaRepositoryFake();
        var unidade = escolas.AddUnidade(new UnidadeEscolar("Unidade A"));
        var trilha = new TrilhaAcaoRepositoryFake();
        var servico = CriarServico(repositorio, escolas, trilha);
        var escopo = new EscopoUsuario(99, PapelUsuario.AdminSecretaria, null, unidade.Id);

        var criado = servico.CriarUsuarioAdministrado(
            DtoNutricionista("a@teste.com", "CRN-3 12345", unidade.Id), escopo);

        var atualizado = servico.AtualizarUsuario(
            criado.Valor,
            new AtualizarUsuarioDTO
            {
                Nome = "Nutri Teste",
                Email = "a@teste.com",
                Papel = PapelUsuario.Nutricionista,
                UnidadeId = unidade.Id,
                Crn = "CRN-3 54321"
            },
            escopo);

        Assert.Equal(ResultadoStatus.Ok, atualizado.Status);

        var acao = Assert.Single(
            trilha.GetAcoes(), item => item.Tipo == "CrnAlterado");

        Assert.Equal("CRN-3-12345", acao.ValorAnterior);
        Assert.Equal("CRN-3-54321", acao.ValorNovo);
        Assert.Equal(escopo.UsuarioId, acao.AutorId);
        Assert.Equal(DateTimeKind.Utc, acao.EmUtc.Kind);
    }
}
