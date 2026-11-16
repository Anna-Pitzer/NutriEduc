using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da ocorrência (Spec 016). Concentra a autorização de criação por vínculo, a
/// derivação de <see cref="Ocorrencia.EscolaId"/>/<see cref="Ocorrencia.UnidadeId"/>, o
/// escopo de listagem e a separação de quem muda status por papel. O agregado permanece dono
/// das transições e do histórico.
/// </summary>
public sealed class OcorrenciaService
{
    private readonly IOcorrenciaRepository _ocorrencias;
    private readonly IAlunoRepository _alunos;
    private readonly ICardapioRepository _cardapios;
    private readonly IAlimentoRepository _alimentos;
    private readonly IVinculoRepository _vinculos;
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;
    private readonly NotificacaoService _notificacoes;

    public OcorrenciaService(
        IOcorrenciaRepository ocorrencias,
        IAlunoRepository alunos,
        ICardapioRepository cardapios,
        IAlimentoRepository alimentos,
        IVinculoRepository vinculos,
        IUsuarioRepository usuarios,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio,
        NotificacaoService notificacoes)
    {
        _ocorrencias = ocorrencias;
        _alunos = alunos;
        _cardapios = cardapios;
        _alimentos = alimentos;
        _vinculos = vinculos;
        _usuarios = usuarios;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
        _notificacoes = notificacoes;
    }

    // ---------- Criação ----------

