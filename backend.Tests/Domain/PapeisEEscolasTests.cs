using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Invariantes novas da Spec 009: escola, unidade, vínculo e o estado de autorização do
/// usuário. A regra "usuário sem papel não autentica" depende de `Papel` ser anulável.
/// </summary>
public class PapeisEEscolasTests
{
    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Escola_ComNomeCurto_Lanca(string nome)
    {
        Assert.Throws<ArgumentException>(() => new Escola(nome, 1));
    }

    [Fact]
    public void Escola_ComNomeLongo_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new Escola(new string('a', 121), 1));
    }

    [Fact]
    public void Escola_ComNomeComEspacos_Normaliza()
    {
        var escola = new Escola("  Escola Central  ", 1);

        Assert.Equal("Escola Central", escola.Nome);
        Assert.True(escola.Ativa);
    }

    [Fact]
    public void Unidade_ComNomeValido_NaoTemEscola()
    {
        var unidade = new UnidadeEscolar("Unidade A");

        Assert.Equal("Unidade A", unidade.Nome);
        Assert.True(unidade.Ativa);
        Assert.Null(unidade.EscolaId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public void Unidade_ComNomeInvalido_Lanca(string nome)
    {
        Assert.Throws<ArgumentException>(() => new UnidadeEscolar(nome));
    }

    [Fact]
    public void Usuario_AdminEscolaSemEscola_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
                "ana@exemplo.com", PapelUsuario.AdminEscola));
    }

    [Fact]
    public void Usuario_NutricionistaComEscola_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
                "ana@exemplo.com", PapelUsuario.Nutricionista, escolaId: 1));
    }

    [Fact]
    public void Usuario_SemPapelComEscola_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
                "ana@exemplo.com", papel: null, escolaId: 1));
    }

    [Fact]
    public void Usuario_MerendeiraComUnidade_Aceita()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com", PapelUsuario.Merendeira, escolaId: 1, unidadeId: 2);

        Assert.Equal(PapelUsuario.Merendeira, usuario.Papel);
        Assert.Equal(1, usuario.EscolaId);
        Assert.Equal(2, usuario.UnidadeId);
    }

    [Fact]
    public void Usuario_AdminEscolaComUnidade_Aceita()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com", PapelUsuario.AdminEscola, escolaId: 1, unidadeId: 2);

        Assert.Equal(PapelUsuario.AdminEscola, usuario.Papel);
        Assert.Equal(1, usuario.EscolaId);
        Assert.Equal(2, usuario.UnidadeId);
    }

    /// <summary>
    /// Bootstrap v2: o AdminSecretaria é global, mas pode ser atribuído a uma unidade de
    /// origem. A unidade é atribuição, não escopo de dados.
    /// </summary>
    [Fact]
    public void Usuario_AdminSecretariaComUnidade_AceitaAtribuicao()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com", PapelUsuario.AdminSecretaria, unidadeId: 2);

        Assert.Equal(PapelUsuario.AdminSecretaria, usuario.Papel);
        Assert.Null(usuario.EscolaId);
        Assert.Equal(2, usuario.UnidadeId);
    }

    [Fact]
    public void Usuario_NutricionistaComUnidade_Aceita()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com", PapelUsuario.Nutricionista, unidadeId: 2);

        Assert.Equal(PapelUsuario.Nutricionista, usuario.Papel);
        Assert.Equal(2, usuario.UnidadeId);
        Assert.Null(usuario.EscolaId);
    }

    [Fact]
    public void Usuario_NutricionistaSemUnidade_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
                "ana@exemplo.com", PapelUsuario.Nutricionista));
    }

    [Fact]
    public void Usuario_ResponsavelComUnidadeSemEscola_Aceita()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com", PapelUsuario.Responsavel, unidadeId: 2);

        Assert.Equal(PapelUsuario.Responsavel, usuario.Papel);
        Assert.Equal(2, usuario.UnidadeId);
        Assert.Null(usuario.EscolaId);
    }

    [Fact]
    public void Usuario_ResponsavelComEscola_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
                "ana@exemplo.com", PapelUsuario.Responsavel, escolaId: 1, unidadeId: 2));
    }

    [Fact]
    public void Usuario_SemPapel_NaoAutenticaComoNutricionista()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com");

        Assert.Null(usuario.Papel);
        Assert.True(usuario.Ativo);
    }

    [Fact]
    public void Usuario_DesativarEReativar_AlteraAtivo()
    {
        var usuario = new Usuario("Ana", "12345678901", new DateOnly(1990, 1, 1),
            "ana@exemplo.com", PapelUsuario.Nutricionista, unidadeId: 2);

        usuario.Desativar();
        Assert.False(usuario.Ativo);

        usuario.Reativar();
        Assert.True(usuario.Ativo);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void Vinculo_ComIdInvalido_Lanca(int alunoId, int usuarioId)
    {
        Assert.Throws<ArgumentException>(() =>
            new VinculoResponsavelAluno(alunoId, usuarioId));
    }
}
