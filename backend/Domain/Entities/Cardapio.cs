using Ntc.Domain.Services;
using StatusDeAdaptacao = Ntc.Domain.Entity.StatusAdaptacao;

namespace Ntc.Domain.Entity;

/// <summary>
/// Agregado do cardápio (Spec 012, RF1 a RF13; Spec 015, RF1 a RF9).
///
/// Reúne nome, unidade, período de vigência, situação, versão, refeições, itens e a trilha
/// append-only de eventos. Toda validação passa por <c>Validar</c>, que devolve um buffer; só
/// depois <c>Aplicar</c> toca no estado (ADR-004).
///
/// As transições de situação vivem aqui (Spec 015, RNF1): pré-condições e gravação de evento
/// no próprio agregado. O serviço apenas carrega dados, chama o domínio e persiste.
/// </summary>
public class Cardapio
{
    public const int NomeMin = 2;
    public const int NomeMax = 120;
    public const int RefeicoesMaximas = 10;

    private readonly List<Refeicao> _refeicoes = new();
    private readonly List<HistoricoCardapio> _historico = new();
    private readonly List<SubstituicaoDeItem> _substituicoes = new();
    private readonly List<PendenciaAdaptacao> _pendencias = new();
    private readonly List<ConfirmacaoDeIncompatibilidade> _confirmacoes = new();

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public int UnidadeId { get; private set; }

    public PeriodoVigencia Periodo { get; private set; } = null!;

    public SituacaoCardapio Situacao { get; private set; }

    // ---------- Versão geral e adaptada (Spec 029) ----------

    /// <summary>Geral ou Adaptado; todo cardápio tem um tipo (RF1).</summary>
    public TipoCardapio Tipo { get; private set; } = TipoCardapio.Geral;

    /// <summary>Geral de origem da versão adaptada; nulo no geral (RF1).</summary>
    public int? OrigemId { get; private set; }

    /// <summary>Escola da versão adaptada; nulo no geral (RF1).</summary>
    public int? EscolaId { get; private set; }

    /// <summary>Chave canônica do conjunto de restrições; nula no geral (RF2).</summary>
    public string? ConjuntoRestricoesChave { get; private set; }

    /// <summary>Estado da adaptação; nulo no geral (RF1 e RF7).</summary>
    public StatusAdaptacao? StatusAdaptacao { get; private set; }

    /// <summary>Origem da duplicação (Spec 012, RF6).</summary>
    public int? CardapioDuplicadoDeId { get; private set; }

    /// <summary>Origem da revisão (Spec 015, RF6); nulo quando o cardápio não é uma revisão.</summary>
    public int? RevisaoDeId { get; private set; }

    /// <summary>Versão do cardápio; incrementada pela revisão (Spec 015, RF6).</summary>
    public int Versao { get; private set; } = 1;

    public DateTime CriadoEmUtc { get; private set; }

    public DateTime AtualizadoEmUtc { get; private set; }

    /// <summary>Preenchido na publicação (Spec 015, RF2); nulo em rascunho.</summary>
    public DateTime? PublicadoEmUtc { get; private set; }

    /// <summary>Snapshot do nome de quem publicou (Spec 015, RF2).</summary>
    public string PublicadoPor { get; private set; } = string.Empty;

    // ---------- Nutricionista responsável (snapshot, sem FK — ADR-009) ----------

    public int? NutricionistaId { get; private set; }

    public string NutricionistaNome { get; private set; } = string.Empty;

    public string NutricionistaEmail { get; private set; } = string.Empty;

    public string NutricionistaCpf { get; private set; } = string.Empty;

    public string? NutricionistaCrn { get; private set; }

    public IReadOnlyList<Refeicao> Refeicoes => _refeicoes.AsReadOnly();

    public IReadOnlyList<HistoricoCardapio> Historico => _historico.AsReadOnly();

