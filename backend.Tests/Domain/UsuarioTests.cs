using Backend.Tests.Support;
using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

public class UsuarioTests
{
    private static Usuario SemSenha()
    {
        return new Usuario(
            "Maria Silva",
            "12345678901",
            new DateOnly(2000, 1, 1),
            "maria@exemplo.com");
    }

    private static Usuario Criar(string senha = "12345678")
    {
        var usuario = SemSenha();
        usuario.DefinirSenha(senha);
        return usuario;
    }

    [Fact]
    public void AtualizarPerfil_ComDadosValidos_NormalizaNomeEEmail()
    {
        var usuario = Criar();

        usuario.AtualizarPerfil("  João Souza  ", "  Joao@Exemplo.COM  ");

        Assert.Equal("João Souza", usuario.Nome);
        Assert.Equal("joao@exemplo.com", usuario.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void AtualizarPerfil_ComNomeInvalido_Lanca(string nome)
    {
        var usuario = Criar();

        Assert.Throws<ArgumentException>(() => usuario.AtualizarPerfil(nome, "joao@exemplo.com"));
    }

    [Fact]
    public void AtualizarPerfil_ComNomeAcimaDe100Caracteres_Lanca()
    {
        var usuario = Criar();

        Assert.Throws<ArgumentException>(() =>
            usuario.AtualizarPerfil(new string('a', 101), "joao@exemplo.com"));
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("@exemplo.com")]
    [InlineData("joao@")]
    public void AtualizarPerfil_ComEmailInvalido_Lanca(string email)
    {
        var usuario = Criar();

        Assert.Throws<ArgumentException>(() => usuario.AtualizarPerfil("João Souza", email));
    }

    [Fact]
    public void AtualizarPerfil_ComEmailAcimaDe150Caracteres_Lanca()
    {
        var usuario = Criar();
        var email = new string('a', 140) + "@exemplo.com";

        Assert.Throws<ArgumentException>(() => usuario.AtualizarPerfil("João Souza", email));
    }

    [Fact]
    public void AtualizarPerfil_Invalido_NaoAlteraEstado()
    {
        var usuario = Criar();

        Assert.Throws<ArgumentException>(() => usuario.AtualizarPerfil("A", "joao@exemplo.com"));

        Assert.Equal("Maria Silva", usuario.Nome);
        Assert.Equal("maria@exemplo.com", usuario.Email);
    }

    [Fact]
    public void AtualizarTelefone_ComMascara_Normaliza()
    {
        var usuario = Criar();

        usuario.AtualizarTelefone("(11) 99999-9999");

        Assert.Equal("11999999999", usuario.Telefone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AtualizarTelefone_SemValor_Limpa(string? telefone)
    {
        var usuario = Criar();
        usuario.AtualizarTelefone("11999999999");

        usuario.AtualizarTelefone(telefone);

        Assert.Null(usuario.Telefone);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void AtualizarTelefone_ComQuantidadeInvalidaDeDigitos_Lanca(string telefone)
    {
        var usuario = Criar();

        Assert.Throws<ArgumentException>(() => usuario.AtualizarTelefone(telefone));
    }

    [Fact]
    public void AtualizarFoto_ComPngValido_Aceita()
    {
        var usuario = Criar();
        var foto = "data:image/png;base64," + Convert.ToBase64String(new byte[] { 1, 2, 3 });

        usuario.AtualizarFoto(foto);

        Assert.Equal(foto, usuario.Foto);
    }

    [Theory]
    [InlineData("data:image/gif;base64,AAAA")]
    [InlineData("data:image/png;base64,")]
    [InlineData("data:image/png;base64,@@@")]
    public void AtualizarFoto_ComConteudoInvalido_Lanca(string foto)
    {
        var usuario = Criar();

        Assert.Throws<ArgumentException>(() => usuario.AtualizarFoto(foto));
    }

    [Fact]
    public void AtualizarFoto_AcimaDe5MB_Lanca()
    {
        var usuario = Criar();
        var bytes = new byte[5 * 1024 * 1024 + 1];
        var foto = "data:image/png;base64," + Convert.ToBase64String(bytes);

        Assert.Throws<ArgumentException>(() => usuario.AtualizarFoto(foto));
    }

    [Fact]
    public void AtualizarFoto_Nula_Limpa()
    {
        var usuario = Criar();
        usuario.AtualizarFoto("data:image/png;base64," + Convert.ToBase64String(new byte[] { 1 }));

        usuario.AtualizarFoto(null);

        Assert.Null(usuario.Foto);
    }

    [Fact]
    public void DefinirSenha_ComMenosDeOitoCaracteres_Lanca()
    {
        var usuario = SemSenha();

        Assert.Throws<ArgumentException>(() => usuario.DefinirSenha("1234567"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DefinirSenha_Vazia_Lanca(string senha)
    {
        var usuario = SemSenha();

        Assert.Throws<ArgumentException>(() => usuario.DefinirSenha(senha));
    }

    [Fact]
    public void DefinirSenha_Nula_Lanca()
    {
        var usuario = SemSenha();

        Assert.Throws<ArgumentException>(() => usuario.DefinirSenha(null!));
    }

    [Fact]
    public void DefinirSenha_ComValorValido_PersisteHashComPrefixo()
    {
        var usuario = SemSenha();

        usuario.DefinirSenha("abcdefgh");

        Assert.NotNull(usuario.Senha);
        Assert.StartsWith("pbkdf2-sha256$", usuario.Senha);
        Assert.NotEqual("abcdefgh", usuario.Senha);
    }

    [Fact]
    public void DefinirSenha_MesmaSenhaEmDoisUsuarios_GeraHashesDiferentes()
    {
        var primeiro = Criar(senha: "abcdefgh");
        var segundo = Criar(senha: "abcdefgh");

        Assert.NotEqual(primeiro.Senha, segundo.Senha);
    }

    [Fact]
    public void SenhaConfere_ComSenhaCorreta_RetornaTrue()
    {
        var usuario = Criar(senha: "abcdefgh");

        Assert.True(usuario.SenhaConfere("abcdefgh"));
    }

    [Fact]
    public void SenhaConfere_ComSenhaIncorreta_RetornaFalse()
    {
        var usuario = Criar(senha: "abcdefgh");

        Assert.False(usuario.SenhaConfere("abcdefgi"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SenhaConfere_ComSenhaVazia_RetornaFalse(string? senha)
    {
        var usuario = Criar(senha: "abcdefgh");

        Assert.False(usuario.SenhaConfere(senha));
    }

    [Fact]
    public void SenhaConfere_SemSenhaPersistida_RetornaFalse()
    {
        var usuario = SemSenha();

        Assert.False(usuario.SenhaConfere("abcdefgh"));
    }

    [Fact]
    public void SenhaConfere_ComHashMalformado_RetornaFalse()
    {
        var usuario = SemSenha();
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "pbkdf2-sha256$100000$YWJj");

        Assert.False(usuario.SenhaConfere("abcdefgh"));
    }

    [Fact]
    public void SenhaLegada_ComSenhaEmClaro_RetornaTrue()
    {
        var usuario = SemSenha();
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "senhaantiga");

        Assert.True(usuario.SenhaLegada("senhaantiga"));
        Assert.False(usuario.SenhaConfere("senhaantiga"));
    }

    [Fact]
    public void SenhaLegada_ComHashPersistido_RetornaFalse()
    {
        var usuario = Criar(senha: "abcdefgh");

        Assert.False(usuario.SenhaLegada("abcdefgh"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SenhaLegada_ComSenhaVazia_RetornaFalse(string? senha)
    {
        var usuario = SemSenha();
        UsuarioFabrica.DefinirSenhaEmClaro(usuario, "senhaantiga");

        Assert.False(usuario.SenhaLegada(senha));
    }
}
