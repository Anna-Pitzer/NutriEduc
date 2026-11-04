using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Fronteiras da Spec 024: política de exclusão pura (F7) e tradução de escopo para filtro
/// de negócio (F5), sempre longe de sessão dentro do domínio.
/// </summary>
public class PoliticaDeExclusaoTests
{
    [Fact]
    public void Escola_SemReferencias_PodeSerExcluida()
    {
        Assert.Null(PoliticaDeExclusao.ParaEscola(temUsuarios: false, temUnidadesLegadas: false));
    }

    [Fact]
    public void Escola_ComUsuario_Impede()
    {
        var impedimento = PoliticaDeExclusao.ParaEscola(temUsuarios: true, temUnidadesLegadas: false);

        Assert.Contains("usuários vinculados", impedimento);
    }

    [Fact]
    public void Escola_ComUnidadeLegada_Impede()
    {
        var impedimento = PoliticaDeExclusao.ParaEscola(temUsuarios: false, temUnidadesLegadas: true);

        Assert.Contains("unidades com esta escola", impedimento);
    }

    [Fact]
    public void Unidade_ComAluno_Impede()
    {
        var impedimento = PoliticaDeExclusao.ParaUnidade(
            temUsuarios: false, temAlunos: true, temTokenOuConsumo: false);

        Assert.Contains("alunos vinculados", impedimento);
    }

    [Fact]
    public void Unidade_ComTokenOuConsumo_Impede()
    {
        var impedimento = PoliticaDeExclusao.ParaUnidade(
            temUsuarios: false, temAlunos: false, temTokenOuConsumo: true);

        Assert.Contains("cadastro por token", impedimento);
    }

    [Fact]
    public void Usuario_PropriaConta_Impede()
    {
        var impedimento = PoliticaDeExclusao.ParaUsuario(
            alvoId: 7,
            alvoEhAdminSecretaria: false,
            solicitanteId: 7,
            totalAdminSecretaria: 5,
            temVinculos: false,
            temTokens: false);

        Assert.Contains("própria conta", impedimento);
    }

    [Fact]
    public void Usuario_UltimoAdminSecretaria_Impede()
    {
        var impedimento = PoliticaDeExclusao.ParaUsuario(
            alvoId: 7,
            alvoEhAdminSecretaria: true,
            solicitanteId: 1,
            totalAdminSecretaria: 1,
            temVinculos: false,
            temTokens: false);

        Assert.Contains("último administrador", impedimento);
    }

    [Fact]
    public void Usuario_SemReferencias_PodeSerExcluido()
    {
        Assert.Null(PoliticaDeExclusao.ParaUsuario(
            alvoId: 7,
            alvoEhAdminSecretaria: false,
            solicitanteId: 1,
            totalAdminSecretaria: 3,
            temVinculos: false,
            temTokens: false));
    }
}

/// <summary>Tradução papel → filtro de negócio continua no lugar certo (F5).</summary>
public class FiltrosDeConsultaTests
{
    [Fact]
    public void Aluno_PapeisDeUnidade_FiltramPorUnidade()
    {
        var nutricionista = new EscopoUsuario(1, PapelUsuario.Nutricionista, null, 12);
        var secretaria = new EscopoUsuario(2, PapelUsuario.AdminSecretaria, null, 12);

        Assert.Equal(new FiltroAluno(UnidadeId: 12), FiltrosDeConsulta.ParaAluno(nutricionista));
        Assert.Equal(new FiltroAluno(UnidadeId: 12), FiltrosDeConsulta.ParaAluno(secretaria));
    }

    [Fact]
    public void Aluno_AdminEscola_FiltraPorEscola()
    {
        var escopo = new EscopoUsuario(3, PapelUsuario.AdminEscola, 9, null);

        Assert.Equal(new FiltroAluno(EscolaId: 9), FiltrosDeConsulta.ParaAluno(escopo));
    }

    [Fact]
    public void Aluno_Merendeira_FiltraPorEscola()
    {
        var escopo = new EscopoUsuario(4, PapelUsuario.Merendeira, 9, 12);

        Assert.Equal(new FiltroAluno(EscolaId: 9), FiltrosDeConsulta.ParaAluno(escopo));
    }

    [Fact]
    public void Aluno_Responsavel_FiltraPorVinculo()
    {
        var escopo = new EscopoUsuario(5, PapelUsuario.Responsavel, 9, 12);

        Assert.Equal(new FiltroAluno(ResponsavelUsuarioId: 5), FiltrosDeConsulta.ParaAluno(escopo));
    }

    [Fact]
    public void Aluno_EscopoSemReferencia_NaoDevolveNada()
    {
        var merendeiraSemUnidade = new EscopoUsuario(6, PapelUsuario.Merendeira, null, null);

        Assert.Equal(FiltroAluno.Nenhum, FiltrosDeConsulta.ParaAluno(merendeiraSemUnidade));
    }

    [Fact]
    public void Usuario_AdminEscola_RecebeRecorteDaPropriaEscola()
    {
        var escopo = new EscopoUsuario(7, PapelUsuario.AdminEscola, 9, null);
        var filtro = new FiltroUsuarios(null, null, null, null, null);

        Assert.Equal(9, FiltrosDeConsulta.ParaUsuario(filtro, escopo).EscolaId);
    }

    [Fact]
    public void Usuario_SemUnidade_NaoVeNada()
    {
        var escopo = new EscopoUsuario(8, PapelUsuario.AdminSecretaria, null, null);
        var filtro = new FiltroUsuarios(PapelUsuario.Merendeira, 4, null, true, null);

        Assert.Equal(-1, FiltrosDeConsulta.ParaUsuario(filtro, escopo).UnidadeId);
    }
}
