using Ntc.Domain.Services;

namespace Ntc.Domain.Entity;

/// <summary>
/// Solicitação de cardápio, ingrediente, restrição ou dados cadastrais (Spec 017, RF1–RF5;
/// Spec 030 para o tipo <c>DadosCadastrais</c>).
///
/// O agregado é dono do ciclo de status e do histórico append-only. As transições usam uma
/// tabela explícita de pares válidos; nenhuma transição nova nasce de um `switch` local. O
/// efeito da aprovação é decidido e executado pelo serviço, nunca dentro do agregado.
/// </summary>
public class Solicitacao
{
    public const int DescricaoMin = 5;
    public const int DescricaoMax = 1000;
    public const int RespostaMin = 2;
    public const int RespostaMax = 500;

    private static readonly IReadOnlyDictionary<StatusSolicitacao, StatusSolicitacao[]> TransicoesPermitidas =
        new Dictionary<StatusSolicitacao, StatusSolicitacao[]>
        {
            [StatusSolicitacao.Aberta] = new[]
            {
                StatusSolicitacao.EmAnalise,
                StatusSolicitacao.Cancelada
            },
            [StatusSolicitacao.EmAnalise] = new[]
            {
                StatusSolicitacao.Aprovada,
                StatusSolicitacao.Recusada
            },
            [StatusSolicitacao.Aprovada] = new[]
            {
                StatusSolicitacao.Concluida
            }
        };

    private readonly List<HistoricoSolicitacao> _historico = new();
    private readonly List<SolicitacaoAlteracao> _alteracoes = new();

    public int Id { get; private set; }

    public TipoSolicitacao Tipo { get; private set; }

    public int UnidadeId { get; private set; }

    public int? EscolaId { get; private set; }

    public int? AlunoId { get; private set; }

    public int? CardapioId { get; private set; }

    /// <summary>Snapshot do nome do aluno no momento do pedido (RNF3/ADR-009).</summary>
    public string? AlunoNome { get; private set; }

    /// <summary>Snapshot do nome do cardápio no momento do pedido (RNF3/ADR-009).</summary>
    public string? CardapioNome { get; private set; }

    public string? RestricaoNome { get; private set; }

    /// <summary>
    /// Indica que o solicitante informou reação anafilática para a restrição pedida
    /// (Spec 035, revisão). Só tem efeito em <c>InclusaoAlergia</c>/<c>AlteracaoRestricao</c>
    /// e é aplicado ao aluno na aprovação.
    /// </summary>
    public bool Anafilaxia { get; private set; }