    public Resultado<OcorrenciaDTO> Criar(CriarOcorrenciaDTO dto, EscopoUsuario escopo)
    {
        if (dto.Tipo is not { } tipo || !Enum.IsDefined(tipo))
        {
            return Resultado<OcorrenciaDTO>.Invalido("Informe um tipo de ocorrência válido.");
        }

        if (!PodeCriar(escopo.Papel))
        {
            return Resultado<OcorrenciaDTO>.Proibido();
        }

        if (escopo.Papel == PapelUsuario.Responsavel && tipo != TipoOcorrencia.Aluno)
        {
            return Resultado<OcorrenciaDTO>.Proibido();
        }

        int unidadeId;
        int? escolaId;
        string? alunoNome = null;
        string? cardapioNome = null;
        string? refeicaoNome = null;
        string? alimentoNome = null;

        switch (tipo)
        {
            case TipoOcorrencia.Aluno:
            {
                if (dto.AlunoId is not { } idAluno || idAluno <= 0)
                {
                    return Resultado<OcorrenciaDTO>.Invalido("Informe o aluno da ocorrência.");
                }

                var resolucao = ResolverAlunoParaOcorrencia(idAluno, escopo);

                if (resolucao.Status != ResultadoStatus.Ok)
                {
                    return Rejeitar<OcorrenciaDTO>(resolucao);
                }

                var aluno = resolucao.Valor!;
                unidadeId = aluno.UnidadeId!.Value;
                escolaId = aluno.EscolaId;
                alunoNome = aluno.Nome;
                break;
            }

            case TipoOcorrencia.Refeicao:
            {
                if (dto.CardapioId is not { } idCardapio || idCardapio <= 0)
                {
                    return Resultado<OcorrenciaDTO>.Invalido("Informe o cardápio da ocorrência.");
                }

                if (dto.RefeicaoId is not { } idRefeicao || idRefeicao <= 0)
                {
                    return Resultado<OcorrenciaDTO>.Invalido("Informe a refeição da ocorrência.");
                }

                var cardapio = _cardapios.ObterComRefeicoes(idCardapio);

                if (cardapio is null || (escopo.UnidadeId is { } unidadeEscopo &&
                                         cardapio.UnidadeId != unidadeEscopo))
                {
                    return Resultado<OcorrenciaDTO>.NaoEncontrado("Cardápio não encontrado.");
                }

                if (cardapio.Refeicoes.All(refeicao => refeicao.Id != idRefeicao))
                {
                    return Resultado<OcorrenciaDTO>.Invalido(
                        "A refeição não pertence ao cardápio informado.");
                }

                unidadeId = cardapio.UnidadeId;
                escolaId = escopo.EscolaId;
                cardapioNome = cardapio.Nome;
                refeicaoNome = cardapio.Refeicoes.First(refeicao => refeicao.Id == idRefeicao).Nome;
                break;
            }

            case TipoOcorrencia.Alimento:
            {
                if (dto.AlimentoId is not { } idAlimento || idAlimento <= 0)
                {
                    return Resultado<OcorrenciaDTO>.Invalido("Informe o alimento da ocorrência.");
                }

                var alimento = _alimentos.Obter(idAlimento);

                if (alimento is null)
                {
                    return Resultado<OcorrenciaDTO>.Invalido("Alimento não encontrado.");
                }

                if (escopo.UnidadeId is not { } unidadeDoEscopo)
                {
                    return Resultado<OcorrenciaDTO>.Proibido();
                }

                unidadeId = unidadeDoEscopo;
                escolaId = escopo.EscolaId;
                alimentoNome = alimento.Nome;
                break;
            }

            case TipoOcorrencia.Outro:
            {
                if (escopo.UnidadeId is not { } unidadeDoEscopo)
                {
                    return Resultado<OcorrenciaDTO>.Proibido();
                }

                unidadeId = unidadeDoEscopo;
                escolaId = escopo.EscolaId;
                break;
            }

            default:
                return Resultado<OcorrenciaDTO>.Invalido("Tipo de ocorrência não suportado.");
        }

        Ocorrencia ocorrencia;

        try
        {
            ocorrencia = new Ocorrencia(
                tipo,
                unidadeId,
                escolaId,
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                dto.Descricao ?? string.Empty,
                dto.AlunoId,
                dto.CardapioId,
                dto.RefeicaoId,
                dto.AlimentoId,
                alunoNome,
                cardapioNome,
                refeicaoNome,
                alimentoNome,
                _relogio.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<OcorrenciaDTO>.Invalido(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _ocorrencias.Add(ocorrencia);
            _unidadeDeTrabalho.Salvar();

            NotificarOcorrenciaRegistrada(ocorrencia);
        });

        return Carregar(ocorrencia.Id, escopo);
    }

    // ---------- Leitura ----------

    public Resultado<PaginaDTO<OcorrenciaResumoDTO>> Listar(
        string? tipo,
        string? status,
        int? alunoId,
        int? unidadeId,
        int? escolaId,
        DateTime? de,
        DateTime? ate,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (!TentarLerTipo(tipo, out var tipoFiltro))
        {
            return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Invalido(
                $"Tipo inválido: '{tipo}'.");
        }

        if (!TentarLerStatus(status, out var statusFiltro))
        {
            return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Invalido(
                $"Status inválido: '{status}'.");
        }

        var filtro = new FiltroOcorrencia(
            Tipo: tipoFiltro,
            Status: statusFiltro,
            AlunoId: alunoId,
            De: de,
            Ate: ate);

        switch (escopo.Papel)
        {
            case PapelUsuario.AdminSecretaria:
            case PapelUsuario.Nutricionista:
            {
                if (escopo.UnidadeId is not { } unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
                }

                if (unidadeId is { } pedida && pedida != unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
                }

                filtro = filtro with { UnidadeId = unidadeDoEscopo };
                break;
            }

            case PapelUsuario.AdminEscola:
            case PapelUsuario.Merendeira:
            {
                if (escopo.UnidadeId is not { } unidadeDoEscopo ||
                    escopo.EscolaId is not { } escolaDoEscopo)
                {
                    return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
                }

                if (unidadeId is { } pedida && pedida != unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
                }

                if (escolaId is { } escolaPedida && escolaPedida != escolaDoEscopo)
                {
                    return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
                }

                filtro = filtro with { UnidadeId = unidadeDoEscopo, EscolaId = escolaDoEscopo };
                break;
            }

            case PapelUsuario.Responsavel:
            {
                if (unidadeId is not null || escolaId is not null)
                {
                    return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
                }

                filtro = filtro with { ResponsavelUsuarioId = escopo.UsuarioId };
                break;
            }

            default:
                return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Proibido();
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _ocorrencias.Listar(filtro, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<OcorrenciaResumoDTO>>.Ok(
            new PaginaDTO<OcorrenciaResumoDTO>
            {
                Pagina = paginaEfetiva,
                Tamanho = tamanhoEfetivo,
                Total = total,
                Itens = itens.Select(ParaResumo).ToList()
            });
    }

    public Resultado<OcorrenciaDTO> Obter(int id, EscopoUsuario escopo) =>
        Carregar(id, escopo);

    // ---------- Mudança de status e cancelamento ----------

    public Resultado<OcorrenciaDTO> MudarStatus(
        int id,
        MudarStatusOcorrenciaDTO dto,
        EscopoUsuario escopo)
    {
        var ocorrencia = _ocorrencias.ObterComHistorico(id);

        if (ocorrencia is null)
        {
            return Resultado<OcorrenciaDTO>.NaoEncontrado("Ocorrência não encontrada.");
        }

        if (!PodeMudarStatus(escopo.Papel))
        {
            return Resultado<OcorrenciaDTO>.Proibido();
        }

        if (NegacaoDeVisao(ocorrencia, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<OcorrenciaDTO>.NaoEncontrado("Ocorrência não encontrada.")
                : Resultado<OcorrenciaDTO>.Proibido();
        }

        if (dto.Status is not { } destino || !Enum.IsDefined(destino))
        {
            return Resultado<OcorrenciaDTO>.Invalido("Informe um status válido.");
        }

        var utc = _relogio.GetUtcNow().UtcDateTime;

        try
        {
            ocorrencia.MudarStatus(
                destino,
                dto.Observacao ?? string.Empty,
                NomeDoAutor(escopo),
                utc);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<OcorrenciaDTO>.Invalido(excecao.Message);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<OcorrenciaDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _ocorrencias.Update(ocorrencia);
            _notificacoes.NotificarVarios(
                DestinatariosDaOcorrencia(ocorrencia),
                TipoNotificacao.OcorrenciaStatus,
                "Ocorrência atualizada",
                $"A ocorrência #{ocorrencia.Id} mudou para {ocorrencia.Status}.",
                "ocorrencia",
                ocorrencia.Id,
                utc);
        });

        return Carregar(id, escopo);
    }

    public Resultado<OcorrenciaDTO> Cancelar(int id, EscopoUsuario escopo)
    {
        var ocorrencia = _ocorrencias.ObterComHistorico(id);

        if (ocorrencia is null)
        {
            return Resultado<OcorrenciaDTO>.NaoEncontrado("Ocorrência não encontrada.");
        }

        if (ocorrencia.AutorId != escopo.UsuarioId)
        {
            return Resultado<OcorrenciaDTO>.Proibido();
        }

        try
        {
            ocorrencia.Cancelar(NomeDoAutor(escopo), _relogio.GetUtcNow().UtcDateTime);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<OcorrenciaDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _ocorrencias.Update(ocorrencia));

        return Carregar(id, escopo);
    }

    // ---------- Apoio ----------

    private Resultado<OcorrenciaDTO> Carregar(int id, EscopoUsuario escopo)
    {
        var ocorrencia = _ocorrencias.ObterComHistorico(id);

        if (ocorrencia is null)
        {
            return Resultado<OcorrenciaDTO>.NaoEncontrado("Ocorrência não encontrada.");
        }

        if (NegacaoDeVisao(ocorrencia, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<OcorrenciaDTO>.NaoEncontrado("Ocorrência não encontrada.")
                : Resultado<OcorrenciaDTO>.Proibido();
        }

        return Resultado<OcorrenciaDTO>.Ok(ParaDetalhe(ocorrencia, escopo));
    }

    private Resultado<Aluno> ResolverAlunoParaOcorrencia(int alunoId, EscopoUsuario escopo)
    {
        var aluno = _alunos.ObterPorId(alunoId);

        if (aluno is null)
        {
            return Resultado<Aluno>.Invalido("Aluno não encontrado.");
        }

        if (aluno.UnidadeId is not { } unidadeAluno || aluno.EscolaId is not { } escolaAluno)
        {
            return Resultado<Aluno>.Invalido("O aluno não possui escola e unidade válidas.");
        }

        if (escopo.UnidadeId is { } unidadeEscopo && unidadeEscopo != unidadeAluno)
        {
            return Resultado<Aluno>.NaoEncontrado("Aluno não encontrado.");
        }

        if (escopo.EscolaId is { } escolaEscopo && escolaEscopo != escolaAluno)
        {
            return Resultado<Aluno>.Proibido();
        }

        if (escopo.Papel == PapelUsuario.Responsavel &&
            !_vinculos.Existe(alunoId, escopo.UsuarioId))
        {
            return Resultado<Aluno>.Proibido();
        }

        return Resultado<Aluno>.Ok(aluno);
    }

    private static Resultado<T> Rejeitar<T>(Resultado<Aluno> origem) =>
        origem.Status switch
        {
            ResultadoStatus.NaoEncontrado => Resultado<T>.NaoEncontrado(origem.Erro!),
            ResultadoStatus.Proibido => Resultado<T>.Proibido(),
            _ => Resultado<T>.Invalido(origem.Erro ?? "Aluno inválido.")
        };

    private static bool PodeCriar(PapelUsuario papel) =>
        papel is PapelUsuario.Merendeira
            or PapelUsuario.Responsavel
            or PapelUsuario.Nutricionista
            or PapelUsuario.AdminEscola;

    private static bool PodeMudarStatus(PapelUsuario papel) =>
        papel is PapelUsuario.Nutricionista or PapelUsuario.AdminEscola;

    private ResultadoStatus? NegacaoDeVisao(Ocorrencia ocorrencia, EscopoUsuario escopo) =>
        escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria =>
                ocorrencia.UnidadeId == escopo.UnidadeId
                    ? null
                    : ResultadoStatus.NaoEncontrado,

            PapelUsuario.Nutricionista =>
                ocorrencia.UnidadeId != escopo.UnidadeId
                    ? ResultadoStatus.NaoEncontrado
                    : null,

            PapelUsuario.AdminEscola =>
                ocorrencia.UnidadeId != escopo.UnidadeId
                    ? ResultadoStatus.NaoEncontrado
                    : ocorrencia.EscolaId == escopo.EscolaId
                        ? null
                        : ResultadoStatus.Proibido,

            PapelUsuario.Merendeira =>
                ocorrencia.UnidadeId != escopo.UnidadeId
                    ? ResultadoStatus.NaoEncontrado
                    : ocorrencia.EscolaId == escopo.EscolaId
                        ? null
                        : ResultadoStatus.Proibido,

            PapelUsuario.Responsavel => NegacaoDeVisaoDoResponsavel(ocorrencia, escopo),

            _ => ResultadoStatus.Proibido
        };

    private ResultadoStatus? NegacaoDeVisaoDoResponsavel(
        Ocorrencia ocorrencia,
        EscopoUsuario escopo)
    {
        if (ocorrencia.AlunoId is not { } alunoId)
        {
            return ResultadoStatus.Proibido;
        }

        return _vinculos.Existe(alunoId, escopo.UsuarioId)
            ? null
            : ResultadoStatus.Proibido;
    }

    private IEnumerable<int> DestinatariosDaOcorrencia(Ocorrencia ocorrencia)
    {
        var destinatarios = new List<int> { ocorrencia.AutorId };

        if (ocorrencia.AlunoId is { } alunoId)
        {
            destinatarios.AddRange(
                _vinculos.ListarPorAluno(alunoId)
                    .Select(vinculo => vinculo.ResponsavelUsuarioId)
                    .Where(id => _usuarios.GetById(id)?.Ativo == true));
        }

        return destinatarios;
    }

    /// <summary>
    /// Avisa quem pode analisar a ocorrência (Nutricionista da unidade e AdminEscola da
    /// escola) que um novo relato foi registrado. O autor não recebe o próprio relato.
    /// </summary>
    private void NotificarOcorrenciaRegistrada(Ocorrencia ocorrencia)
    {
        var destinatarios = _usuarios
            .Listar(new FiltroUsuarios(
                PapelUsuario.Nutricionista,
                null,
                ocorrencia.UnidadeId,
                true,
                null))
            .Select(usuario => usuario.Id)
            .ToList();

        if (ocorrencia.EscolaId is { } escolaId)
        {
            destinatarios.AddRange(_usuarios
                .Listar(new FiltroUsuarios(
                    PapelUsuario.AdminEscola,
                    escolaId,
                    ocorrencia.UnidadeId,
                    true,
                    null))
                .Select(usuario => usuario.Id));
        }

        _notificacoes.NotificarVariosUmaVez(
            destinatarios.Where(id => id != ocorrencia.AutorId),
            TipoNotificacao.OcorrenciaRegistrada,
            $"Nova ocorrência #{ocorrencia.Id}",
            $"Ocorrência de {ocorrencia.Tipo} aguardando análise.",
            "Ocorrencia",
            ocorrencia.Id,
            DateTime.UtcNow);
    }

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    private static bool TentarLerTipo(string? texto, out TipoOcorrencia? tipo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            tipo = null;
            return true;
        }

        if (Enum.TryParse<TipoOcorrencia>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            tipo = valor;
            return true;
        }

        tipo = null;
        return false;
    }

    private static bool TentarLerStatus(string? texto, out StatusOcorrencia? status)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<StatusOcorrencia>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            status = valor;
            return true;
        }

        status = null;
        return false;
    }

