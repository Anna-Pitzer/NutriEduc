using Ntc.Domain.Services;

namespace Ntc.Domain.Entity;

/// <summary>
/// Registro de problema relacionado a aluno, refeição ou alimento (Spec 016, RF1–RF5).
///
/// O agregado é dono dos vínculos (RF2), do ciclo de status e do histórico append-only.
/// As transições usam uma tabela explícita de pares válidos; nenhuma transição nova nasce
/// de um switch local. O cancelamento é restrito ao mesmo dia da criação (RF9).
/// </summary>
public class Ocorrencia
{
    public const int DescricaoMin = 5;
    public const int DescricaoMax = 1000;
    public const int ObservacaoMin = 2;
    public const int ObservacaoMax = 500;

    private readonly List<HistoricoOcorrencia> _historico = new();

    public int Id { get; private set; }

    public TipoOcorrencia Tipo { get; private set; }

    public int UnidadeId { get; private set; }

    public int? EscolaId { get; private set; }

    public int? AlunoId { get; private set; }

    public int? CardapioId { get; private set; }

    public int? RefeicaoId { get; private set; }

    public int? AlimentoId { get; private set; }

    /// <summary>Snapshot do nome do aluno no momento do registro (RNF3/ADR-009).</summary>
    public string? AlunoNome { get; private set; }

    /// <summary>Snapshot do nome do cardápio no momento do registro (RNF3/ADR-009).</summary>
    public string? CardapioNome { get; private set; }

    /// <summary>Snapshot do nome da refeição no momento do registro (RNF3/ADR-009).</summary>
    public string? RefeicaoNome { get; private set; }

