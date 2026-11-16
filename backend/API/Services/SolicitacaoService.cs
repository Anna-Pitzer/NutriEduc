using System.Globalization;
using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da solicitação (Spec 017).
///
/// Concentra três decisões que não pertencem ao agregado: quem pode criar cada tipo, qual
/// papel decide cada tipo e qual é o efeito da aprovação. O agregado permanece dono das
/// transições e do histórico; o efeito usa os donos já existentes (AlunoService para
/// restrições, Spec 015 para cardápio) e nunca uma escrita paralela.
/// </summary>
public sealed class SolicitacaoService
{
    private readonly ISolicitacaoRepository _solicitacoes;
    private readonly IAlunoRepository _alunos;
    private readonly ICardapioRepository _cardapios;
    private readonly IEscolaRepository _escolas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly AlunoService _alunoService;
    private readonly NotificacaoService _notificacoes;

    public SolicitacaoService(
        ISolicitacaoRepository solicitacoes,
        IAlunoRepository alunos,
        ICardapioRepository cardapios,
        IEscolaRepository escolas,
        IUsuarioRepository usuarios,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        AlunoService alunoService,
        NotificacaoService notificacoes)
    {
        _solicitacoes = solicitacoes;
        _alunos = alunos;
        _cardapios = cardapios;
        _escolas = escolas;
        _usuarios = usuarios;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _alunoService = alunoService;
        _notificacoes = notificacoes;
    }

    // ---------- Criação ----------

    public Resultado<SolicitacaoDTO> Criar(CriarSolicitacaoDTO dto, EscopoUsuario escopo)
    {
        if (dto.Tipo is not { } tipo || !Enum.IsDefined(tipo))
        {
            return Resultado<SolicitacaoDTO>.Invalido("Informe um tipo de solicitação válido.");
        }

        if (!PodeCriar(escopo.Papel, tipo))
        {
            return Resultado<SolicitacaoDTO>.Proibido();
        }

        if (tipo == TipoSolicitacao.DadosCadastrais)
        {
            return CriarDadosCadastrais(dto, escopo);
        }

        int unidadeId;
        int? escolaId;
        int? cardapioId = null;
        string? alunoNome = null;
        string? cardapioNome = null;
        Aluno? aluno = null;

        switch (tipo)
        {
            case TipoSolicitacao.MudancaCardapio:
            {
                if (dto.CardapioId is not { } idCardapio || idCardapio <= 0)
                {
                    return Resultado<SolicitacaoDTO>.Invalido("Informe o cardápio da solicitação.");
                }

                var cardapio = _cardapios.ObterComRefeicoes(idCardapio);

                if (cardapio is null || cardapio.UnidadeId != escopo.UnidadeId)
                {
                    return Resultado<SolicitacaoDTO>.NaoEncontrado("Cardápio não encontrado.");
                }

                unidadeId = cardapio.UnidadeId;
                escolaId = escopo.EscolaId;
                cardapioId = cardapio.Id;
                cardapioNome = cardapio.Nome;
                break;
            }

            case TipoSolicitacao.Ingrediente:
            case TipoSolicitacao.FaltaIngrediente:
            {
                if (escopo.UnidadeId is not { } unidadeDoEscopo)
                {
                    return Resultado<SolicitacaoDTO>.Proibido();
                }

                unidadeId = unidadeDoEscopo;
                escolaId = escopo.EscolaId;
                break;
            }

            case TipoSolicitacao.InclusaoAlergia:
            case TipoSolicitacao.AlteracaoRestricao:
            {
                if (dto.AlunoId is not { } idAluno || idAluno <= 0)
                {
                    return Resultado<SolicitacaoDTO>.Invalido("Informe o aluno da solicitação.");
                }

                var resolucao = ResolverAlunoParaCriacao(idAluno, escopo);

                if (resolucao.Status != ResultadoStatus.Ok)
                {
                    return Rejeitar<SolicitacaoDTO>(resolucao);
                }

                aluno = resolucao.Valor!;

                var contexto = ContextoDoAluno(aluno);

                if (contexto is null)
                {
                    return Resultado<SolicitacaoDTO>.Invalido(
                        "O aluno não possui escola e unidade válidas.");
                }

                if (escopo.Papel is not PapelUsuario.Responsavel &&
                    escopo.UnidadeId != contexto.Value.UnidadeId)
                {
                    return Resultado<SolicitacaoDTO>.NaoEncontrado("Aluno não encontrado.");
                }

                // RF20: o mesmo aluno não pode ter dois pedidos abertos para a mesma restrição.
                var restricaoSolicitada = (dto.RestricaoNome ?? string.Empty).Trim();

                if (restricaoSolicitada.Length > 0 &&
                    _solicitacoes.IdDaAbertaParaRestricao(aluno.Id, restricaoSolicitada)
                        is { } idDaAberta)
                {
                    return Resultado<SolicitacaoDTO>.Conflito(
                        $"Já existe solicitação aberta para a restrição " +
                        $"'{restricaoSolicitada}': #{idDaAberta}.");
                }

                unidadeId = contexto.Value.UnidadeId;
                escolaId = aluno.EscolaId;
                alunoNome = aluno.Nome;
                break;
            }

            default:
                return Resultado<SolicitacaoDTO>.Invalido("Tipo de solicitação não suportado.");
        }

        Solicitacao solicitacao;

        try
        {
            solicitacao = new Solicitacao(
                tipo,
                unidadeId,
                escolaId,
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                dto.Descricao,
                aluno?.Id ?? dto.AlunoId,
                cardapioId,
                dto.RestricaoNome,
                dto.Ingrediente,
                alunoNome,
                cardapioNome,
                DateTime.UtcNow,
                dto.Anafilaxia);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<SolicitacaoDTO>.Invalido(excecao.Message);
        }

        RegistrarSolicitacao(solicitacao);

        return Carregar(solicitacao.Id, escopo);
    }

