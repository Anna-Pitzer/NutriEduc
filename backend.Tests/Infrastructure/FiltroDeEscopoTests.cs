using Microsoft.EntityFrameworkCore;
using Ntc.Database;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Infrastructure.Seguranca;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Filtro de escopo traduzido para SQL (Spec 028, I4). O teste inspeciona a consulta,
/// não o resultado em memória: nenhum recorte pode acontecer depois do `ToList`.
/// </summary>
public class FiltroDeEscopoTests
{
    private static ConnectionContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<ConnectionContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new ConnectionContext(options);
    }

    [Fact]
    public void Alunos_AdminSecretaria_FiltraPorUnidadeNoSql()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(1, PapelUsuario.AdminSecretaria, null, 12);

        var sql = FiltroDeEscopo
            .Alunos(contexto.Alunos, contexto.Vinculos, escopo)
            .ToQueryString();

        Assert.Contains("\"UnidadeId\"", sql);
    }

    [Fact]
    public void Alunos_AdminEscola_FiltraPorEscolaNoSql()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(2, PapelUsuario.AdminEscola, 9, 12);

        var sql = FiltroDeEscopo
            .Alunos(contexto.Alunos, contexto.Vinculos, escopo)
            .ToQueryString();

        Assert.Contains("\"EscolaId\"", sql);
    }

    [Fact]
    public void Alunos_Responsavel_FiltraPorVinculoNoSql()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(3, PapelUsuario.Responsavel, null, 12);

        var sql = FiltroDeEscopo
            .Alunos(contexto.Alunos, contexto.Vinculos, escopo)
            .ToQueryString();

        Assert.Contains("Vinculos", sql);
        Assert.Contains("ResponsavelUsuarioId", sql);
    }

    [Fact]
    public void Alunos_EscopoSemReferencia_NaoDevolveNada()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(4, PapelUsuario.Merendeira, null, null);

        var sql = FiltroDeEscopo
            .Alunos(contexto.Alunos, contexto.Vinculos, escopo)
            .ToQueryString();

        Assert.Contains("WHERE 0", sql);
    }

    [Fact]
    public void Escolas_AdminEscola_FiltraPorIdNoSql()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(5, PapelUsuario.AdminEscola, 9, 12);

        var sql = FiltroDeEscopo.Escolas(contexto.Escolas, escopo).ToQueryString();

        Assert.Contains("\"Id\"", sql);
    }

    [Fact]
    public void Usuarios_AdminSecretaria_FiltraPorUnidadeNoSql()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(6, PapelUsuario.AdminSecretaria, null, 12);

        var sql = FiltroDeEscopo.Usuarios(contexto.Usuarios, escopo).ToQueryString();

        Assert.Contains("UnidadeId", sql);
    }

    [Fact]
    public void Cardapios_Nutricionista_FiltraPorUnidadeNoSql()
    {
        using var contexto = CriarContexto();
        var escopo = new EscopoUsuario(7, PapelUsuario.Nutricionista, null, 12);

        var sql = FiltroDeEscopo.Cardapios(contexto.Cardapios, escopo).ToQueryString();

        Assert.Contains("UnidadeId", sql);
    }
}