    public string? Ingrediente { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    public StatusSolicitacao Status { get; private set; }

    public int SolicitanteId { get; private set; }

    public string SolicitanteNome { get; private set; } = string.Empty;

    public string? Resposta { get; private set; }

    public string? RespondidoPor { get; private set; }

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    public IReadOnlyList<HistoricoSolicitacao> Historico => _historico.AsReadOnly();

    /// <summary>Alterações do tipo <c>DadosCadastrais</c> (Spec 030, RF2).</summary>
    public IReadOnlyList<SolicitacaoAlteracao> Alteracoes => _alteracoes.AsReadOnly();

    private Solicitacao()
    {
    }

    public Solicitacao(
        TipoSolicitacao tipo,
        int unidadeId,
        int? escolaId,
        int solicitanteId,
        string? solicitanteNome,
        string? descricao,
        int? alunoId,
        int? cardapioId,
        string? restricaoNome,
        string? ingrediente,
        string? alunoNome,
        string? cardapioNome,
        DateTime utc,
        bool anafilaxia = false)
    {
        if (!Enum.IsDefined(tipo))
            throw new ArgumentException("Tipo de solicitação inválido.");

        if (unidadeId <= 0)
            throw new ArgumentException("A solicitação exige uma unidade válida.");

        if (solicitanteId <= 0)
            throw new ArgumentException("A solicitação exige um solicitante válido.");

        var nome = ValidacaoDeCampo.Texto(solicitanteNome, "solicitante", 2, 100);
        var texto = ValidacaoDeCampo.Texto(descricao, "descrição", DescricaoMin, DescricaoMax);

        Tipo = tipo;
        UnidadeId = unidadeId;
        EscolaId = escolaId is > 0 ? escolaId : null;
        SolicitanteId = solicitanteId;
        SolicitanteNome = nome;
        Descricao = texto;

        switch (tipo)
        {
            case TipoSolicitacao.MudancaCardapio:
                if (cardapioId is not > 0)
                    throw new ArgumentException("A solicitação de mudança de cardápio exige o cardápio.");
                CardapioId = cardapioId;
                break;

            case TipoSolicitacao.InclusaoAlergia:
            case TipoSolicitacao.AlteracaoRestricao:
                if (alunoId is not > 0)
                    throw new ArgumentException("A solicitação de restrição exige o aluno.");
                AlunoId = alunoId;
                RestricaoNome = ValidacaoDeCampo.Texto(restricaoNome, "restrição", 2, 60);
                Anafilaxia = anafilaxia;
                break;

            case TipoSolicitacao.Ingrediente:
            case TipoSolicitacao.FaltaIngrediente:
                Ingrediente = ValidacaoDeCampo.Texto(ingrediente, "ingrediente", 2, 120);
                break;

            case TipoSolicitacao.DadosCadastrais:
                // O payload e a validação de campos são da Spec 030. O agregado só garante
                // o alvo e o fluxo; a criação desse tipo é rejeitada até o serviço da 030
                // existir.
                if (alunoId is not > 0)
                    throw new ArgumentException("A solicitação de dados cadastrais exige o aluno.");
                AlunoId = alunoId;
                break;
        }

        AlunoNome = NormalizarNome(alunoNome);
        CardapioNome = NormalizarNome(cardapioNome);

        Status = StatusSolicitacao.Aberta;
        CriadoEmUtc = AtualizadoEmUtc = NormalizarUtc(utc);
        _historico.Add(new HistoricoSolicitacao(Id, Status, nome, null, CriadoEmUtc));
    }

    /// <summary>
    /// Anexa as alterações do tipo <c>DadosCadastrais</c> logo após a construção, antes de
    /// persistir (Spec 030, RF2). Só existe uma chamada por solicitação e o tipo é fechado.
    /// </summary>
    internal void DefinirAlteracoesDeDados(IEnumerable<AlteracaoSolicitada> alteracoes)
    {
        if (Tipo != TipoSolicitacao.DadosCadastrais)
        {
            throw new InvalidOperationException(
                "Somente a solicitação de dados cadastrais carrega alterações de campo.");
        }

        if (Status != StatusSolicitacao.Aberta || _alteracoes.Count > 0)
        {
            throw new InvalidOperationException(
                "As alterações só podem ser definidas na criação da solicitação.");
        }

        var lista = (alteracoes ?? Array.Empty<AlteracaoSolicitada>()).ToList();

        if (lista.Count == 0)
        {
            throw new ArgumentException("A solicitação de dados cadastrais exige ao menos uma alteração.");
        }

        if (lista.Select(alteracao => alteracao.Campo).Distinct().Count() != lista.Count)
        {
            throw new ArgumentException("O mesmo campo não pode aparecer duas vezes no pedido.");
        }

        foreach (var alteracao in lista)
        {
            _alteracoes.Add(new SolicitacaoAlteracao(
                alteracao.Campo,
                alteracao.ValorSolicitado,
                alteracao.ValorNoPedido));
        }
    }

    public void IniciarAnalise(string autorNome, DateTime utc) =>
        Transicionar(StatusSolicitacao.EmAnalise, autorNome, null, utc);

    public void Aprovar(string resposta, string autorNome, DateTime utc) =>
        Transicionar(StatusSolicitacao.Aprovada, autorNome, resposta, utc);

    public void Recusar(string resposta, string autorNome, DateTime utc) =>
        Transicionar(StatusSolicitacao.Recusada, autorNome, resposta, utc);

    public void Concluir(string autorNome, DateTime utc) =>
        Transicionar(StatusSolicitacao.Concluida, autorNome, null, utc);

    public void Cancelar(string autorNome, DateTime utc) =>
        Transicionar(StatusSolicitacao.Cancelada, autorNome, null, utc);

    private void Transicionar(
        StatusSolicitacao destino,
        string autorNome,
        string? resposta,
        DateTime utc)
    {
        if (!TransicoesPermitidas.TryGetValue(Status, out var permitidas) ||
            !permitidas.Contains(destino))
        {
            throw new TransicaoInvalidaException(
                $"Não é possível passar de {Status} para {destino}.");
        }

        var autor = ValidacaoDeCampo.Texto(autorNome, "autor", 2, 100);
        string? respostaValidada = null;

        if (destino is StatusSolicitacao.Aprovada or StatusSolicitacao.Recusada)
        {
            respostaValidada = ValidacaoDeCampo.Texto(
                resposta, "resposta", RespostaMin, RespostaMax);
        }

        Status = destino;
        AtualizadoEmUtc = NormalizarUtc(utc);

        if (destino is StatusSolicitacao.Aprovada or StatusSolicitacao.Recusada)
        {
            Resposta = respostaValidada;
            RespondidoPor = autor;
        }

        _historico.Add(new HistoricoSolicitacao(
            Id,
            destino,
            autor,
            respostaValidada,
            AtualizadoEmUtc));
    }

    private static DateTime NormalizarUtc(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();

    private static string? NormalizarNome(string? nome) =>
        string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
}