    /// <summary>Substituições aplicadas na versão adaptada (RF5 e T5).</summary>
    public IReadOnlyList<SubstituicaoDeItem> Substituicoes => _substituicoes.AsReadOnly();

    /// <summary>Motivos que impedem a publicação da versão adaptada (RF7 e RF8).</summary>
    public IReadOnlyList<PendenciaAdaptacao> Pendencias => _pendencias.AsReadOnly();

    /// <summary>Confirmações append-only de incompatibilidade mantida no geral (RF12 e RF13).</summary>
    public IReadOnlyList<ConfirmacaoDeIncompatibilidade> Confirmacoes => _confirmacoes.AsReadOnly();

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private Cardapio()
    {
    }

    public Cardapio(
        string? nome,
        int unidadeId,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes,
        int? duplicadoDeId = null,
        int? revisaoDeId = null,
        int versao = 1,
        NutricionistaResponsavel? nutricionista = null,
        TipoCardapio tipo = TipoCardapio.Geral,
        int? origemId = null,
        int? escolaId = null,
        ConjuntoDeRestricoes? conjunto = null,
        StatusAdaptacao? statusAdaptacao = null)
    {
        var dados = Validar(nome, unidadeId, periodo, refeicoes);
        var adaptacao = ValidarAdaptacao(tipo, origemId, escolaId, conjunto, statusAdaptacao);

        Aplicar(dados);

        Tipo = adaptacao.Tipo;
        OrigemId = adaptacao.OrigemId;
        EscolaId = adaptacao.EscolaId;
        ConjuntoRestricoesChave = adaptacao.ConjuntoRestricoesChave;
        StatusAdaptacao = adaptacao.StatusAdaptacao;

        if (nutricionista is not null)
        {
            DefinirNutricionista(nutricionista);
        }

        Situacao = SituacaoCardapio.Rascunho;
        CardapioDuplicadoDeId = duplicadoDeId;
        RevisaoDeId = revisaoDeId;
        Versao = versao;
        CriadoEmUtc = AtualizadoEmUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Cria a versão adaptada de um geral para uma escola e um conjunto (RF1 a RF3 e RF5).
    /// A origem precisa ser um geral já persistido: adaptado de adaptado não existe.
    /// </summary>
    public static Cardapio CriarAdaptado(
        Cardapio origem,
        int escolaId,
        ConjuntoDeRestricoes conjunto,
        IEnumerable<Refeicao>? refeicoes,
        IEnumerable<ItemSubstituido>? substituicoes,
        IEnumerable<PendenciaDeAdaptacao>? pendencias,
        StatusAdaptacao status)
    {
        ArgumentNullException.ThrowIfNull(origem);
        ArgumentNullException.ThrowIfNull(conjunto);

        if (origem.Tipo != TipoCardapio.Geral)
        {
            throw new ArgumentException(
                "Uma versão adaptada só pode ser criada a partir de um cardápio geral.");
        }

        if (origem.Id <= 0)
        {
            throw new ArgumentException(
                "O cardápio geral precisa estar persistido antes de ser adaptado.");
        }

        var adaptado = new Cardapio(
            origem.Nome,
            origem.UnidadeId,
            origem.Periodo,
            refeicoes,
            tipo: TipoCardapio.Adaptado,
            origemId: origem.Id,
            escolaId: escolaId,
            conjunto: conjunto,
            statusAdaptacao: status,
            nutricionista: origem.RetratoDaNutricionista());

        adaptado.SubstituirRegistrosDaAdaptacao(substituicoes, pendencias);

        return adaptado;
    }

    /// <summary>
    /// Substitui o resultado de uma readaptação no rascunho existente (RF3 e RNF5):
    /// refeições, substituições e pendências antigas são descartadas juntas, para que não
    /// sobre registro de uma versão que não existe mais.
    /// </summary>
    public void AplicarAdaptacao(
        IEnumerable<Refeicao>? refeicoes,
        IEnumerable<ItemSubstituido>? substituicoes,
        IEnumerable<PendenciaDeAdaptacao>? pendencias,
        StatusAdaptacao status,
        string? autorNome,
        DateTime utc)
    {
        if (Tipo != TipoCardapio.Adaptado)
        {
            throw new InvalidOperationException(
                "Somente uma versão adaptada pode receber o resultado da adaptação.");
        }

        if (Situacao != SituacaoCardapio.Rascunho)
        {
            throw new TransicaoInvalidaException(
                "Somente versão adaptada em rascunho pode ser readaptada.");
        }

        if (status is not (StatusDeAdaptacao.Atualizado or StatusDeAdaptacao.PendenteDeRevisao))
        {
            throw new ArgumentException(
                "O resultado de uma adaptação só pode ser Atualizado ou PendenteDeRevisao.");
        }

        var dados = Validar(Nome, UnidadeId, Periodo, refeicoes);
        var novasPendencias = (pendencias ?? Array.Empty<PendenciaDeAdaptacao>()).ToList();

        if (status == StatusDeAdaptacao.Atualizado && novasPendencias.Count > 0)
        {
            throw new ArgumentException("Versão atualizada não pode carregar pendências.");
        }

        if (status == StatusDeAdaptacao.PendenteDeRevisao && novasPendencias.Count == 0)
        {
            throw new ArgumentException("Versão pendente precisa registrar ao menos um motivo.");
        }

        _refeicoes.Clear();
        _refeicoes.AddRange(dados.Refeicoes);

        SubstituirRegistrosDaAdaptacao(substituicoes, novasPendencias);

        StatusAdaptacao = status;
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Editado, autorNome ?? "sistema", "Readaptação", utc);
    }

