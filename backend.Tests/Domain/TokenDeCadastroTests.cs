using Ntc.Domain.Entity;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Regras do token temporário do painel (Spec 022 v2): validade de 30 minutos e uso
/// único. A entidade revalida no momento do uso, porque entre a consulta e a gravação o
/// tempo pode ter passado.
/// </summary>
public class TokenDeCadastroTests
{
    private static readonly DateTime Agora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static TokenDeCadastro Criar(DateTime? criadoEm = null)
    {
        var inicio = criadoEm ?? Agora;

        return new TokenDeCadastro(
            unidadeId: 1,
            hash: "hash-de-teste",
            criadoPorUsuarioId: 2,
            criadoEm: inicio,
            expiraEm: inicio.AddMinutes(TokenDeCadastro.ValidadeEmMinutos));
    }

    [Fact]
    public void EstaValido_DentroDoPrazo_RetornaTrue()
    {
        var token = Criar();

        Assert.True(token.EstaValido(Agora.AddMinutes(29)));
    }

    [Fact]
    public void EstaValido_NoInstanteDaExpiracao_RetornaFalse()
    {
        var token = Criar();

        Assert.False(token.EstaValido(Agora.AddMinutes(TokenDeCadastro.ValidadeEmMinutos)));
    }

    [Fact]
    public void MarcarUsado_ComTokenValido_MarcaDataEUso()
    {
        var token = Criar();

        token.MarcarUsado(Agora.AddMinutes(1));

        Assert.Equal(Agora.AddMinutes(1), token.UsadoEm);
        Assert.False(token.EstaValido(Agora.AddMinutes(2)));
    }

    [Fact]
    public void MarcarUsado_ComTokenExpirado_Lanca()
    {
        var token = Criar();

        Assert.Throws<ArgumentException>(() =>
            token.MarcarUsado(Agora.AddMinutes(TokenDeCadastro.ValidadeEmMinutos)));
    }

    [Fact]
    public void MarcarUsado_DuasVezes_Lanca()
    {
        var token = Criar();
        token.MarcarUsado(Agora);

        Assert.Throws<ArgumentException>(() => token.MarcarUsado(Agora.AddMinutes(1)));
    }

    [Fact]
    public void Construtor_ComExpiracaoAnteriorACriacao_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new TokenDeCadastro(
            unidadeId: 1,
            hash: "hash",
            criadoPorUsuarioId: 2,
            criadoEm: Agora,
            expiraEm: Agora.AddMinutes(-1)));
    }

    [Fact]
    public void Construtor_SemHash_Lanca()
    {
        Assert.Throws<ArgumentException>(() => new TokenDeCadastro(
            unidadeId: 1,
            hash: " ",
            criadoPorUsuarioId: 2,
            criadoEm: Agora,
            expiraEm: Agora.AddMinutes(30)));
    }
}
