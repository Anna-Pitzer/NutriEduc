using Backend.Tests.Fakes;
using Backend.Tests.Support;
using Ntc.Application;
using Xunit;

namespace Backend.Tests.Services;

public class UsuarioServiceTests
{
    private static DadosUsuarioDTO DtoUsuario(
        string email = "maria@exemplo.com",
        string senha = "12345678")
    {
        return new DadosUsuarioDTO("Maria Silva", "12345678901", new DateOnly(2000, 1, 1), email)
        {
            Senha = senha
        };
    }

    [Fact]
    public void CriarUsuario_ComDadosValidos_Persiste()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);

        Assert.True(servico.CriarUsuario(DtoUsuario()));

        var persistido = repositorio.GetByEmail("maria@exemplo.com");

        Assert.NotNull(persistido);
        Assert.Equal(1, persistido!.Id);
        Assert.Equal("Maria Silva", persistido.Nome);
        Assert.StartsWith("pbkdf2-sha256$", persistido.Senha);
        Assert.True(persistido.SenhaConfere("12345678"));
    }

    [Fact]
    public void CriarUsuario_ComEmailDuplicado_RetornaFalse()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        Assert.False(servico.CriarUsuario(DtoUsuario()));
    }

    [Fact]
    public void CriarUsuario_ComSenhaCurta_LancaENaoPersiste()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);

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
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var valido = servico.VerificarLogin(new LoginUsuarioDTO
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
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var valido = servico.VerificarLogin(new LoginUsuarioDTO
        {
            Email = "maria@exemplo.com",
            Senha = "87654321"
        });

        Assert.False(valido);
    }

    [Fact]
    public void VerificarLogin_ComEmailInexistente_RetornaFalse()
    {
        var servico = new UsuarioService(new UsuarioRepositoryFake());

        var valido = servico.VerificarLogin(new LoginUsuarioDTO
        {
            Email = "ninguem@exemplo.com",
            Senha = "12345678"
        });

        Assert.False(valido);
    }

    [Fact]
    public void AtualizarPerfil_Existente_AtualizaDados()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);
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
        var servico = new UsuarioService(new UsuarioRepositoryFake());

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
        var servico = new UsuarioService(repositorio);
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
    public void TrocarSenha_ComSenhaCurta_RetornaFalse()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var hashOriginal = repositorio.GetByEmail("maria@exemplo.com")!.Senha;

        Assert.False(servico.TrocarSenha("maria@exemplo.com", "1234567"));
        Assert.Equal(hashOriginal, repositorio.GetByEmail("maria@exemplo.com")!.Senha);
    }

    [Fact]
    public void TrocarSenha_ComSenhaNova_Atualiza()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        Assert.True(servico.TrocarSenha("maria@exemplo.com", "abcdefgh"));

        var persistido = repositorio.GetByEmail("maria@exemplo.com")!;

        Assert.True(persistido.SenhaConfere("abcdefgh"));
        Assert.False(persistido.SenhaConfere("12345678"));
    }

    [Fact]
    public void CriarUsuario_SemSenha_LancaENaoPersiste()
    {
        var repositorio = new UsuarioRepositoryFake();
        var servico = new UsuarioService(repositorio);
        var dto = DtoUsuario();
        dto.Senha = null;

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
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var usuario = repositorio.GetByEmail("maria@exemplo.com")!;
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "senhaantiga");

        var valido = servico.VerificarLogin(new LoginUsuarioDTO
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
        var servico = new UsuarioService(repositorio);
        servico.CriarUsuario(DtoUsuario());

        var usuario = repositorio.GetByEmail("maria@exemplo.com")!;
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "senhaantiga");

        var valido = servico.VerificarLogin(new LoginUsuarioDTO
        {
            Email = "maria@exemplo.com",
            Senha = "outrasenha"
        });

        Assert.False(valido);
        Assert.Equal("senhaantiga", usuario.Senha);
    }
}