    private OcorrenciaResumoDTO ParaResumo(Ocorrencia ocorrencia) =>
        new()
        {
            Id = ocorrencia.Id,
            Tipo = ocorrencia.Tipo,
            Status = ocorrencia.Status,
            UnidadeId = ocorrencia.UnidadeId,
            EscolaId = ocorrencia.EscolaId,
            AlunoId = ocorrencia.AlunoId,
            Aluno = ocorrencia.AlunoNome,
            CardapioId = ocorrencia.CardapioId,
            Cardapio = ocorrencia.CardapioNome,
            RefeicaoId = ocorrencia.RefeicaoId,
            Refeicao = ocorrencia.RefeicaoNome,
            AlimentoId = ocorrencia.AlimentoId,
            Alimento = ocorrencia.AlimentoNome,
            Descricao = ocorrencia.Descricao,
            Autor = ocorrencia.AutorNome,
            CriadoEm = ocorrencia.CriadoEmUtc
        };

    private OcorrenciaDTO ParaDetalhe(Ocorrencia ocorrencia, EscopoUsuario escopo)
    {
        var resumo = ParaResumo(ocorrencia);

        return new OcorrenciaDTO
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
            RefeicaoId = resumo.RefeicaoId,
            Refeicao = resumo.Refeicao,
            AlimentoId = resumo.AlimentoId,
            Alimento = resumo.Alimento,
            Descricao = resumo.Descricao,
            Autor = resumo.Autor,
            CriadoEm = resumo.CriadoEm,
            PodeCancelar = ocorrencia.Status == StatusOcorrencia.Aberta &&
                           ocorrencia.AutorId == escopo.UsuarioId,
            Historico = ocorrencia.Historico
                .OrderBy(evento => evento.CriadoEmUtc)
                .ThenBy(evento => evento.Id)
                .Select(evento => new HistoricoOcorrenciaDTO
                {
                    Status = evento.Status,
                    Autor = evento.AutorNome,
                    Observacao = evento.Observacao,
                    CriadoEm = evento.CriadoEmUtc
                })
                .ToList()
        };
    }
}
