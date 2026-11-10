using Ntc.Domain.Entity;
using Ntc.Domain.Services;
using Xunit;

namespace Backend.Tests.Domain;

/// <summary>
/// Invariantes e transições da ocorrência (Spec 016): vínculo obrigatório por tipo,
/// tabela explícita de status e cancelamento restrito ao dia da criação.
/// </summary>
public class OcorrenciaTests
{
    private static readonly DateTime Utc = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static Ocorrencia Criar(
        TipoOcorrencia tipo = TipoOcorrencia.Aluno,
        string descricao = "Ocorrência de teste.",
        int? alunoId = 1,
        int? cardapioId = null,
        int? refeicaoId = null,
        int? alimentoId = null,
        int? escolaId = 20,
        string? alunoNome = "Ana",
        string? cardapioNome = null,
        string? refeicaoNome = null,
        string? alimentoNome = null,
        DateTime? criadoEm = null) =>
        new(tipo, 10, escolaId, 3, "Paulo", descricao, alunoId, cardapioId, refeicaoId, alimentoId,
            alunoNome, cardapioNome, refeicaoNome, alimentoNome, criadoEm ?? Utc);

    // ---------- Descrição ----------

    [Fact]
    public void DescricaoCurta_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(descricao: "abc"));

    [Fact]
    public void DescricaoLonga_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(descricao: new string('a', 1001)));

    // ---------- Vínculo por tipo ----------

    [Fact]
    public void Aluno_SemAlunoId_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(alunoId: null));

    [Fact]
    public void Aluno_ComOutroVinculo_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(cardapioId: 5));

    [Fact]
    public void Refeicao_SemRefeicao_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(
            TipoOcorrencia.Refeicao, cardapioId: 5, refeicaoId: null));

    [Fact]
    public void Refeicao_SemCardapio_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(
            TipoOcorrencia.Refeicao, cardapioId: null, refeicaoId: 7));

    [Fact]
    public void Alimento_SemAlimentoId_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(
            TipoOcorrencia.Alimento, alimentoId: null));

    [Fact]
    public void Outro_ComVinculo_Lanca() =>
        Assert.Throws<ArgumentException>(() => Criar(
            TipoOcorrencia.Outro, alunoId: 1));

    // ---------- Criação ----------

    [Fact]
    public void Criacao_RegistraAbertaEPrimeroEvento()
    {
        var ocorrencia = Criar();

        Assert.Equal(StatusOcorrencia.Aberta, ocorrencia.Status);
        Assert.Equal(10, ocorrencia.UnidadeId);
        Assert.Equal(20, ocorrencia.EscolaId);
        Assert.Equal(3, ocorrencia.AutorId);
        Assert.Equal(1, ocorrencia.AlunoId);
        Assert.Equal(Utc, ocorrencia.CriadoEmUtc);

        var evento = Assert.Single(ocorrencia.Historico);
        Assert.Equal(StatusOcorrencia.Aberta, evento.Status);
        Assert.Equal("Paulo", evento.AutorNome);
        Assert.Equal("Criação", evento.Observacao);
    }

    [Fact]
    public void EscolaNula_QuandoNaoInformada()
    {
        var ocorrencia = Criar(escolaId: null);

        Assert.Null(ocorrencia.EscolaId);
    }

    [Fact]
    public void Criacao_PreservaSnapshotDosNomes()
    {
        var ocorrencia = Criar(alunoNome: "Maria");

        Assert.Equal("Maria", ocorrencia.AlunoNome);
        Assert.Null(ocorrencia.CardapioNome);
        Assert.Null(ocorrencia.RefeicaoNome);
        Assert.Null(ocorrencia.AlimentoNome);
    }

    [Fact]
    public void Refeicao_PreservaNomesDoCardapioERefeicao()
    {
        var ocorrencia = Criar(
            TipoOcorrencia.Refeicao,
            alunoId: null,
            cardapioId: 5,
            refeicaoId: 7,
            alunoNome: null,
            cardapioNome: "Semana 12",
            refeicaoNome: "Almoço");

        Assert.Equal("Semana 12", ocorrencia.CardapioNome);
        Assert.Equal("Almoço", ocorrencia.RefeicaoNome);
        Assert.Null(ocorrencia.AlunoNome);
    }

    // ---------- Transições válidas ----------

    [Fact]
    public void Fluxo_Valido_AnalisaResolveArquiva()
    {
        var ocorrencia = Criar();

        ocorrencia.MudarStatus(StatusOcorrencia.EmAnalise, "Encaminhado.", "Maria", Utc.AddMinutes(1));
        ocorrencia.MudarStatus(StatusOcorrencia.Resolvida, "Resolvido.", "Maria", Utc.AddMinutes(2));
        ocorrencia.MudarStatus(StatusOcorrencia.Arquivada, "Arquivado.", "Maria", Utc.AddMinutes(3));

        Assert.Equal(StatusOcorrencia.Arquivada, ocorrencia.Status);
        Assert.Equal(
            new[]
            {
                StatusOcorrencia.Aberta,
                StatusOcorrencia.EmAnalise,
                StatusOcorrencia.Resolvida,
                StatusOcorrencia.Arquivada
            },
            ocorrencia.Historico.Select(evento => evento.Status));
    }

    [Fact]
    public void Reabrir_EmAnaliseParaAberta_Funciona()
    {
        var ocorrencia = Criar();
        ocorrencia.MudarStatus(StatusOcorrencia.EmAnalise, "Encaminhado.", "Maria", Utc.AddMinutes(1));

        ocorrencia.MudarStatus(StatusOcorrencia.Aberta, "Reaberto.", "Maria", Utc.AddMinutes(2));

        Assert.Equal(StatusOcorrencia.Aberta, ocorrencia.Status);
    }

    // ---------- Transições inválidas ----------

    [Theory]
    [InlineData(StatusOcorrencia.Resolvida)]
    [InlineData(StatusOcorrencia.Arquivada)]
    [InlineData(StatusOcorrencia.Cancelada)]
    public void Transicao_DiretaDeAberta_Lanca(StatusOcorrencia destino)
    {
        Assert.Throws<TransicaoInvalidaException>(() =>
            Criar().MudarStatus(destino, "Observação.", "Maria", Utc.AddMinutes(1)));
    }

    [Fact]
    public void Transicao_ArquivadaParaAberta_Lanca()
    {
        var ocorrencia = Criar();
        ocorrencia.MudarStatus(StatusOcorrencia.EmAnalise, "Encaminhado.", "Maria", Utc.AddMinutes(1));
        ocorrencia.MudarStatus(StatusOcorrencia.Resolvida, "Resolvido.", "Maria", Utc.AddMinutes(2));
        ocorrencia.MudarStatus(StatusOcorrencia.Arquivada, "Arquivado.", "Maria", Utc.AddMinutes(3));

        Assert.Throws<TransicaoInvalidaException>(() =>
            ocorrencia.MudarStatus(StatusOcorrencia.Aberta, "Reaberto.", "Maria", Utc.AddMinutes(4)));
    }

    [Fact]
    public void ObservacaoCurta_Lanca()
    {
        Assert.Throws<ArgumentException>(() =>
            Criar().MudarStatus(StatusOcorrencia.EmAnalise, "a", "Maria", Utc.AddMinutes(1)));
    }

    // ---------- Cancelamento ----------

    [Fact]
    public void Cancelar_NoMesmoDia_Funciona()
    {
        var ocorrencia = Criar();
        var mesmoDia = Utc.AddHours(6);

        ocorrencia.Cancelar("Paulo", mesmoDia);

        Assert.Equal(StatusOcorrencia.Cancelada, ocorrencia.Status);
    }

    [Fact]
    public void Cancelar_EmOutroDia_Lanca()
    {
        var ocorrencia = Criar();

        Assert.Throws<TransicaoInvalidaException>(() =>
            ocorrencia.Cancelar("Paulo", Utc.AddDays(1)));
    }

    [Fact]
    public void Cancelar_NaoAberta_Lanca()
    {
        var ocorrencia = Criar();
        ocorrencia.MudarStatus(StatusOcorrencia.EmAnalise, "Encaminhado.", "Maria", Utc.AddMinutes(1));

        Assert.Throws<TransicaoInvalidaException>(() =>
            ocorrencia.Cancelar("Paulo", Utc.AddHours(1)));
    }

    // ---------- Fronteira UTC × dia local (America/Sao_Paulo) ----------

    [Fact]
    public void Cancelar_MesmoDiaLocal_ComDiaUtcDiferente_Funciona()
    {
        // Criado às 23:00 UTC (20:00 em São Paulo); cancelado às 01:00 UTC do dia seguinte,
        // ainda 22:00 do mesmo dia local. O dia UTC difere, mas o dia local é o mesmo.
        var criadoEm = new DateTime(2026, 11, 10, 23, 0, 0, DateTimeKind.Utc);
        var ocorrencia = Criar(criadoEm: criadoEm);

        ocorrencia.Cancelar("Paulo", new DateTime(2026, 11, 11, 1, 0, 0, DateTimeKind.Utc));

        Assert.Equal(StatusOcorrencia.Cancelada, ocorrencia.Status);
    }

    [Fact]
    public void Cancelar_DiaLocalSeguinte_ComDiaUtcIgual_Lanca()
    {
        // Criado às 02:59 UTC (23:59 do dia anterior em São Paulo); cancelado às 03:01 UTC
        // (00:01 do dia seguinte local). O dia UTC é o mesmo, mas a meia-noite local passou.
        var criadoEm = new DateTime(2026, 11, 10, 2, 59, 0, DateTimeKind.Utc);
        var ocorrencia = Criar(criadoEm: criadoEm);

        Assert.Throws<TransicaoInvalidaException>(() =>
            ocorrencia.Cancelar("Paulo", new DateTime(2026, 11, 10, 3, 1, 0, DateTimeKind.Utc)));
    }
}