    // ---------- Leitura ----------

    public Resultado<PaginaDTO<SolicitacaoResumoDTO>> Listar(
        string? tipo,
        string? status,
        int? unidadeId,
        int? escolaId,
        int? alunoId,
        int? cardapioId,
        DateTime? de,
        DateTime? ate,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (!TentarLerTipo(tipo, out var tipoFiltro))
        {
            return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Invalido(
                $"Tipo inválido: '{tipo}'.");
        }

        if (!TentarLerStatus(status, out var statusFiltro))
        {
            return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Invalido(
                $"Status inválido: '{status}'.");
        }

        var filtro = new FiltroSolicitacao(
            Tipo: tipoFiltro,
            Status: statusFiltro,
            AlunoId: alunoId,
            CardapioId: cardapioId,
            De: de,
            Ate: ate);

        switch (escopo.Papel)
        {
            case PapelUsuario.AdminSecretaria:
            case PapelUsuario.Nutricionista:
            {
                if (escopo.UnidadeId is not { } unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
                }

                if (unidadeId is { } pedida && pedida != unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
                }

                filtro = filtro with { UnidadeId = unidadeDoEscopo };

                if (escopo.Papel == PapelUsuario.Nutricionista)
                {
                    filtro = filtro with
                    {
                        TiposExcluidos = new[] { TipoSolicitacao.DadosCadastrais }
                    };
                }

                break;
            }

            case PapelUsuario.AdminEscola:
            case PapelUsuario.Merendeira:
            {
                if (escopo.UnidadeId is not { } unidadeDoEscopo ||
                    escopo.EscolaId is not { } escolaDoEscopo)
                {
                    return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
                }

                if (unidadeId is { } pedida && pedida != unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
                }

                if (escolaId is { } escolaPedida && escolaPedida != escolaDoEscopo)
                {
                    return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
                }

                filtro = filtro with
                {
                    UnidadeId = unidadeDoEscopo,
                    EscolaId = escolaDoEscopo
                };

                if (escopo.Papel == PapelUsuario.Merendeira)
                {
                    filtro = filtro with
                    {
                        TiposExcluidos = new[] { TipoSolicitacao.DadosCadastrais }
                    };
                }

                break;
            }

            case PapelUsuario.Responsavel:
            {
                if (unidadeId is not null || escolaId is not null)
                {
                    return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
                }

                filtro = filtro with { ResponsavelUsuarioId = escopo.UsuarioId };
                break;
            }

            default:
                return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Proibido();
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _solicitacoes.Listar(filtro, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<SolicitacaoResumoDTO>>.Ok(
            new PaginaDTO<SolicitacaoResumoDTO>
            {
                Pagina = paginaEfetiva,
                Tamanho = tamanhoEfetivo,
                Total = total,
                Itens = itens.Select(ParaResumo).ToList()
            });
    }

    public Resultado<SolicitacaoDTO> Obter(int id, EscopoUsuario escopo) =>
        Carregar(id, escopo);

    // ---------- Decisão ----------

    public Resultado<SolicitacaoDTO> Analisar(int id, EscopoUsuario escopo) =>
        Transicionar(id, escopo, solicitacao =>
            solicitacao.IniciarAnalise(NomeDoAutor(escopo), DateTime.UtcNow));

    /// <summary>
    /// Confirma a anafilaxia do aluno a partir de uma solicitação de inclusão de alergia
    /// (Spec 017/023), enquanto ela ainda está aberta ou em análise. Pode ser usada pelo
    /// ADM da unidade (dono da decisão), pelo Nutricionista da unidade (responsável
    /// clínico) e pelo Responsável do próprio pedido/família. O evento entra na trilha do
    /// aluno com a origem <c>Solicitacao:&lt;id&gt;</c>; a decisão da solicitação não muda.
    /// </summary>
    public Resultado<SolicitacaoDTO> MarcarAnafilaxia(int id, EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.ObterComHistorico(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (solicitacao.Tipo != TipoSolicitacao.InclusaoAlergia)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "Somente solicitações de inclusão de alergia podem marcar anafilaxia.");
        }

        if (escopo.Papel == PapelUsuario.Responsavel)
        {
            // O responsável só marca no pedido da própria família (solicitante ou vínculo).
            if (NegacaoDeVisaoDoResponsavel(solicitacao, escopo) is not null)
            {
                return Resultado<SolicitacaoDTO>.Proibido();
            }
        }
        else if (escopo.Papel is PapelUsuario.AdminSecretaria or PapelUsuario.Nutricionista)
        {
            if (solicitacao.UnidadeId != escopo.UnidadeId)
            {
                return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
            }
        }
        else
        {
            return Resultado<SolicitacaoDTO>.Proibido();
        }

        if (solicitacao.Status is not (StatusSolicitacao.Aberta or StatusSolicitacao.EmAnalise))
        {
            return Resultado<SolicitacaoDTO>.Conflito("A solicitação já foi decidida.");
        }

        var aluno = solicitacao.AlunoId is { } alunoId
            ? _alunos.ObterPorId(alunoId)
            : null;

        if (aluno is null)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "Aluno da solicitação não encontrado.");
        }

