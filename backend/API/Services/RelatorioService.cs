using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Relatorios;

/// <summary>
/// Caso de uso dos relatórios (Spec 019). O serviço decide acesso e escopo, valida o
/// período, resolve o recorte e delega a leitura ao porto. Nenhuma regra mora no
/// controller e nenhuma consulta mora aqui (ADR-004, ADR-012, ADR-019).
/// </summary>
public sealed class RelatorioService
{
    public const int LinhasDePreview = 200;
    public const int LinhasMaximas = 50_000;

    private readonly IRelatorioRepository _relatorios;
    private readonly IExportadorDeRelatorio _exportador;
    private readonly AutorizacaoService _autorizacao;
    private readonly TimeProvider _relogio;

    public RelatorioService(
        IRelatorioRepository relatorios,
        IExportadorDeRelatorio exportador,
        AutorizacaoService autorizacao,
        TimeProvider relogio)
    {
        _relatorios = relatorios;
        _exportador = exportador;
        _autorizacao = autorizacao;
        _relogio = relogio;
    }

    public Resultado<RelatorioPreviewDTO> Previsualizar(
        string? tipo,
        DateOnly? de,
        DateOnly? ate,
        int? unidadeId,
        int? escolaId,
        EscopoUsuario escopo)
    {
        var preparo = Preparar(tipo, de, ate, unidadeId, escolaId, escopo);

        if (!preparo.Permitido)
        {
            return Recusar<RelatorioPreviewDTO>(preparo);
        }

        var tabela = _relatorios.Obter(preparo.Tipo!.Value, preparo.Filtro!);

        return Resultado<RelatorioPreviewDTO>.Ok(new RelatorioPreviewDTO
        {
            Tipo = preparo.Tipo.Value.ToString(),
            Titulo = tabela.Titulo,
            Colunas = tabela.Colunas.ToList(),
            Linhas = tabela.Linhas
                .Take(LinhasDePreview)
                .Select(linha => linha.ToList())
                .ToList(),
            TotalLinhas = tabela.Linhas.Count,
            Rodape = tabela.Rodape
        });
    }

    public Resultado<ArquivoDeRelatorio> Exportar(
        string? tipo,
        DateOnly? de,
        DateOnly? ate,
        int? unidadeId,
        int? escolaId,
        EscopoUsuario escopo)
    {
        var preparo = Preparar(tipo, de, ate, unidadeId, escolaId, escopo);

        if (!preparo.Permitido)
        {
            return Recusar<ArquivoDeRelatorio>(preparo);
        }

        var tabela = _relatorios.Obter(preparo.Tipo!.Value, preparo.Filtro!);

        if (tabela.Linhas.Count > LinhasMaximas)
        {
            return Resultado<ArquivoDeRelatorio>.LimiteExcedido(
                $"O relatório excede {LinhasMaximas} linhas. Reduza o período.");
        }

        var conteudo = _exportador.Gerar(tabela);

        var nome =
            $"relatorio-{preparo.Tipo.Value.ToString().ToLowerInvariant()}-" +
            $"{_relogio.GetUtcNow():yyyyMMddHHmm}.{_exportador.Extensao}";

        return Resultado<ArquivoDeRelatorio>.Ok(
            new ArquivoDeRelatorio(conteudo, nome, _exportador.TipoDeConteudo));
    }

    // ---------- Apoio ----------

    private Preparo Preparar(
        string? tipo,
        DateOnly? de,
        DateOnly? ate,
        int? unidadeId,
        int? escolaId,
        EscopoUsuario escopo)
    {
        var tipoValido = CatalogoDeRelatorios.Interpretar(tipo);

        if (tipoValido is null)
        {
            return Preparo.Invalido("Relatório desconhecido.");
        }

        if (_autorizacao.Verificar(Recurso.Relatorio, escopo, OperacaoDeAcesso.Ler) is { } negacao)
        {
            return Preparo.Negado(negacao);
        }

        if (FiltroDeRelatorio.Validar(de, ate) is { } erro)
        {
            return Preparo.Invalido(erro);
        }

        if (escopo.EscopoDeUnidade)
        {
            if (escopo.UnidadeId is not { } unidadeDoEscopo)
            {
                return Preparo.Negado(ResultadoStatus.Proibido);
            }

            if (unidadeId is { } pedida && pedida != unidadeDoEscopo)
            {
                return Preparo.Negado(ResultadoStatus.Proibido);
            }

            return Preparo.Pronto(
                tipoValido.Value,
                FiltroDeRelatorio.Normalizar(de, ate, Hoje(), unidadeDoEscopo, escolaId));
        }

        if (escopo.Papel == PapelUsuario.AdminEscola)
        {
            if (escopo.UnidadeId is not { } unidadeDoEscopo ||
                escopo.EscolaId is not { } escolaDoEscopo)
            {
                return Preparo.Negado(ResultadoStatus.Proibido);
            }

            if (unidadeId is { } pedida && pedida != unidadeDoEscopo)
            {
                return Preparo.Negado(ResultadoStatus.Proibido);
            }

            if (escolaId is { } pedidaEscola && pedidaEscola != escolaDoEscopo)
            {
                return Preparo.Negado(ResultadoStatus.Proibido);
            }

            return Preparo.Pronto(
                tipoValido.Value,
                FiltroDeRelatorio.Normalizar(de, ate, Hoje(), unidadeDoEscopo, escolaDoEscopo));
        }

        return Preparo.Negado(ResultadoStatus.Proibido);
    }

    private DateOnly Hoje() => DateOnly.FromDateTime(_relogio.GetUtcNow().UtcDateTime);

    private static Resultado<T> Recusar<T>(Preparo preparo) =>
        preparo.Status == ResultadoStatus.Proibido
            ? Resultado<T>.Proibido()
            : Resultado<T>.Invalido(preparo.Erro ?? "Dados inválidos.");

    private sealed record Preparo(
        ResultadoStatus? Status,
        string? Erro,
        TipoDeRelatorio? Tipo,
        FiltroDeRelatorio? Filtro)
    {
        public bool Permitido => Status is null;

        public static Preparo Pronto(TipoDeRelatorio tipo, FiltroDeRelatorio filtro) =>
            new(null, null, tipo, filtro);

        public static Preparo Invalido(string erro) =>
            new(ResultadoStatus.Invalido, erro, null, null);

        public static Preparo Negado(ResultadoStatus status) =>
            new(status, "Acesso negado.", null, null);
    }
}