    /// <summary>
    /// Marca a versão como desatualizada quando o geral muda (RF20). Só sai de
    /// <c>Atualizado</c>: uma versão pendente continua pendente, porque o problema dela não
    /// foi resolvido. Repetir a chamada não altera a data.
    /// </summary>
    public void MarcarDesatualizado(string? autorNome, DateTime utc)
    {
        if (Tipo != TipoCardapio.Adaptado)
        {
            throw new InvalidOperationException(
                "Somente versão adaptada pode ser desatualizada.");
        }

        if (StatusAdaptacao != StatusDeAdaptacao.Atualizado)
        {
            return;
        }

        StatusAdaptacao = StatusDeAdaptacao.Desatualizado;
        AtualizadoEmUtc = utc;
        RegistrarEvento(
            EventoCardapio.Editado,
            autorNome ?? "sistema",
            "Geral alterado: versão desatualizada",
            utc);
    }

    /// <summary>
    /// Registra a confirmação explícita de uma incompatibilidade mantida no geral (RF12 e
    /// RF13). É append-only e exclusiva de rascunho do tipo geral.
    /// </summary>
    public void RegistrarConfirmacaoDeIncompatibilidade(
        string autorNome,
        int alimentoId,
        string alimentoNome,
        IEnumerable<string> escolas,
        IEnumerable<string> conjuntos,
        DateTime utc)
    {
        if (Tipo != TipoCardapio.Geral)
        {
            throw new InvalidOperationException(
                "A confirmação de incompatibilidade pertence ao cardápio geral.");
        }

        if (Situacao != SituacaoCardapio.Rascunho)
        {
            throw new TransicaoInvalidaException(
                "A confirmação só é registrada na montagem de um rascunho.");
        }

        _confirmacoes.Add(new ConfirmacaoDeIncompatibilidade(
            autorNome,
            alimentoId,
            alimentoNome,
            string.Join(", ", escolas.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.Ordinal)),
            string.Join(", ", conjuntos.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal)),
            utc));
    }

    /// <summary>
    /// Substitui nome, período e refeições de um rascunho (Spec 012, RF12). Valida tudo antes
    /// de alterar qualquer campo; cardápio que não é rascunho não é editável.
    /// </summary>
    public void Atualizar(
        string? nome,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes)
    {
        if (Tipo != TipoCardapio.Geral)
        {
            throw new InvalidOperationException(
                "O conteúdo de uma versão adaptada é derivado; use a readaptação.");
        }

        if (Situacao != SituacaoCardapio.Rascunho)
        {
            throw new InvalidOperationException("Só é possível editar cardápio em rascunho.");
        }

        var dados = Validar(nome, UnidadeId, periodo, refeicoes);

        Aplicar(dados);
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    // ---------- Autoria (Spec 027) ----------

    /// <summary>
    /// Grava o retrato de quem elaborou o cardápio. Nome é obrigatório; e-mail, CPF e CRN
    /// entram normalizados e podem estar ausentes no cadastro da profissional.
    /// </summary>
    public void DefinirNutricionista(NutricionistaResponsavel nutricionista)
    {
        var nome = (nutricionista.Nome ?? string.Empty).Trim();

        if (nome.Length < 2 || nome.Length > 100)
        {
            throw new ArgumentException(
                "A nutricionista responsável exige um nome de 2 a 100 caracteres.");
        }

        NutricionistaId = nutricionista.Id > 0 ? nutricionista.Id : null;
        NutricionistaNome = nome;
        NutricionistaEmail = (nutricionista.Email ?? string.Empty).Trim().ToLowerInvariant();
        NutricionistaCpf = new string(
            (nutricionista.Cpf ?? string.Empty).Where(char.IsDigit).ToArray());

        // O snapshot grava apenas a forma canônica do CRN (Spec 031, T5).
        var crn = CrnDeNutricionista.Normalizar(nutricionista.Crn);
        NutricionistaCrn = crn.Length == 0 ? null : crn;
    }

    /// <summary>Retrato atual da autoria, usado para propagar revisão e duplicação.</summary>
    private NutricionistaResponsavel? RetratoDaNutricionista() =>
        NutricionistaId is null && NutricionistaNome.Length == 0
            ? null
            : new NutricionistaResponsavel(
                NutricionistaId ?? 0,
                NutricionistaNome,
                NutricionistaEmail,
                NutricionistaCpf,
                NutricionistaCrn);

    // ---------- Transições (Spec 015) ----------

    public void Publicar(string autorNome, DateTime utc)
    {
        if (Situacao == SituacaoCardapio.Publicado)
            throw new TransicaoInvalidaException("Cardápio já publicado.");
        if (Situacao != SituacaoCardapio.Rascunho)
            throw new TransicaoInvalidaException("Somente rascunho pode ser publicado.");

        Situacao = SituacaoCardapio.Publicado;
        PublicadoEmUtc = utc;
        PublicadoPor = string.IsNullOrWhiteSpace(autorNome) ? "sistema" : autorNome.Trim();
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Publicado, autorNome, null, utc);
    }

    public void Cancelar(string motivo, string autorNome, DateTime utc)
    {
        motivo = (motivo ?? string.Empty).Trim();

        if (motivo.Length < 2 || motivo.Length > 500)
            throw new ArgumentException("O motivo deve ter entre 2 e 500 caracteres.");

        if (Situacao is SituacaoCardapio.Cancelado or SituacaoCardapio.Substituido)
            throw new TransicaoInvalidaException("Cardápio já encerrado.");

        Situacao = SituacaoCardapio.Cancelado;
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Cancelado, autorNome, motivo, utc);
    }

    public Cardapio CriarRevisao(string autorNome, DateTime utc)
    {
        if (Tipo != TipoCardapio.Geral)
        {
            throw new TransicaoInvalidaException(
                "Versão adaptada é derivada do geral e não é revisada diretamente.");
        }

        if (Situacao != SituacaoCardapio.Publicado)
            throw new TransicaoInvalidaException("Somente cardápio publicado pode ser revisado.");

        var revisao = new Cardapio(
            Nome,
            UnidadeId,
            Periodo,
            ClonarRefeicoes(),
            duplicadoDeId: null,
            revisaoDeId: Id,
            versao: Versao + 1,
            nutricionista: RetratoDaNutricionista());

        revisao.RegistrarEvento(EventoCardapio.RevisaoCriada, autorNome, $"Revisão de #{Id}", utc);

        return revisao;
    }

    internal void MarcarSubstituido(int porCardapioId, string autorNome, DateTime utc)
    {
        if (Situacao != SituacaoCardapio.Publicado)
            throw new TransicaoInvalidaException("Somente publicado pode ser substituído.");

        Situacao = SituacaoCardapio.Substituido;
        AtualizadoEmUtc = utc;
        RegistrarEvento(EventoCardapio.Substituido, autorNome, $"Substituído por #{porCardapioId}", utc);
    }

    // ---------- Registro de eventos ----------

    /// <summary>Evento de criação (Spec 015, RF9). Chamado pelo serviço logo após construir.</summary>
    public void RegistrarCriacao(string? autorNome) =>
        RegistrarEvento(EventoCardapio.Criado, autorNome ?? "sistema", null, DateTime.UtcNow);

    /// <summary>Evento de edição de rascunho (Spec 015, RF9).</summary>
    public void RegistrarEdicao(string? autorNome) =>
        RegistrarEvento(EventoCardapio.Editado, autorNome ?? "sistema", null, DateTime.UtcNow);

    /// <summary>Evento de duplicação (Spec 015, RF9).</summary>
    public void RegistrarDuplicacao(string? autorNome) =>
        RegistrarEvento(EventoCardapio.Duplicado, autorNome ?? "sistema", null, DateTime.UtcNow);

    private void RegistrarEvento(EventoCardapio evento, string autorNome, string? motivo, DateTime utc)
    {
        _historico.Add(new HistoricoCardapio(Id, evento, Versao, autorNome, motivo, utc));
    }

    /// <summary>Cópia para duplicação (Spec 012, RF6) e para a revisão (Spec 015, RF6).</summary>
    internal static Cardapio Copiar(
        Cardapio origem,
        string nome,
        PeriodoVigencia periodo,
        int? duplicadoDeId = null)
    {
        var refeicoes = origem._refeicoes.Select(Refeicao.Copiar);

        return new Cardapio(
            nome,
            origem.UnidadeId,
            periodo,
            refeicoes,
            duplicadoDeId,
            nutricionista: origem.RetratoDaNutricionista());
    }

    /// <summary>Quantidade total de itens, usada na listagem sem carregar as refeições.</summary>
    public int TotalDeItens => _refeicoes.Sum(refeicao => refeicao.Itens.Count);

    private IEnumerable<Refeicao> ClonarRefeicoes() => _refeicoes.Select(Refeicao.Copiar);

    private void Aplicar((string Nome, int UnidadeId, PeriodoVigencia Periodo, List<Refeicao> Refeicoes) dados)
    {
        Nome = dados.Nome;
        UnidadeId = dados.UnidadeId;
        Periodo = dados.Periodo;

        _refeicoes.Clear();
        _refeicoes.AddRange(dados.Refeicoes);
    }

    private void SubstituirRegistrosDaAdaptacao(
        IEnumerable<ItemSubstituido>? substituicoes,
        IEnumerable<PendenciaDeAdaptacao>? pendencias)
    {
        _substituicoes.Clear();
        _pendencias.Clear();

        foreach (var substituicao in substituicoes ?? Array.Empty<ItemSubstituido>())
        {
            _substituicoes.Add(new SubstituicaoDeItem(
                substituicao.RefeicaoOrdem,
                substituicao.RefeicaoNome,
                substituicao.AlimentoOriginalId,
                substituicao.AlimentoOriginalNome,
                substituicao.AlimentoSubstitutoId,
                substituicao.AlimentoSubstitutoNome,
                substituicao.QuantidadeGramas,
                string.Join(", ", substituicao.RestricoesBloqueantes)));
        }

        foreach (var pendencia in pendencias ?? Array.Empty<PendenciaDeAdaptacao>())
        {
            _pendencias.Add(new PendenciaAdaptacao(
                pendencia.RefeicaoOrdem,
                pendencia.RefeicaoNome,
                pendencia.AlimentoId,
                pendencia.AlimentoNome,
                pendencia.Motivo));
        }
    }

    private readonly record struct DadosDaAdaptacao(
        TipoCardapio Tipo,
        int? OrigemId,
        int? EscolaId,
        string? ConjuntoRestricoesChave,
        StatusAdaptacao? StatusAdaptacao);

    /// <summary>
    /// Regra única de RF1: o geral não tem origem, escola, conjunto nem status de adaptação;
    /// o adaptado exige os quatro e nunca aceita conjunto vazio (RF2).
    /// </summary>
    private static DadosDaAdaptacao ValidarAdaptacao(
        TipoCardapio tipo,
        int? origemId,
        int? escolaId,
        ConjuntoDeRestricoes? conjunto,
        StatusAdaptacao? statusAdaptacao)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new ArgumentException("Tipo de cardápio inválido.");
        }

        if (tipo == TipoCardapio.Geral)
        {
            if (origemId is not null || escolaId is not null || conjunto is not null || statusAdaptacao is not null)
            {
                throw new ArgumentException(
                    "Cardápio geral não possui origem, escola, conjunto nem status de adaptação.");
            }

            return new DadosDaAdaptacao(TipoCardapio.Geral, null, null, null, null);
        }

        if (origemId is null or <= 0)
        {
            throw new ArgumentException("A versão adaptada exige um cardápio de origem.");
        }

        if (escolaId is null or <= 0)
        {
            throw new ArgumentException("A versão adaptada exige uma escola.");
        }

        if (conjunto is null || conjunto.EstaVazio)
        {
            throw new ArgumentException(
                "A versão adaptada exige um conjunto de restrições não vazio.");
        }

        if (statusAdaptacao is null || !Enum.IsDefined(statusAdaptacao.Value))
        {
            throw new ArgumentException("A versão adaptada exige o status de adaptação.");
        }

        return new DadosDaAdaptacao(
            TipoCardapio.Adaptado,
            origemId,
            escolaId,
            conjunto.Chave,
            statusAdaptacao);
    }

    private static (string Nome, int UnidadeId, PeriodoVigencia Periodo, List<Refeicao> Refeicoes) Validar(
        string? nome,
        int unidadeId,
        PeriodoVigencia? periodo,
        IEnumerable<Refeicao>? refeicoes)
    {
        var nomeValidado = ValidacaoDeCampo.Texto(nome, "nome", NomeMin, NomeMax);

        if (unidadeId <= 0)
        {
            throw new ArgumentException("O cardápio exige uma unidade válida.");
        }

        if (periodo is null)
        {
            throw new ArgumentException("O período do cardápio é obrigatório.");
        }

        var validados = (refeicoes ?? Array.Empty<Refeicao>()).ToList();

        if (validados.Count > RefeicoesMaximas)
        {
            throw new ArgumentException($"O cardápio aceita no máximo {RefeicoesMaximas} refeições.");
        }

        if (validados.GroupBy(refeicao => refeicao.Ordem).Any(grupo => grupo.Count() > 1))
        {
            throw new ArgumentException("Duas refeições não podem ter a mesma ordem.");
        }

        if (validados.GroupBy(refeicao => refeicao.Nome, StringComparer.OrdinalIgnoreCase)
            .Any(grupo => grupo.Count() > 1))
        {
            throw new ArgumentException("Duas refeições não podem ter o mesmo nome.");
        }

        return (nomeValidado, unidadeId, periodo, validados);
    }
}