    /// <summary>Snapshot do nome do alimento no momento do registro (RNF3/ADR-009).</summary>
    public string? AlimentoNome { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    public StatusOcorrencia Status { get; private set; }

    public int AutorId { get; private set; }

    public string AutorNome { get; private set; } = string.Empty;

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    public IReadOnlyList<HistoricoOcorrencia> Historico => _historico.AsReadOnly();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Ocorrencia()
    {
    }

    public Ocorrencia(
        TipoOcorrencia tipo,
        int unidadeId,
        int? escolaId,
        int autorId,
        string autorNome,
        string descricao,
        int? alunoId,
        int? cardapioId,
        int? refeicaoId,
        int? alimentoId,
        string? alunoNome,
        string? cardapioNome,
        string? refeicaoNome,
        string? alimentoNome,
        DateTime utc)
    {
        if (!Enum.IsDefined(tipo))
            throw new ArgumentException("Tipo de ocorrência inválido.");

        if (unidadeId <= 0)
            throw new ArgumentException("A ocorrência exige uma unidade válida.");

        if (autorId <= 0)
            throw new ArgumentException("A ocorrência exige um autor válido.");

        var texto = (descricao ?? string.Empty).Trim();
        if (texto.Length is < DescricaoMin or > DescricaoMax)
            throw new ArgumentException("A descrição deve ter entre 5 e 1000 caracteres.");

        ValidarVinculos(tipo, alunoId, cardapioId, refeicaoId, alimentoId);

        var nome = ValidacaoDeCampo.Texto(autorNome, "autor", 2, 100);

        Tipo = tipo;
        UnidadeId = unidadeId;
        EscolaId = escolaId is > 0 ? escolaId : null;
        AutorId = autorId;
        AutorNome = nome;
        Descricao = texto;
        AlunoId = alunoId is > 0 ? alunoId : null;
        CardapioId = cardapioId is > 0 ? cardapioId : null;
        RefeicaoId = refeicaoId is > 0 ? refeicaoId : null;
        AlimentoId = alimentoId is > 0 ? alimentoId : null;
        AlunoNome = NormalizarNome(alunoNome);
        CardapioNome = NormalizarNome(cardapioNome);
        RefeicaoNome = NormalizarNome(refeicaoNome);
        AlimentoNome = NormalizarNome(alimentoNome);

        Status = StatusOcorrencia.Aberta;
        CriadoEmUtc = AtualizadoEmUtc = NormalizarUtc(utc);
        _historico.Add(new HistoricoOcorrencia(
            StatusOcorrencia.Aberta, nome, "Criação", CriadoEmUtc));
    }

    public void MudarStatus(
        StatusOcorrencia destino,
        string observacao,
        string autorNome,
        DateTime utc)
    {
        var texto = (observacao ?? string.Empty).Trim();
        if (texto.Length is < ObservacaoMin or > ObservacaoMax)
            throw new ArgumentException("A observação deve ter entre 2 e 500 caracteres.");

        bool permitido = (Status, destino) switch
        {
            (StatusOcorrencia.Aberta, StatusOcorrencia.EmAnalise) => true,
            (StatusOcorrencia.EmAnalise, StatusOcorrencia.Resolvida) => true,
            (StatusOcorrencia.Resolvida, StatusOcorrencia.Arquivada) => true,
            (StatusOcorrencia.EmAnalise, StatusOcorrencia.Aberta) => true,
            _ => false
        };

        if (!permitido)
            throw new TransicaoInvalidaException("Transição de status inválida.");

        var autor = ValidacaoDeCampo.Texto(autorNome, "autor", 2, 100);

        Status = destino;
        AtualizadoEmUtc = NormalizarUtc(utc);
        _historico.Add(new HistoricoOcorrencia(destino, autor, texto, AtualizadoEmUtc));
    }

    public void Cancelar(string autorNome, DateTime utc)
    {
        if (Status != StatusOcorrencia.Aberta)
            throw new TransicaoInvalidaException("Somente ocorrência aberta pode ser cancelada.");

        var data = NormalizarUtc(utc);
        if (DiaLocal(data) != DiaLocal(CriadoEmUtc))
            throw new TransicaoInvalidaException("Cancelamento só é permitido no dia da criação.");

        var autor = ValidacaoDeCampo.Texto(autorNome, "autor", 2, 100);

        Status = StatusOcorrencia.Cancelada;
        AtualizadoEmUtc = data;
        _historico.Add(new HistoricoOcorrencia(
            StatusOcorrencia.Cancelada, autor, "Cancelada pelo autor", data));
    }

    /// <summary>
    /// Verifica que o vínculo preenchido corresponde ao tipo e que o vínculo obrigatório do
    /// tipo existe. Não consulta banco: é invariante estrutural, não existência.
    /// </summary>
    private static void ValidarVinculos(
        TipoOcorrencia tipo,
        int? alunoId,
        int? cardapioId,
        int? refeicaoId,
        int? alimentoId)
    {
        switch (tipo)
        {
            case TipoOcorrencia.Aluno:
                if (alunoId is not > 0)
                    throw new ArgumentException("A ocorrência de aluno exige o aluno.");
                if (cardapioId is not null || refeicaoId is not null || alimentoId is not null)
                    throw new ArgumentException("Vínculo incoerente com o tipo Aluno.");
                break;

            case TipoOcorrencia.Refeicao:
                if (cardapioId is not > 0 || refeicaoId is not > 0)
                    throw new ArgumentException("A ocorrência de refeição exige cardápio e refeição.");
                if (alunoId is not null || alimentoId is not null)
                    throw new ArgumentException("Vínculo incoerente com o tipo Refeicao.");
                break;

            case TipoOcorrencia.Alimento:
                if (alimentoId is not > 0)
                    throw new ArgumentException("A ocorrência de alimento exige o alimento.");
                if (alunoId is not null || cardapioId is not null || refeicaoId is not null)
                    throw new ArgumentException("Vínculo incoerente com o tipo Alimento.");
                break;

            case TipoOcorrencia.Outro:
                if (alunoId is not null || cardapioId is not null ||
                    refeicaoId is not null || alimentoId is not null)
                    throw new ArgumentException("Vínculo incoerente com o tipo Outro.");
                break;
        }
    }

    private static DateTime NormalizarUtc(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();

    private static string? NormalizarNome(string? nome) =>
        string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();

    /// <summary>Fuso que define o "mesmo dia" do cancelamento (RF9): dia civil de Brasília.</summary>
    private static readonly TimeZoneInfo FusoLocal = ResolverFusoLocal();

    private static TimeZoneInfo ResolverFusoLocal()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    private static DateOnly DiaLocal(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(NormalizarUtc(utc), FusoLocal));
}
