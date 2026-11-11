using Ntc.Domain.Services;

namespace Ntc.Domain.Entity;

/// <summary>
/// Solicitação de acesso (Spec 033). Guarda a intenção do autocadastro (tipo e unidade),
/// o CRN pedido quando nutricionista e a decisão administrativa (papel efetivamente
/// concedido, escola, autor e data). A conta é a pessoa; a solicitação é a intenção; a
/// aprovação é o ato administrativo — cada um com seu estado, sem que "sem papel"
/// signifique três coisas ao mesmo tempo.
/// </summary>
public class SolicitacaoDeAcesso
{
    public const int MotivoMin = 5;
    public const int MotivoMax = 500;
    public const int JustificativaMin = 10;
    public const int JustificativaMax = 500;

    public int Id { get; private set; }

    public int UsuarioId { get; private set; }

    /// <summary>Imutável (RF2): corrigir intenção exige nova solicitação.</summary>
    public TipoCadastroSolicitado TipoSolicitado { get; private set; }

    public int UnidadeId { get; private set; }

    public StatusSolicitacaoAcesso Status { get; private set; }

    /// <summary>CRN solicitado no cadastro, normalizado pela Spec 031; nulo fora de nutrição.</summary>
    public string? CrnSolicitado { get; private set; }

    public DateTime SolicitadoEmUtc { get; private set; }

    public DateTime? DecididoEmUtc { get; private set; }

    public int? DecididoPorId { get; private set; }

    /// <summary>Snapshot do nome do decisor (ADR-009); não é FK.</summary>
    public string? DecididoPorNome { get; private set; }

    /// <summary>Papel efetivamente atribuído; pode diferir do solicitado só com justificativa.</summary>
    public PapelUsuario? PapelConcedido { get; private set; }

    /// <summary>Obrigatório para merendeira; proibido nos demais.</summary>
    public int? EscolaConcedidaId { get; private set; }

    /// <summary>Obrigatório na recusa; também registra a correção de papel quando houver.</summary>
    public string? Motivo { get; private set; }

    public string? JustificativaCorrecaoPapel { get; private set; }

    /// <summary>Somente para o EF Core.</summary>
    private SolicitacaoDeAcesso()
    {
    }

    public SolicitacaoDeAcesso(
        int usuarioId,
        TipoCadastroSolicitado tipoSolicitado,
        int unidadeId,
        string? crnSolicitado,
        DateTime solicitadoEmUtc)
    {
        if (usuarioId <= 0)
            throw new ArgumentException("A solicitação exige um usuário válido.");

        if (!Enum.IsDefined(tipoSolicitado))
            throw new ArgumentException("Tipo de cadastro inválido.");

        if (unidadeId <= 0)
            throw new ArgumentException("A solicitação exige uma unidade válida.");

        UsuarioId = usuarioId;
        TipoSolicitado = tipoSolicitado;
        UnidadeId = unidadeId;
        CrnSolicitado = NormalizarCrn(tipoSolicitado, crnSolicitado);
        SolicitadoEmUtc = NormalizarUtc(solicitadoEmUtc);
        Status = StatusSolicitacaoAcesso.Pendente;
    }

    public void Aprovar(
        PapelUsuario papelConcedido,
        int? escolaConcedidaId,
        int decididoPorId,
        string? decididoPorNome,
        string? justificativaCorrecaoPapel,
        DateTime utc)
    {
        GarantirPendente();
        ValidarPapelConcedido(papelConcedido);

        if (papelConcedido == PapelUsuario.Merendeira && escolaConcedidaId is not > 0)
            throw new ArgumentException("A aprovação de merendeira exige a escola.");

        if (papelConcedido != PapelUsuario.Merendeira && escolaConcedidaId is not null)
            throw new ArgumentException("Apenas merendeira recebe escola na aprovação.");

        var corrigido = papelConcedido != PoliticaDeAprovacao.PapelPadrao(TipoSolicitado);

        if (corrigido)
        {
            JustificativaCorrecaoPapel = ValidacaoDeCampo.Texto(
                justificativaCorrecaoPapel,
                "justificativa de correção de papel",
                JustificativaMin,
                JustificativaMax);
        }
        else
        {
            JustificativaCorrecaoPapel = null;
        }

        Status = StatusSolicitacaoAcesso.Aprovada;
        PapelConcedido = papelConcedido;
        EscolaConcedidaId = escolaConcedidaId;
        DecididoPorId = ValidarDecisor(decididoPorId);
        DecididoPorNome = ValidacaoDeCampo.Texto(decididoPorNome, "autor", 2, 100);
        DecididoEmUtc = NormalizarUtc(utc);
    }

    public void Recusar(
        string? motivo,
        int decididoPorId,
        string? decididoPorNome,
        DateTime utc)
    {
        GarantirPendente();

        Motivo = ValidacaoDeCampo.Texto(motivo, "motivo", MotivoMin, MotivoMax);
        Status = StatusSolicitacaoAcesso.Recusada;
        DecididoPorId = ValidarDecisor(decididoPorId);
        DecididoPorNome = ValidacaoDeCampo.Texto(decididoPorNome, "autor", 2, 100);
        DecididoEmUtc = NormalizarUtc(utc);
    }

    private void GarantirPendente()
    {
        if (Status != StatusSolicitacaoAcesso.Pendente)
            throw new TransicaoInvalidaException("A solicitação já foi decidida.");
    }

    private static void ValidarPapelConcedido(PapelUsuario papel)
    {
        if (papel is PapelUsuario.AdminSecretaria or PapelUsuario.AdminEscola)
            throw new ArgumentException(PoliticaDeAprovacao.MensagemPapelAdministrativo);

        if (papel is not (PapelUsuario.Nutricionista
            or PapelUsuario.Merendeira
            or PapelUsuario.Responsavel))
            throw new ArgumentException("Papel concedido inválido.");
    }

    private static int ValidarDecisor(int decididoPorId)
    {
        if (decididoPorId <= 0)
            throw new ArgumentException("A decisão exige um autor válido.");

        return decididoPorId;
    }

    private static string? NormalizarCrn(TipoCadastroSolicitado tipo, string? crnSolicitado)
    {
        if (tipo == TipoCadastroSolicitado.Nutricionista)
        {
            if (!CrnDeNutricionista.TryParse(crnSolicitado, null, out var valor, out var erro))
                throw new ArgumentException(erro ?? CrnDeNutricionista.MensagemFormatoInvalido);

            return valor!.Valor;
        }

        if (!string.IsNullOrWhiteSpace(crnSolicitado))
            throw new ArgumentException(CrnDeNutricionista.MensagemSomenteNutricionista);

        return null;
    }

    private static DateTime NormalizarUtc(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
}
