using Ntc.Application;
using Ntc.Application.Seguranca;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso do cardápio (Spec 012 e Spec 029).
///
/// O serviço decide quem lê e quem escreve (RF8/RF16), valida unidade, alimentos e
/// nutricionista, aplica a regra de alimento desativado (RF4 — depende do catálogo e do
/// estado anterior, por isso não vive no agregado) e delega cálculo e invariantes ao
/// domínio. A montagem manual produz o geral e, na mesma transação, uma versão adaptada por
/// <c>(escola, conjunto distinto)</c> (Spec 029, RF10).
/// </summary>
public sealed class CardapioService
{
    public const int MaximoDeAdaptados = 50;

    private const string SomenteRascunhoEditavel = "Só é possível editar cardápio em rascunho.";
    private const string SomenteRascunhoExcluivel = "Cardápio publicado não pode ser excluído.";

    private readonly ICardapioRepository _cardapios;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IRestricaoRepository _restricoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly AutorizacaoService _autorizacao;

    public CardapioService(
        ICardapioRepository cardapios,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos,
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IRestricaoRepository restricoes,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        AutorizacaoService autorizacao)
    {
        _cardapios = cardapios;
        _alimentos = alimentos;
        _alunos = alunos;
        _escolas = escolas;
        _usuarios = usuarios;
        _restricoes = restricoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _autorizacao = autorizacao;
    }

    // ---------- Leitura ----------

