using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Invariantes e transições da solicitação (Spec 017): campos obrigatórios por tipo,
/// tabela explícita de status e resposta obrigatória nas decisões.
/// </summary>
public class SolicitacaoTests
{
    private static readonly DateTime Utc = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static Solicitacao Criar(
        TipoSolicitacao tipo = TipoSolicitacao.InclusaoAlergia,
        int? alunoId = 1,
        int? cardapioId = null,
        string? restricao = "Lactose",
        string? ingrediente = null,
        string? alunoNome = "Ana",
        string? cardapioNome = null,
        string descricao = "Pedido de correção cadastral.") =>
        new(
            tipo,
            10,
            20,
            3,
            "Paulo",
            descricao,
            alunoId,
            cardapioId,
            restricao,
            ingrediente,
            alunoNome,
            cardapioNome,
            Utc);

    [Fact]
    public void Restricao_SemAluno_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(alunoId: null));
    }

    [Fact]
    public void Restricao_SemNome_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(restricao: null));
    }

    [Fact]
    public void MudancaCardapio_SemCardapio_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(
            TipoSolicitacao.MudancaCardapio,
            alunoId: null,
            restricao: null));
    }

    [Fact]
    public void Ingrediente_SemIngrediente_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(
            TipoSolicitacao.Ingrediente,
            alunoId: null,
            restricao: null,
            ingrediente: null));
    }

    [Fact]
    public void DescricaoCurta_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Criar(descricao: "abc"));
    }

    [Fact]
    public void Criacao_RegistraEstadoAbertoEPrimeiroEvento()
    {
        var solicitacao = Criar();

        Assert.Equal(StatusSolicitacao.Aberta, solicitacao.Status);
        Assert.Equal(10, solicitacao.UnidadeId);
        Assert.Equal(20, solicitacao.EscolaId);
        Assert.Equal(3, solicitacao.SolicitanteId);
        Assert.Equal(Utc, solicitacao.CriadoEmUtc);

        var evento = Assert.Single(solicitacao.Historico);
        Assert.Equal(StatusSolicitacao.Aberta, evento.Status);
        Assert.Equal("Paulo", evento.AutorNome);
    }

    [Fact]
    public void Criacao_PreservaSnapshotDosNomes()
    {
        var solicitacao = Criar(alunoNome: "Ana", cardapioNome: null);

        Assert.Equal("Ana", solicitacao.AlunoNome);
        Assert.Null(solicitacao.CardapioNome);
    }

    [Fact]
    public void MudancaCardapio_PreservaNomeDoCardapio()
    {
        var solicitacao = Criar(
            TipoSolicitacao.MudancaCardapio,
            alunoId: null,
            cardapioId: 5,
            restricao: null,
            alunoNome: null,
            cardapioNome: "Semana 12");

        Assert.Equal("Semana 12", solicitacao.CardapioNome);
        Assert.Null(solicitacao.AlunoNome);
    }

    [Fact]
    public void Fluxo_Valido_AnalisaAprovaConclui()
    {
        var solicitacao = Criar();

        solicitacao.IniciarAnalise("Maria", Utc.AddMinutes(1));
        solicitacao.Aprovar("Confirmado com a família.", "Maria", Utc.AddMinutes(2));
        solicitacao.Concluir("Maria", Utc.AddMinutes(3));

        Assert.Equal(StatusSolicitacao.Concluida, solicitacao.Status);
        Assert.Equal("Maria", solicitacao.RespondidoPor);
        Assert.Equal(
            new[]
            {
                StatusSolicitacao.Aberta,
                StatusSolicitacao.EmAnalise,
                StatusSolicitacao.Aprovada,
                StatusSolicitacao.Concluida
            },
            solicitacao.Historico.Select(evento => evento.Status));
    }

    [Fact]
    public void Cancelar_ApenasEmAberta()
    {
        var solicitacao = Criar();
        solicitacao.Cancelar("Paulo", Utc.AddMinutes(1));
        Assert.Equal(StatusSolicitacao.Cancelada, solicitacao.Status);

        var outra = Criar();
        outra.IniciarAnalise("Maria", Utc.AddMinutes(1));

        Assert.Throws<TransicaoInvalidaException>(() =>
            outra.Cancelar("Paulo", Utc.AddMinutes(2)));
    }

    [Fact]
    public void Aprovar_SemPassarPorAnalise_Lanca()
    {
        Assert.Throws<TransicaoInvalidaException>(() =>
            Criar().Aprovar("Confirmado.", "Maria", Utc.AddMinutes(1)));
    }

    [Fact]
    public void Aprovar_SemResposta_Lanca()
    {
        var solicitacao = Criar();
        solicitacao.IniciarAnalise("Maria", Utc.AddMinutes(1));

        Assert.Throws<ArgumentException>(() =>
            solicitacao.Aprovar(string.Empty, "Maria", Utc.AddMinutes(2)));
    }

    [Fact]
    public void Recusar_ComRespostaCurta_Lanca()
    {
        var solicitacao = Criar();
        solicitacao.IniciarAnalise("Maria", Utc.AddMinutes(1));

        Assert.Throws<ArgumentException>(() =>
            solicitacao.Recusar("n", "Maria", Utc.AddMinutes(2)));
    }

    [Fact]
    public void DadosCadastrais_SemAlteracoes_Lanca()
    {
        var solicitacao = new Solicitacao(
            TipoSolicitacao.DadosCadastrais,
            10,
            20,
            3,
            "Paulo",
            "Correção solicitada pela família.",
            1,
            null,
            null,
            null,
            null,
            null,
            Utc);

        Assert.Throws<ArgumentException>(() =>
            solicitacao.DefinirAlteracoesDeDados(Array.Empty<AlteracaoSolicitada>()));
    }

    [Fact]
    public void DadosCadastrais_CampoRepetido_Lanca()
    {
        var solicitacao = new Solicitacao(
            TipoSolicitacao.DadosCadastrais,
            10,
            20,
            3,
            "Paulo",
            "Correção solicitada pela família.",
            1,
            null,
            null,
            null,
            null,
            null,
            Utc);

        var alteracoes = new[]
        {
            new AlteracaoSolicitada(CampoDeDadoDoAluno.Nome, "Ana Maria", "Ana"),
            new AlteracaoSolicitada(CampoDeDadoDoAluno.Nome, "Ana Maria", "Ana")
        };

        Assert.Throws<ArgumentException>(() =>
            solicitacao.DefinirAlteracoesDeDados(alteracoes));
    }

    [Fact]
    public void DadosCadastrais_ComAlteracoes_RegistraSnapshot()
    {
        var solicitacao = new Solicitacao(
            TipoSolicitacao.DadosCadastrais,
            10,
            20,
            3,
            "Paulo",
            "Correção solicitada pela família.",
            1,
            null,
            null,
            null,
            null,
            null,
            Utc);

        solicitacao.DefinirAlteracoesDeDados(new[]
        {
            new AlteracaoSolicitada(CampoDeDadoDoAluno.Nome, "Ana Maria", "Ana"),
            new AlteracaoSolicitada(
                CampoDeDadoDoAluno.TelefoneResponsavel,
                "11977776666",
                "11988888888")
        });

        Assert.Equal(2, solicitacao.Alteracoes.Count);
        Assert.Equal(
            "Ana",
            solicitacao.Alteracoes
                .Single(alteracao => alteracao.Campo == CampoDeDadoDoAluno.Nome)
                .ValorNoPedido);
    }
}
