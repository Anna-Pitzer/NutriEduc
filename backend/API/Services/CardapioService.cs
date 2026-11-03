using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso do cardápio (Spec 012).
///
/// O serviço decide quem lê (RF8), valida a unidade e os alimentos, aplica a regra de
/// alimento desativado (RF4 — que depende do catálogo e do estado anterior, por isso não
/// vive no agregado) e delega cálculo e invariantes ao domínio. Rascunho publicado não é
/// editado nem excluído: as transições pertencem à Spec 015.
/// </summary>
public sealed class CardapioService
{
    private const string SomenteRascunhoEditavel = "Só é possível editar cardápio em rascunho.";
    private const string SomenteRascunhoExcluivel = "Cardápio publicado não pode ser excluído.";

    private readonly ICardapioRepository _cardapios;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public CardapioService(
        ICardapioRepository cardapios,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos,
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _cardapios = cardapios;
        _alimentos = alimentos;
        _alunos = alunos;
        _escolas = escolas;
        _usuarios = usuarios;
        _unidadeDeTrabalho = unidadeDeTrabalho;
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

        if (escopo.Papel == PapelUsuario.Merendeira)
        {
            if (escopo.UnidadeId is not { } unidadeDoUsuario ||
                (unidadeId is { } solicitada && solicitada != unidadeDoUsuario))
            {
                return Resultado<PaginaDTO<CardapioResumoDTO>>.Proibido();
            }

            unidadeId = unidadeDoUsuario;
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);

        var (itens, total) = _cardapios.Listar(
            new FiltroCardapio(unidadeId, data, situacaoFiltro),
            paginaEfetiva,
            tamanhoEfetivo);

        var resumos = itens.Select(cardapio =>
        {
            var resumo = ParaResumo(cardapio);
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

        if (escopo.Papel == PapelUsuario.Merendeira && escopo.UnidadeId != cardapio.UnidadeId)
        {
            return Resultado<CardapioDTO>.Proibido();
        }

        return Resultado<CardapioDTO>.Ok(ParaDetalhe(cardapio));
    }

    /// <summary>Rota pública (RF9): sem sessão, exige unidade e devolve só publicados.</summary>
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
            Itens = itens.Select(ParaDetalhe).ToList()
        });
    }

    // ---------- Escrita ----------

    public Resultado<CardapioDTO> Criar(SalvarCardapioDTO dto, EscopoUsuario escopo)
    {
        if (_escolas.GetUnidade(dto.UnidadeId) is null)
        {
            return Resultado<CardapioDTO>.Invalido("Unidade não encontrada.");
        }

        var refeicoes = MontarRefeicoes(dto, jaExistentes: null, out var erro);

        if (erro is not null)
        {
            return Resultado<CardapioDTO>.Invalido(erro);
        }

        var cardapio = new Cardapio(dto.Nome, dto.UnidadeId, Periodo(dto.Periodo), refeicoes);

        cardapio.RegistrarCriacao(NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Add(cardapio));

        return Carregar(cardapio.Id);
    }

    public Resultado<CardapioDTO> Atualizar(int id, SalvarCardapioDTO dto, EscopoUsuario escopo)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        if (cardapio is null)
        {
            return Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (cardapio.Situacao != SituacaoCardapio.Rascunho)
        {
            return Resultado<CardapioDTO>.Conflito(SomenteRascunhoEditavel);
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
        cardapio.RegistrarEdicao(NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Update(cardapio));

        return Carregar(cardapio.Id);
    }

    public Resultado<bool> Excluir(int id)
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

        var nome = string.IsNullOrWhiteSpace(dto.Nome) ? origem.Nome : dto.Nome!;
        var copia = Cardapio.Copiar(origem, nome, Periodo(dto.Periodo), duplicadoDeId: origem.Id);

        copia.RegistrarDuplicacao(NomeDoAutor(escopo));

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _cardapios.Add(copia));

        return Carregar(copia.Id);
    }

    // ---------- Apoio ----------

    private static bool PodeLer(EscopoUsuario escopo) =>
        escopo.Papel is PapelUsuario.Nutricionista or PapelUsuario.Merendeira;

    /// <summary>Snapshot do autor do evento: o nome é copiado, nunca referenciado por FK.</summary>
    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

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

    private Resultado<CardapioDTO> Carregar(int id)
    {
        var cardapio = _cardapios.ObterComRefeicoes(id);

        return cardapio is null
            ? Resultado<CardapioDTO>.NaoEncontrado("Cardápio não encontrado.")
            : Resultado<CardapioDTO>.Ok(ParaDetalhe(cardapio));
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

    private static CardapioResumoDTO ParaResumo(Cardapio cardapio) =>
        CardapioMapper.ParaResumo(cardapio);

    private static CardapioDTO ParaDetalhe(Cardapio cardapio) =>
        CardapioMapper.ParaDetalhe(cardapio);
}