    public Resultado<PaginaDTO<CardapioResumoDTO>> Listar(
        int? unidadeId,
        DateOnly? data,
        string? situacao,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
        {
            return Resultado<PaginaDTO<CardapioResumoDTO>>.Proibido();
        }

        if (!TentarLerSituacao(situacao, out var situacaoFiltro))
        {
            return Resultado<PaginaDTO<CardapioResumoDTO>>.Invalido(
                $"Situação inválida: '{situacao}'. Use Rascunho, Publicado, Cancelado ou Substituido.");
        }

        if (!TentarRecorte(escopo, unidadeId, out var unidadeDoEscopo, out var escolaVisivel))
        {
            return Resultado<PaginaDTO<CardapioResumoDTO>>.Proibido();
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);

        var (itens, total) = _cardapios.Listar(
            new FiltroCardapio(
                unidadeDoEscopo,
                data,
                situacaoFiltro,
                Refeicao: null,
                EscolaVisivelId: escolaVisivel),
            paginaEfetiva,
            tamanhoEfetivo);

        var resumos = itens.Select(cardapio =>
        {
            var resumo = CardapioMapper.ParaResumo(cardapio);
            var (bloqueantes, alertas) = ContarIncompatibilidades(cardapio);
            resumo.TotalBloqueantes = bloqueantes;
            resumo.TotalAlertas = alertas;
            return resumo;
        }).ToList();

        return Resultado<PaginaDTO<CardapioResumoDTO>>.Ok(new PaginaDTO<CardapioResumoDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = resumos
        });
    }

    public Resultado<CardapioDTO> Obter(int id, EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
        {
            return Resultado<CardapioDTO>.Proibido();
        }

        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        var negacao = _autorizacao.Verificar(
            RecursoDo(cardapio),
            escopo,
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(cardapio.UnidadeId, cardapio.EscolaId));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (negacao is not null)
        {
            return Resultado<CardapioDTO>.Proibido();
        }

        return Resultado<CardapioDTO>.Ok(ParaDetalhe(cardapio));
    }

    /// <summary>Rota pública (RF9): sem sessão, exige unidade e devolve só o geral publicado.</summary>
    public Resultado<PaginaDTO<CardapioDTO>> ListarPublicados(
        int? unidadeId,
        DateOnly? data,
        string? refeicao,
        int? pagina,
        int? tamanho)
    {
        if (unidadeId is not { } unidade || unidade <= 0)
        {
            return Resultado<PaginaDTO<CardapioDTO>>.Invalido(
                "Informe a unidade para consultar os cardápios publicados.");
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);

        var filtro = new FiltroCardapio(
            UnidadeId: unidade,
            Data: data ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Situacao: SituacaoCardapio.Publicado,
            Refeicao: refeicao);

        var (itens, total) = _cardapios.ListarPublicados(filtro, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<CardapioDTO>>.Ok(new PaginaDTO<CardapioDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(cardapio => ParaDetalhe(cardapio)).ToList()
        });
    }

    /// <summary>
    /// Versões adaptadas de um geral (RF18). A origem da consulta é o próprio geral; se o
    /// id recebido for de um adaptado, a listagem devolve os irmãos da mesma origem.
    /// AdminEscola e Merendeira enxergam apenas a versão da própria escola.
    /// </summary>
    public Resultado<List<CardapioAdaptadoResumoDTO>> ListarAdaptados(int id, EscopoUsuario escopo)
    {
        if (!PodeLer(escopo))
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Proibido();
        }

        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.NaoEncontrado(
                "Cardápio não encontrado.");
        }

        var negacao = _autorizacao.Verificar(
            RecursoDo(cardapio),
            escopo,
            OperacaoDeAcesso.Ler,
            new AlvoDeEscopo(cardapio.UnidadeId, cardapio.EscolaId));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.NaoEncontrado(
                "Cardápio não encontrado.");
        }

        if (negacao is not null)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Proibido();
        }

        var origemId = cardapio.Tipo == TipoCardapio.Adaptado
            ? cardapio.OrigemId ?? 0
            : cardapio.Id;

        if (origemId <= 0)
        {
            return Resultado<List<CardapioAdaptadoResumoDTO>>.Ok(
                new List<CardapioAdaptadoResumoDTO>());
        }

        var nomesDeEscola = _escolas.ListarPorUnidade(cardapio.UnidadeId)
            .ToDictionary(escola => escola.Id, escola => escola.Nome);

        var itens = _cardapios.ListarAdaptados(origemId)
            .Where(adaptado =>
                escopo.Papel is not (PapelUsuario.AdminEscola or PapelUsuario.Merendeira) ||
                adaptado.EscolaId == escopo.EscolaId)
            .Select(adaptado => new CardapioAdaptadoResumoDTO
            {
                Id = adaptado.Id,
                Nome = adaptado.Nome,
                OrigemId = origemId,
                EscolaId = adaptado.EscolaId ?? 0,
                EscolaNome = adaptado.EscolaId is { } escolaId
                    ? nomesDeEscola.GetValueOrDefault(escolaId, string.Empty)
                    : string.Empty,
                ConjuntoRestricoesChave = adaptado.ConjuntoRestricoesChave ?? string.Empty,
                StatusAdaptacao = adaptado.StatusAdaptacao,
                Situacao = adaptado.Situacao,
                TotalSubstituicoes = adaptado.Substituicoes.Count,
                TotalPendencias = adaptado.Pendencias.Count,
                SemRestricoes = false
            })
            .ToList();

        return Resultado<List<CardapioAdaptadoResumoDTO>>.Ok(itens);
    }

    // ---------- Escrita ----------

    public Resultado<CardapioDTO> Criar(SalvarCardapioDTO dto, EscopoUsuario escopo)
    {
        if (_escolas.GetUnidade(dto.UnidadeId) is null)
        {
            return Resultado<CardapioDTO>.Invalido("Unidade não encontrada.");
        }

        var negacao = _autorizacao.Verificar(
            Recurso.CardapioGeral,
            escopo,
            OperacaoDeAcesso.Criar,
            new AlvoDeEscopo(dto.UnidadeId, null));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Unidade não encontrada.");
        }

        if (negacao is not null)
        {
            return Resultado<CardapioDTO>.Proibido();
        }

        var nutricionista = RetratoDaNutricionista(dto.NutricionistaId, dto.UnidadeId, out var motivo);

        if (nutricionista is null)
        {
            return Resultado<CardapioDTO>.Invalido(motivo!);
        }

        var refeicoes = MontarRefeicoes(dto, jaExistentes: null, out var erro);

        if (erro is not null)
        {
            return Resultado<CardapioDTO>.Invalido(erro);
        }

        var geral = new Cardapio(
            dto.Nome,
            dto.UnidadeId,
            Periodo(dto.Periodo),
            refeicoes,
            nutricionista: nutricionista);

        var autor = NomeDoAutor(escopo);
        var agora = DateTime.UtcNow;
        var grupos = GruposDaUnidade(dto.UnidadeId);
        var catalogo = CatalogoParaAdaptacao(geral);
        var restricoes = CatalogoDeRestricoes();

        var falha = ValidarIncompatibilidades(
            geral,
            grupos,
            catalogo,
            restricoes,
            dto.ConfirmarIncompatibilidades,
            autor,
            agora);

        if (falha is not null)
        {
            return Resultado<CardapioDTO>.Invalido(falha);
        }

        if (grupos.Count > MaximoDeAdaptados)
        {
            throw new LimiteDeAdaptacoesException(MaximoDeAdaptados, grupos.Count);
        }

        var resultados = grupos
            .Select(grupo => (
                Grupo: grupo,
                Resultado: AdaptacaoDeCardapio.Adaptar(
                    geral,
                    grupo.EscolaId,
                    grupo.Conjunto,
                    catalogo,
                    restricoes)))
            .ToList();

        geral.RegistrarCriacao(autor);

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _cardapios.Add(geral);

            // O geral precisa do id gerado antes de as versões adaptadas apontarem para ele
            // (FK OrigemId); a transação continua aberta, então tudo é atômico (RF10).
            _unidadeDeTrabalho.Salvar();

            foreach (var (grupo, resultado) in resultados)
            {
                var adaptado = Cardapio.CriarAdaptado(
                    geral,
                    grupo.EscolaId,
                    grupo.Conjunto,
                    resultado.Refeicoes,
                    resultado.Substituicoes,
                    resultado.Pendencias,
                    resultado.Status);

                adaptado.RegistrarCriacao(autor);
                _cardapios.Add(adaptado);
            }
        });

        return Carregar(geral.Id);
    }

    public Resultado<CardapioDTO> Atualizar(int id, SalvarCardapioDTO dto, EscopoUsuario escopo)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (cardapio.Tipo != TipoCardapio.Geral)
        {
            return Resultado<CardapioDTO>.Invalido(
                "O conteúdo de uma versão adaptada é derivado; use a readaptação.");
        }

        if (cardapio.Situacao != SituacaoCardapio.Rascunho)
        {
            return Resultado<CardapioDTO>.Conflito(SomenteRascunhoEditavel);
        }

        var negacao = _autorizacao.Verificar(
            Recurso.CardapioGeral,
            escopo,
            OperacaoDeAcesso.Atualizar,
            new AlvoDeEscopo(cardapio.UnidadeId, null));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (negacao is not null)
        {
            return Resultado<CardapioDTO>.Proibido();
        }

        if (dto.NutricionistaId is { } nutricionistaId)
        {
            var nutricionista = RetratoDaNutricionista(nutricionistaId, cardapio.UnidadeId, out var motivo);

            if (nutricionista is null)
            {
                return Resultado<CardapioDTO>.Invalido(motivo!);
            }

            cardapio.DefinirNutricionista(nutricionista);
        }

        // Alimento que já estava no cardápio continua aceito mesmo desativado (RF4).
        var jaExistentes = cardapio.Refeicoes
            .SelectMany(refeicao => refeicao.Itens)
            .Select(item => item.AlimentoId)
            .ToHashSet();

        var refeicoes = MontarRefeicoes(dto, jaExistentes, out var erro);

        if (erro is not null)
        {
            return Resultado<CardapioDTO>.Invalido(erro);
        }

        cardapio.Atualizar(dto.Nome, Periodo(dto.Periodo), refeicoes);

        var autor = NomeDoAutor(escopo);
        var agora = DateTime.UtcNow;
        var grupos = GruposDaUnidade(cardapio.UnidadeId);
        var catalogo = CatalogoParaAdaptacao(cardapio);

        var falha = ValidarIncompatibilidades(
            cardapio,
            grupos,
            catalogo,
            CatalogoDeRestricoes(),
            dto.ConfirmarIncompatibilidades,
            autor,
            agora);

        if (falha is not null)
        {
            return Resultado<CardapioDTO>.Invalido(falha);
        }

        cardapio.RegistrarEdicao(autor);

        // RF20: o geral mudou; toda versão adaptada existente deixa de provar o conteúdo.
        var adaptados = _cardapios.ListarAdaptados(cardapio.Id);

        foreach (var adaptado in adaptados)
        {
            adaptado.MarcarDesatualizado(autor, agora);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _cardapios.Update(cardapio);

            foreach (var adaptado in adaptados)
            {
                _cardapios.Update(adaptado);
            }
        });

        return Carregar(cardapio.Id);
    }

    public Resultado<bool> Excluir(int id, EscopoUsuario escopo)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
        {
            return Resultado<bool>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (cardapio.Situacao != SituacaoCardapio.Rascunho)
        {
            return Resultado<bool>.Conflito(SomenteRascunhoExcluivel);
        }

        var negacao = _autorizacao.Verificar(
            RecursoDo(cardapio),
            escopo,
            OperacaoDeAcesso.Excluir,
            new AlvoDeEscopo(cardapio.UnidadeId, cardapio.EscolaId));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<bool>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (negacao is not null)
        {
            return Resultado<bool>.Proibido();
        }

        if (cardapio.Tipo == TipoCardapio.Geral && _cardapios.ContarAdaptados(cardapio.Id) > 0)
        {
            return Resultado<bool>.Conflito(
                "Exclua primeiro as versões adaptadas deste cardápio geral.");
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Delete(cardapio));

        return Resultado<bool>.Ok(true);
    }

    public Resultado<CardapioDTO> Duplicar(int id, DuplicarCardapioDTO dto, EscopoUsuario escopo)
    {
        var origem = _cardapios.ObterComRefeicoes(id);

        if (origem is null)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (origem.Tipo != TipoCardapio.Geral)
        {
            return Resultado<CardapioDTO>.Invalido(
                "Somente o cardápio geral pode ser duplicado; as versões adaptadas são derivadas.");
        }

        var negacao = _autorizacao.Verificar(
            Recurso.CardapioGeral,
            escopo,
            OperacaoDeAcesso.Criar,
            new AlvoDeEscopo(origem.UnidadeId, null));

        if (negacao == ResultadoStatus.NaoEncontrado)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (negacao is not null)
        {
            return Resultado<CardapioDTO>.Proibido();
        }

        var nome = string.IsNullOrWhiteSpace(dto.Nome) ? origem.Nome : dto.Nome!;
        var copia = Cardapio.Copiar(origem, nome, Periodo(dto.Periodo), duplicadoDeId: origem.Id);

        copia.RegistrarDuplicacao(NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Add(copia));

        return Carregar(copia.Id);
    }

    // ---------- Apoio ----------

    private static bool PodeLer(EscopoUsuario escopo) =>
        escopo.Papel is PapelUsuario.AdminSecretaria
            or PapelUsuario.AdminEscola
            or PapelUsuario.Nutricionista
            or PapelUsuario.Merendeira;

    /// <summary>Snapshot do autor do evento: o nome é copiado, nunca referenciado por FK.</summary>
    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    private NutricionistaResponsavel? RetratoDaNutricionista(
        int? nutricionistaId,
        int unidadeId,
        out string? motivo)
    {
        motivo = null;

        if (nutricionistaId is not { } id || id <= 0)
        {
            motivo = "Informe a nutricionista responsável pelo cardápio.";
            return null;
        }

        var usuario = _usuarios.GetById(id);
        motivo = ValidacaoDeNutricionista.Motivo(usuario, unidadeId);

        return motivo is null && usuario is not null
            ? ValidacaoDeNutricionista.Retrato(usuario)
            : null;
    }

    private static bool TentarLerSituacao(string? texto, out SituacaoCardapio? situacao)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            situacao = null;
            return true;
        }

        if (Enum.TryParse<SituacaoCardapio>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            situacao = valor;
            return true;
        }

        situacao = null;
        return false;
    }

    /// <summary>
    /// Recorte de leitura por papel (Spec 029, RF16): unidade para AdminSecretaria e
    /// Nutricionista; unidade + escola visível para AdminEscola e Merendeira.
    /// </summary>
    private static bool TentarRecorte(
        EscopoUsuario escopo,
        int? unidadeSolicitada,
        out int? unidade,
        out int? escolaVisivel)
    {
        unidade = null;
        escolaVisivel = null;

        if (escopo.Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista)
        {
            if (escopo.UnidadeId is not { } unidadeDoEscopo)
            {
                return false;
            }

            if (unidadeSolicitada is { } pedida && pedida != unidadeDoEscopo)
            {
                return false;
            }

            unidade = unidadeDoEscopo;
            return true;
        }

        if (escopo.Papel is PapelUsuario.AdminEscola or PapelUsuario.Merendeira)
        {
            if (escopo.UnidadeId is not { } unidadeDoEscopo ||
                escopo.EscolaId is not { } escolaDoEscopo)
            {
                return false;
            }

            if (unidadeSolicitada is { } pedida && pedida != unidadeDoEscopo)
            {
                return false;
            }

            unidade = unidadeDoEscopo;
            escolaVisivel = escolaDoEscopo;
            return true;
        }

        return false;
    }

    private static Recurso RecursoDo(Cardapio cardapio) =>
        cardapio.Tipo == TipoCardapio.Adaptado
            ? Recurso.CardapioAdaptado
            : Recurso.CardapioGeral;

    private static PeriodoVigencia Periodo(PeriodoCardapioDTO? dto) =>
        dto is null
            ? throw new ArgumentException("O período do cardápio é obrigatório.")
            : new PeriodoVigencia(dto.Inicio, dto.Fim);

    /// <summary>
    /// Monta as refeições a partir da entrada, validando o catálogo. A regra de RF4 é
    /// stateful: alimento desativado só entra se já estiver no cardápio — por isso o
    /// conjunto anterior é parâmetro.
    /// </summary>
    private List<Refeicao>? MontarRefeicoes(
        SalvarCardapioDTO dto,
        IReadOnlySet<int>? jaExistentes,
        out string? erro)
    {
        erro = null;

        var solicitados = (dto.Refeicoes ?? new List<SalvarRefeicaoDTO>())
            .SelectMany(refeicao => refeicao.Itens ?? new List<SalvarItemCardapioDTO>())
            .Select(item => item.AlimentoId)
            .Distinct()
            .ToList();

        var catalogo = _alimentos.ObterPorIds(solicitados).ToDictionary(alimento => alimento.Id);

        var refeicoes = new List<Refeicao>();

        foreach (var entrada in dto.Refeicoes ?? new List<SalvarRefeicaoDTO>())
        {
            var itens = new List<ItemRefeicao>();

            foreach (var entradaItem in entrada.Itens ?? new List<SalvarItemCardapioDTO>())
            {
                if (!catalogo.TryGetValue(entradaItem.AlimentoId, out var alimento))
                {
                    erro = $"Alimento {entradaItem.AlimentoId} não encontrado.";
                    return null;
                }

                var jaEstava = jaExistentes?.Contains(alimento.Id) == true;

                if (!alimento.Ativo && !jaEstava)
                {
                    erro = $"O alimento '{alimento.Nome}' está desativado e não pode ser adicionado.";
                    return null;
                }

                itens.Add(new ItemRefeicao(alimento.Id, entradaItem.Gramas));
            }

            refeicoes.Add(new Refeicao(entrada.Nome, entrada.Ordem, itens));
        }

        return refeicoes;
    }

    /// <summary>
    /// Catálogo da adaptação: os alimentos do cardápio (inclusive desativados, para que uma
    /// edição preserve histórico) mais os ativos que podem servir de substituto. Uma leitura
    /// por id para cada conjunto (RNF6).
    /// </summary>
    private List<Alimento> CatalogoParaAdaptacao(Cardapio cardapio)
    {
        var idsDoCardapio = cardapio.Refeicoes
            .SelectMany(refeicao => refeicao.Itens)
            .Select(item => item.AlimentoId)
            .Distinct()
            .ToList();

        return _alimentos
            .ObterComVinculosPorIds(idsDoCardapio)
            .Concat(_alimentos.ListarAtivosComVinculos())
            .GroupBy(alimento => alimento.Id)
            .Select(grupo => grupo.First())
            .OrderBy(alimento => alimento.Id)
            .ToList();
    }

    private List<RestricaoDoCatalogo> CatalogoDeRestricoes() =>
        _restricoes
            .Listar(categoria: null, incluirInativas: true)
            .Select(restricao => new RestricaoDoCatalogo(
                restricao.Id,
                restricao.Nome,
                restricao.Categoria))
            .ToList();

    /// <summary>
    /// Grupos de adaptação da unidade: uma versão por escola e conjunto distinto entre os
    /// alunos daquela escola (RF2, RF6, RF10 e RF21).
    /// </summary>
    private List<GrupoDeAdaptacao> GruposDaUnidade(int unidadeId)
    {
        var escolas = _escolas.ListarPorUnidade(unidadeId)
            .Where(escola => escola.Ativa)
            .ToList();

        var alunos = _alunos.Listar(new FiltroAluno(UnidadeId: unidadeId));
        var porNome = RestricoesDoAluno.PorNomeNormalizado(
            _restricoes.Listar(categoria: null, incluirInativas: true));

        var grupos = new List<GrupoDeAdaptacao>();

        foreach (var escola in escolas)
        {
            var conjuntos = alunos
                .Where(aluno => aluno.EscolaId == escola.Id)
                .Select(aluno => RestricoesDoAluno.Conjunto(aluno, porNome))
                .Where(conjunto => !conjunto.EstaVazio)
                .Distinct();

            foreach (var conjunto in conjuntos)
            {
                grupos.Add(new GrupoDeAdaptacao(escola.Id, escola.Nome, conjunto));
            }
        }

        return grupos
            .OrderBy(grupo => grupo.EscolaNome, StringComparer.Ordinal)
            .ThenBy(grupo => grupo.Conjunto.Chave, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Aplica a regra da montagem manual (RF12): alimento sem substituto seguro bloqueia;
    /// alimento com substituto exige confirmação explícita, registrada de forma append-only.
    /// </summary>
    private string? ValidarIncompatibilidades(
        Cardapio geral,
        IReadOnlyList<GrupoDeAdaptacao> grupos,
        IReadOnlyList<Alimento> catalogo,
        IReadOnlyList<RestricaoDoCatalogo> restricoes,
        bool confirmar,
        string autor,
        DateTime utc)
    {
        if (grupos.Count == 0)
        {
            return null;
        }

        var achados = AdaptacaoDeCardapio.VerificarAdaptabilidade(geral, grupos, catalogo, restricoes);

        var semSubstituto = achados.Where(achado => !achado.TemSubstitutoSeguro).ToList();

        if (semSubstituto.Count > 0)
        {
            var detalhes = string.Join(
                "; ",
                semSubstituto
                    .Select(achado =>
                        $"{achado.AlimentoNome} (escola {achado.EscolaNome}, conjunto {achado.Conjunto.Chave})")
                    .Distinct(StringComparer.Ordinal));

            return $"Há alimento sem substituto seguro para o conjunto de restrições: {detalhes}. " +
                   "Ajuste o cardápio antes de salvar.";
        }

        var comSubstituto = achados.Where(achado => achado.TemSubstitutoSeguro).ToList();

        if (comSubstituto.Count == 0)
        {
            return null;
        }

        if (!confirmar)
        {
            var avisos = string.Join(
                "; ",
                comSubstituto
                    .Select(achado =>
                        $"{achado.AlimentoNome} x {string.Join(", ", achado.RestricoesBloqueantes)} " +
                        $"(escola {achado.EscolaNome}, conjunto {achado.Conjunto.Chave})")
                    .Distinct(StringComparer.Ordinal));

            return $"O cardápio contém alimento incompatível com restrição de aluno, mas com substituto seguro: {avisos}. " +
                   "Confirme com confirmarIncompatibilidades=true para salvar.";
        }

        foreach (var grupoPorAlimento in comSubstituto.GroupBy(achado => achado.AlimentoId))
        {
            var primeiro = grupoPorAlimento.First();

            geral.RegistrarConfirmacaoDeIncompatibilidade(
                autor,
                primeiro.AlimentoId,
                primeiro.AlimentoNome,
                grupoPorAlimento.Select(achado => achado.EscolaNome),
                grupoPorAlimento.Select(achado => achado.Conjunto.Chave),
                utc);
        }

        return null;
    }

    private Resultado<CardapioDTO> Carregar(int id)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        return cardapio is null
            ? Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.")
            : Resultado<CardapioDTO>.Ok(ParaDetalhe(cardapio));
    }

    private CardapioDTO ParaDetalhe(Cardapio cardapio)
    {
        var dto = CardapioMapper.ParaDetalhe(cardapio);

        if (!string.IsNullOrWhiteSpace(cardapio.ConjuntoRestricoesChave))
        {
            var catalogo = _restricoes
                .Listar(categoria: null, incluirInativas: true)
                .ToDictionary(restricao => restricao.Id);

            var conjunto = ConjuntoDeRestricoes.DaChave(cardapio.ConjuntoRestricoesChave);

            dto.Restricoes = conjunto.Ids
                .Where(catalogo.ContainsKey)
                .Select(id => new RestricaoDaVersaoDTO
                {
                    Id = id,
                    Nome = catalogo[id].Nome,
                    Categoria = catalogo[id].Categoria
                })
                .ToList();
        }

        if (cardapio.Tipo == TipoCardapio.Geral)
        {
            dto.SemRestricoes = _cardapios.ContarAdaptados(cardapio.Id) == 0;
        }

        return dto;
    }

    /// <summary>
    /// Conta bloqueantes e alertas de um cardápio para o selo da listagem (Spec 014, RF9),
    /// reusando a função pura <see cref="AnaliseCompatibilidade"/> — a severidade nunca é
    /// recalculada aqui (ADR-011). Recebe o cardápio já com refeições e itens carregados.
    /// </summary>
    private (int Bloqueantes, int Alertas) ContarIncompatibilidades(Cardapio cardapio)
    {
        var itens = cardapio.Refeicoes
            .OrderBy(refeicao => refeicao.Ordem)
            .SelectMany(refeicao => refeicao.Itens.Select(item => new ItemAnalisado(
                refeicao.Ordem,
                refeicao.Nome,
                item.AlimentoId,
                item.Alimento?.Nome ?? string.Empty)))
            .ToList();

        var ids = itens.Select(item => item.AlimentoId).Distinct().ToList();

        var vinculosPorAlimento = _alimentos.ObterComVinculosPorIds(ids).ToDictionary(
            alimento => alimento.Id,
            alimento => (IReadOnlyList<VinculoAnalisado>)alimento.Vinculos
                .Where(vinculo => vinculo.Restricao is not null)
                .Select(vinculo => new VinculoAnalisado(
                    vinculo.RestricaoId,
                    vinculo.Restricao!.Nome,
                    vinculo.Restricao.Categoria))
                .ToList());

        var alunos = _alunos.Listar(new FiltroAluno(UnidadeId: cardapio.UnidadeId))
            .Select(aluno => new AlunoAnalisado(aluno.Id, aluno.Nome, aluno.Restricoes))
            .ToList();

        var resultado = AnaliseCompatibilidade.Analisar(itens, vinculosPorAlimento, alunos);

        return (resultado.Bloqueantes.Count, resultado.Alertas.Count);
    }
}