        var aplicacao = _alunoService.MarcarAnafilaxia(
            aluno,
            NomeDoAutor(escopo),
            $"Solicitacao:{solicitacao.Id}");

        if (aplicacao.Status != ResultadoStatus.Ok)
        {
            return Resultado<SolicitacaoDTO>.Invalido(aplicacao.Erro!);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _alunos.UpdateAluno(aluno));

        return Carregar(id, escopo);
    }

    public Resultado<SolicitacaoDTO> Aprovar(
        int id,
        DecidirSolicitacaoDTO dto,
        EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.ObterComHistorico(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (NegacaoDeDecisao(solicitacao, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.")
                : Resultado<SolicitacaoDTO>.Proibido();
        }

        if (solicitacao.Status != StatusSolicitacao.EmAnalise)
        {
            return Resultado<SolicitacaoDTO>.Conflito(
                "Somente solicitação em análise pode ser aprovada.");
        }

        var autor = NomeDoAutor(escopo);
        Aluno? aluno = null;

        if (solicitacao.Tipo is TipoSolicitacao.InclusaoAlergia
            or TipoSolicitacao.AlteracaoRestricao)
        {
            aluno = solicitacao.AlunoId is { } alunoId
                ? _alunos.ObterPorId(alunoId)
                : null;

            if (aluno is null)
            {
                return Resultado<SolicitacaoDTO>.Invalido(
                    "Aluno da solicitação não encontrado.");
            }

            var aplicacao = _alunoService.AplicarRestricaoAprovada(
                aluno,
                solicitacao.RestricaoNome ?? string.Empty,
                autor);

            if (aplicacao.Status != ResultadoStatus.Ok)
            {
                return Resultado<SolicitacaoDTO>.Invalido(aplicacao.Erro!);
            }

            // O solicitante informou anafilaxia: a marcação é aplicada ao aluno junto com a
            // restrição, na mesma transação (Spec 035, revisão).
            if (solicitacao.Anafilaxia)
            {
                var anafilaxia = _alunoService.MarcarAnafilaxia(
                    aluno,
                    autor,
                    $"Solicitacao:{solicitacao.Id}");

                if (anafilaxia.Status != ResultadoStatus.Ok)
                {
                    return Resultado<SolicitacaoDTO>.Invalido(anafilaxia.Erro!);
                }
            }
        }

        if (solicitacao.Tipo == TipoSolicitacao.DadosCadastrais)
        {
            return AprovarDadosCadastrais(solicitacao, dto, escopo, autor);
        }

        try
        {
            solicitacao.Aprovar(dto.Resposta ?? string.Empty, autor, DateTime.UtcNow);
            solicitacao.Concluir(autor, DateTime.UtcNow);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<SolicitacaoDTO>.Invalido(excecao.Message);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<SolicitacaoDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _solicitacoes.Update(solicitacao);

            if (aluno is not null)
            {
                _alunos.UpdateAluno(aluno);
            }

            NotificarMudanca(solicitacao, escopo);
        });

        return Carregar(id, escopo);
    }

    public Resultado<SolicitacaoDTO> Recusar(
        int id,
        DecidirSolicitacaoDTO dto,
        EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.ObterComHistorico(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (solicitacao.Tipo == TipoSolicitacao.DadosCadastrais &&
            (dto.Resposta ?? string.Empty).Trim().Length < 5)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "A recusa de dados cadastrais exige motivo com pelo menos 5 caracteres.");
        }

        return Transicionar(id, escopo, alvo =>
            alvo.Recusar(dto.Resposta ?? string.Empty, NomeDoAutor(escopo), DateTime.UtcNow));
    }

    public Resultado<SolicitacaoDTO> Concluir(int id, EscopoUsuario escopo) =>
        Transicionar(id, escopo, solicitacao =>
            solicitacao.Concluir(NomeDoAutor(escopo), DateTime.UtcNow));

    public Resultado<SolicitacaoDTO> Cancelar(int id, EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.ObterComHistorico(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (solicitacao.SolicitanteId != escopo.UsuarioId)
        {
            return Resultado<SolicitacaoDTO>.Proibido();
        }

        try
        {
            solicitacao.Cancelar(NomeDoAutor(escopo), DateTime.UtcNow);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<SolicitacaoDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _solicitacoes.Update(solicitacao);
            NotificarMudanca(solicitacao, escopo);
        });

        return Carregar(id, escopo);
    }

    // ---------- Dados cadastrais (Spec 030) ----------

    private static readonly IReadOnlyDictionary<string, CampoDeDadoDoAluno> CamposDeDados =
        new Dictionary<string, CampoDeDadoDoAluno>(StringComparer.OrdinalIgnoreCase)
        {
            ["nome"] = CampoDeDadoDoAluno.Nome,
            ["dataNascimento"] = CampoDeDadoDoAluno.DataNascimento,
            ["nomeResponsavel"] = CampoDeDadoDoAluno.NomeResponsavel,
            ["telefoneResponsavel"] = CampoDeDadoDoAluno.TelefoneResponsavel,
            ["observacao"] = CampoDeDadoDoAluno.Observacao
        };

    private Resultado<SolicitacaoDTO> CriarDadosCadastrais(
        CriarSolicitacaoDTO dto,
        EscopoUsuario escopo)
    {
        if (dto.AlunoId is not { } alunoId || alunoId <= 0)
        {
            return Resultado<SolicitacaoDTO>.Invalido("Informe o aluno da solicitação.");
        }

        var resolucao = ResolverAlunoParaCriacao(alunoId, escopo);

        if (resolucao.Status != ResultadoStatus.Ok)
        {
            return Rejeitar<SolicitacaoDTO>(resolucao);
        }

        var aluno = resolucao.Valor!;
        var contexto = ContextoDoAluno(aluno);

        if (contexto is null)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "O aluno não possui escola e unidade válidas.");
        }

        var justificativa = (dto.Justificativa ?? string.Empty).Trim();

        if (justificativa.Length is < 10 or > 500)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "A justificativa deve ter entre 10 e 500 caracteres.");
        }

        if (dto.Alteracoes is not { Count: > 0 } entradas)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "Informe ao menos uma alteração de campo.");
        }

        var alteracoes = new List<AlteracaoSolicitada>();
        var campos = new List<CampoDeDadoDoAluno>();

        foreach (var entrada in entradas)
        {
            if (!CamposDeDados.TryGetValue((entrada.Campo ?? string.Empty).Trim(), out var campo))
            {
                return Resultado<SolicitacaoDTO>.Invalido(
                    $"Campo inválido: '{entrada.Campo}'. Aceitos: nome, dataNascimento, " +
                    "nomeResponsavel, telefoneResponsavel, observacao.");
            }

            if (campos.Contains(campo))
            {
                return Resultado<SolicitacaoDTO>.Invalido(
                    $"O campo '{campo}' aparece mais de uma vez no pedido.");
            }

            var existente = _solicitacoes.IdDaAbertaParaCampo(alunoId, campo);

            if (existente is { } id)
            {
                return Resultado<SolicitacaoDTO>.Conflito(
                    $"Já existe solicitação aberta para o campo '{campo}': #{id}.");
            }

            var valorSolicitado = (entrada.ValorSolicitado ?? string.Empty).Trim();

            if (valorSolicitado.Length > SolicitacaoAlteracao.ValorMaximo)
            {
                return Resultado<SolicitacaoDTO>.Invalido(
                    $"O valor do campo '{campo}' deve ter no máximo " +
                    $"{SolicitacaoAlteracao.ValorMaximo} caracteres.");
            }

            campos.Add(campo);
            alteracoes.Add(new AlteracaoSolicitada(
                campo,
                valorSolicitado,
                ValorAtual(aluno, campo)));
        }

        Solicitacao solicitacao;

        try
        {
            solicitacao = new Solicitacao(
                TipoSolicitacao.DadosCadastrais,
                contexto.Value.UnidadeId,
                aluno.EscolaId,
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                justificativa,
                alunoId,
                null,
                null,
                null,
                aluno.Nome,
                null,
                DateTime.UtcNow);

            solicitacao.DefinirAlteracoesDeDados(alteracoes);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<SolicitacaoDTO>.Invalido(excecao.Message);
        }

        RegistrarSolicitacao(solicitacao);

        return Carregar(solicitacao.Id, escopo);
    }

    private Resultado<SolicitacaoDTO> AprovarDadosCadastrais(
        Solicitacao solicitacao,
        DecidirSolicitacaoDTO dto,
        EscopoUsuario escopo,
        string autor)
    {
        var aluno = solicitacao.AlunoId is { } alunoId ? _alunos.ObterPorId(alunoId) : null;

        if (aluno is null)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "Aluno da solicitação não encontrado.");
        }

        if (aluno.EscolaId is not { } escolaId)
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "O aluno não possui escola vinculada.");
        }

        var divergentes = solicitacao.Alteracoes
            .Where(alteracao => ValorAtual(aluno, alteracao.Campo) != alteracao.ValorNoPedido)
            .Select(alteracao => alteracao.Campo.ToString())
            .ToList();

        if (divergentes.Count > 0)
        {
            return Resultado<SolicitacaoDTO>.Conflito(
                "O cadastro do aluno mudou desde o pedido nos campos: " +
                string.Join(", ", divergentes) +
                ". Nada foi aplicado; o responsável precisa criar um novo pedido.");
        }

        var nome = ValorSolicitado(solicitacao, aluno, CampoDeDadoDoAluno.Nome);
        var dataTexto = ValorSolicitado(solicitacao, aluno, CampoDeDadoDoAluno.DataNascimento);
        var nomeResponsavel = ValorSolicitado(
            solicitacao, aluno, CampoDeDadoDoAluno.NomeResponsavel);
        var telefoneResponsavel = ValorSolicitado(
            solicitacao, aluno, CampoDeDadoDoAluno.TelefoneResponsavel);
        var observacao = ValorSolicitado(solicitacao, aluno, CampoDeDadoDoAluno.Observacao);

        if (!DateOnly.TryParseExact(
                dataTexto,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var nascimento))
        {
            return Resultado<SolicitacaoDTO>.Invalido(
                "Data de nascimento inválida; use o formato yyyy-MM-dd.");
        }

        try
        {
            var (alteracoes, _, _) = aluno.AtualizarDados(
                nome,
                nascimento,
                aluno.Turma,
                aluno.Telefone,
                observacao,
                aluno.Restricoes,
                aluno.Anafilaxia,
                new Responsavel(nomeResponsavel, telefoneResponsavel),
                aluno.UnidadeId,
                escolaId);

            aluno.RegistrarHistorico(
                alteracoes,
                Array.Empty<string>(),
                Array.Empty<string>(),
                autor,
                $"Solicitacao:{solicitacao.Id}");

            solicitacao.Aprovar(dto.Resposta ?? string.Empty, autor, DateTime.UtcNow);
            solicitacao.Concluir(autor, DateTime.UtcNow);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<SolicitacaoDTO>.Invalido(excecao.Message);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<SolicitacaoDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _solicitacoes.Update(solicitacao);
            _alunos.UpdateAluno(aluno);
            NotificarMudanca(solicitacao, escopo);
        });

        return Carregar(solicitacao.Id, escopo);
    }

    private static string ValorAtual(Aluno aluno, CampoDeDadoDoAluno campo) => campo switch
    {
        CampoDeDadoDoAluno.Nome => aluno.Nome,
        CampoDeDadoDoAluno.DataNascimento =>
            aluno.Nascimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        CampoDeDadoDoAluno.NomeResponsavel => aluno.Responsavel.Nome,
        CampoDeDadoDoAluno.TelefoneResponsavel => aluno.Responsavel.Telefone,
        CampoDeDadoDoAluno.Observacao => aluno.Observacao,
        _ => string.Empty
    };

    private static string ValorSolicitado(
        Solicitacao solicitacao,
        Aluno aluno,
        CampoDeDadoDoAluno campo) =>
        solicitacao.Alteracoes
            .FirstOrDefault(alteracao => alteracao.Campo == campo)
            ?.ValorSolicitado ?? ValorAtual(aluno, campo);

    // ---------- Apoio ----------

    private Resultado<SolicitacaoDTO> Transicionar(
        int id,
        EscopoUsuario escopo,
        Action<Solicitacao> transicao)
    {
        var solicitacao = _solicitacoes.ObterComHistorico(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (NegacaoDeDecisao(solicitacao, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.")
                : Resultado<SolicitacaoDTO>.Proibido();
        }

        try
        {
            transicao(solicitacao);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<SolicitacaoDTO>.Invalido(excecao.Message);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<SolicitacaoDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _solicitacoes.Update(solicitacao);
            NotificarMudanca(solicitacao, escopo);
        });

        return Carregar(id, escopo);
    }

    private Resultado<SolicitacaoDTO> Carregar(int id, EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.ObterComHistorico(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (NegacaoDeVisao(solicitacao, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<SolicitacaoDTO>.NaoEncontrado("Solicitação não encontrada.")
                : Resultado<SolicitacaoDTO>.Proibido();
        }

        var detalhe = ParaDetalhe(solicitacao);
        return Resultado<SolicitacaoDTO>.Ok(detalhe);
    }

    private Resultado<Aluno> ResolverAlunoParaCriacao(int alunoId, EscopoUsuario escopo)
    {
        var filtro = escopo.Papel switch
        {
            PapelUsuario.Responsavel =>
                new FiltroAluno(ResponsavelUsuarioId: escopo.UsuarioId),
            PapelUsuario.AdminEscola or PapelUsuario.Merendeira =>
                new FiltroAluno(EscolaId: escopo.EscolaId),
            PapelUsuario.Nutricionista =>
                new FiltroAluno(UnidadeId: escopo.UnidadeId),
            _ => FiltroAluno.Nenhum
        };

        var aluno = _alunos.Obter(alunoId, filtro);

        return aluno is null
            ? Resultado<Aluno>.Proibido()
            : Resultado<Aluno>.Ok(aluno);
    }

    private static (int UnidadeId, int EscolaId)? ContextoDoAluno(Aluno aluno)
    {
        if (aluno.UnidadeId is not { } unidadeId || aluno.EscolaId is not { } escolaId)
        {
            return null;
        }

        return (unidadeId, escolaId);
    }

    private static Resultado<T> Rejeitar<T>(Resultado<Aluno> origem) =>
        origem.Status switch
        {
            ResultadoStatus.NaoEncontrado => Resultado<T>.NaoEncontrado(origem.Erro!),
            ResultadoStatus.Proibido => Resultado<T>.Proibido(),
            _ => Resultado<T>.Invalido(origem.Erro ?? "Aluno inválido.")
        };

    private static bool PodeCriar(PapelUsuario papel, TipoSolicitacao tipo) => tipo switch
    {
        TipoSolicitacao.DadosCadastrais => papel == PapelUsuario.Responsavel,
        TipoSolicitacao.InclusaoAlergia or TipoSolicitacao.AlteracaoRestricao =>
            papel == PapelUsuario.Responsavel,
        _ => papel is PapelUsuario.Merendeira
            or PapelUsuario.AdminEscola
            or PapelUsuario.Nutricionista
    };

    private ResultadoStatus? NegacaoDeVisao(Solicitacao solicitacao, EscopoUsuario escopo) =>
        escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria =>
                solicitacao.UnidadeId == escopo.UnidadeId
                    ? null
                    : ResultadoStatus.NaoEncontrado,

            PapelUsuario.Nutricionista =>
                solicitacao.UnidadeId != escopo.UnidadeId
                    ? ResultadoStatus.NaoEncontrado
                    : solicitacao.Tipo == TipoSolicitacao.DadosCadastrais
                        ? ResultadoStatus.Proibido
                        : null,

            PapelUsuario.AdminEscola =>
                solicitacao.UnidadeId != escopo.UnidadeId
                    ? ResultadoStatus.NaoEncontrado
                    : solicitacao.EscolaId == escopo.EscolaId
                        ? null
                        : ResultadoStatus.Proibido,

            PapelUsuario.Merendeira =>
                solicitacao.Tipo == TipoSolicitacao.DadosCadastrais
                    ? ResultadoStatus.Proibido
                    : solicitacao.UnidadeId != escopo.UnidadeId
                        ? ResultadoStatus.NaoEncontrado
                        : solicitacao.EscolaId == escopo.EscolaId
                            ? null
                            : ResultadoStatus.Proibido,

            PapelUsuario.Responsavel => NegacaoDeVisaoDoResponsavel(solicitacao, escopo),

            _ => ResultadoStatus.Proibido
        };

    private ResultadoStatus? NegacaoDeVisaoDoResponsavel(
        Solicitacao solicitacao,
        EscopoUsuario escopo)
    {
        if (solicitacao.SolicitanteId == escopo.UsuarioId)
        {
            return null;
        }

        if (solicitacao.AlunoId is not { } alunoId)
        {
            return ResultadoStatus.Proibido;
        }

        return _alunos.Obter(
            alunoId,
            new FiltroAluno(ResponsavelUsuarioId: escopo.UsuarioId)) is not null
            ? null
            : ResultadoStatus.Proibido;
    }

    private static ResultadoStatus? NegacaoDeDecisao(
        Solicitacao solicitacao,
        EscopoUsuario escopo)
    {
        var donoDoTipo = DonoDoTipo(solicitacao.Tipo);

        if (escopo.Papel != donoDoTipo)
        {
            return ResultadoStatus.Proibido;
        }

        return solicitacao.UnidadeId == escopo.UnidadeId
            ? null
            : ResultadoStatus.NaoEncontrado;
    }

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    /// <summary>
    /// Emite a notificação de status na mesma unidade de trabalho da transição (Spec 018,
    /// RF8). O conteúdo é o tipo e o status da solicitação, sem dado clínico.
    /// </summary>
    private void NotificarMudanca(Solicitacao solicitacao, EscopoUsuario escopo) =>
        _notificacoes.NotificarSolicitacao(
            solicitacao.SolicitanteId,
            escopo.UsuarioId,
            solicitacao.Id,
            solicitacao.Tipo,
            solicitacao.Status,
            DateTime.UtcNow);

    /// <summary>
    /// Grava a solicitação e, na mesma transação, avisa quem decide o tipo. O id é
    /// persistido antes da emissão para que a notificação referencie a solicitação real.
    /// </summary>
    private void RegistrarSolicitacao(Solicitacao solicitacao) =>
        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _solicitacoes.Add(solicitacao);
            _unidadeDeTrabalho.Salvar();

            NotificarNovaSolicitacao(solicitacao);
        });

    /// <summary>
    /// Notifica os responsáveis pela análise (Nutricionista para cardápio/ingrediente,
    /// AdminSecretaria para alergia/restrição/dado cadastral) da unidade da solicitação.
    /// O solicitante nunca recebe a notificação do próprio pedido.
    /// </summary>
    private void NotificarNovaSolicitacao(Solicitacao solicitacao)
    {
        var destinatarios = _usuarios
            .Listar(new FiltroUsuarios(
                DonoDoTipo(solicitacao.Tipo),
                null,
                solicitacao.UnidadeId,
                true,
                null))
            .Select(usuario => usuario.Id)
            .Where(id => id != solicitacao.SolicitanteId);

        _notificacoes.NotificarVariosUmaVez(
            destinatarios,
            TipoNotificacao.NovaSolicitacao,
            $"Nova solicitação #{solicitacao.Id}",
            $"Solicitação de {solicitacao.Tipo} aguardando análise.",
            "Solicitacao",
            solicitacao.Id,
            DateTime.UtcNow);
    }

    /// <summary>Papel que decide cada tipo de solicitação (RF5 da Spec 017).</summary>
    private static PapelUsuario DonoDoTipo(TipoSolicitacao tipo) =>
        tipo is TipoSolicitacao.MudancaCardapio
            or TipoSolicitacao.Ingrediente
            or TipoSolicitacao.FaltaIngrediente
            ? PapelUsuario.Nutricionista
            : PapelUsuario.AdminSecretaria;

    private static bool TentarLerTipo(string? texto, out TipoSolicitacao? tipo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            tipo = null;
            return true;
        }

        if (Enum.TryParse<TipoSolicitacao>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            tipo = valor;
            return true;
        }

        tipo = null;
        return false;
    }

    private static bool TentarLerStatus(string? texto, out StatusSolicitacao? status)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<StatusSolicitacao>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            status = valor;
            return true;
        }

        status = null;
        return false;
    }

    private SolicitacaoResumoDTO ParaResumo(Solicitacao solicitacao) =>
        new()
        {
            Id = solicitacao.Id,
            Tipo = solicitacao.Tipo,
            Status = solicitacao.Status,
            UnidadeId = solicitacao.UnidadeId,
            EscolaId = solicitacao.EscolaId,
            AlunoId = solicitacao.AlunoId,
            Aluno = solicitacao.AlunoNome,
            CardapioId = solicitacao.CardapioId,
            Cardapio = solicitacao.CardapioNome,
            RestricaoNome = solicitacao.RestricaoNome,
            Ingrediente = solicitacao.Ingrediente,
            Descricao = solicitacao.Descricao,
            SolicitanteId = solicitacao.SolicitanteId,
            Solicitante = solicitacao.SolicitanteNome,
            Resposta = solicitacao.Resposta,
            RespondidoPor = solicitacao.RespondidoPor,
            CriadoEm = solicitacao.CriadoEmUtc
        };

    private SolicitacaoDTO ParaDetalhe(Solicitacao solicitacao)
    {
        var resumo = ParaResumo(solicitacao);

        return new SolicitacaoDTO
        {
            Id = resumo.Id,
            Tipo = resumo.Tipo,
            Status = resumo.Status,
            UnidadeId = resumo.UnidadeId,
            EscolaId = resumo.EscolaId,
            AlunoId = resumo.AlunoId,
            Aluno = resumo.Aluno,
            CardapioId = resumo.CardapioId,
            Cardapio = resumo.Cardapio,
            RestricaoNome = resumo.RestricaoNome,
            Ingrediente = resumo.Ingrediente,
            Descricao = resumo.Descricao,
            Solicitante = resumo.Solicitante,
            Resposta = resumo.Resposta,
            RespondidoPor = resumo.RespondidoPor,
            CriadoEm = resumo.CriadoEm,
            Anafilaxia = solicitacao.Anafilaxia,
            Alteracoes = solicitacao.Alteracoes
                .Select(alteracao => new AlteracaoDeDadosDTO
                {
                    Campo = alteracao.Campo,
                    ValorSolicitado = alteracao.ValorSolicitado,
                    ValorNoPedido = alteracao.ValorNoPedido
                })
                .ToList(),
            Aviso = AvisoDoTipo(solicitacao.Tipo),
            Historico = solicitacao.Historico
                .OrderBy(evento => evento.CriadoEmUtc)
                .ThenBy(evento => evento.Id)
                .Select(evento => new HistoricoSolicitacaoDTO
                {
                    Status = evento.Status,
                    Autor = evento.AutorNome,
                    Resposta = evento.Resposta,
                    CriadoEm = evento.CriadoEmUtc
                })
                .ToList()
        };
    }

    private static string? AvisoDoTipo(TipoSolicitacao tipo) => tipo switch
    {
        TipoSolicitacao.MudancaCardapio =>
            "A aprovação não altera o cardápio; a edição é feita pela Spec 015.",
        TipoSolicitacao.Ingrediente or TipoSolicitacao.FaltaIngrediente =>
            "A aprovação registra a pauta; nenhum dado é alterado automaticamente.",
        _ => null
    };
}
