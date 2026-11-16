using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso do feedback (Spec 018). Concentra a autorização de criação por papel, a
/// derivação de <see cref="Feedback.EscolaId"/>/<see cref="Feedback.UnidadeId"/>, o escopo de
/// listagem e a separação de quem responde/encerra. O agregado permanece dono das transições.
/// </summary>
public sealed class FeedbackService
{
    private readonly IFeedbackRepository _feedbacks;
    private readonly IUsuarioRepository _usuarios;
    private readonly IAlunoRepository _alunos;
    private readonly IVinculoRepository _vinculos;
    private readonly NotificacaoService _notificacoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly TimeProvider _relogio;

    public FeedbackService(
        IFeedbackRepository feedbacks,
        IUsuarioRepository usuarios,
        IAlunoRepository alunos,
        IVinculoRepository vinculos,
        NotificacaoService notificacoes,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        TimeProvider relogio)
    {
        _feedbacks = feedbacks;
        _usuarios = usuarios;
        _alunos = alunos;
        _vinculos = vinculos;
        _notificacoes = notificacoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
    }

    // ---------- Criação ----------

    public Resultado<FeedbackDTO> Criar(CriarFeedbackDTO dto, EscopoUsuario escopo)
    {
        if (!PodeCriar(escopo.Papel))
        {
            return Resultado<FeedbackDTO>.Proibido();
        }

        if (escopo.UnidadeId is not { } unidadeId)
        {
            return Resultado<FeedbackDTO>.Proibido();
        }

        var escolaId = escopo.EscolaId;
        int? alunoId = null;
        string? alunoNome = null;

        if (dto.AlunoId is { } idAluno && idAluno > 0)
        {
            var resolucao = ResolverAlunoParaFeedback(idAluno, escopo);

            if (resolucao.Status != ResultadoStatus.Ok)
            {
                return Rejeitar<FeedbackDTO>(resolucao);
            }

            alunoId = idAluno;
            alunoNome = resolucao.Valor!.Nome;
            escolaId = resolucao.Valor!.EscolaId;
        }

        Feedback feedback;

        try
        {
            feedback = new Feedback(
                dto.Assunto,
                dto.Mensagem,
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                escopo.Papel,
                unidadeId,
                escolaId,
                alunoId,
                alunoNome,
                _relogio.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<FeedbackDTO>.Invalido(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _feedbacks.Add(feedback));

        return Carregar(feedback.Id, escopo);
    }

    // ---------- Leitura ----------

    public Resultado<PaginaDTO<FeedbackResumoDTO>> Listar(
        string? status,
        int? alunoId,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (!TentarLerStatus(status, out var statusFiltro))
        {
            return Resultado<PaginaDTO<FeedbackResumoDTO>>.Invalido(
                $"Status inválido: '{status}'.");
        }

        var filtro = new FiltroFeedback(
            Status: statusFiltro,
            AlunoId: alunoId is > 0 ? alunoId : null);

        switch (escopo.Papel)
        {
            case PapelUsuario.AdminSecretaria:
            case PapelUsuario.Nutricionista:
                if (escopo.UnidadeId is not { } unidadeDoEscopo)
                {
                    return Resultado<PaginaDTO<FeedbackResumoDTO>>.Proibido();
                }

                filtro = filtro with { UnidadeId = unidadeDoEscopo };
                break;

            case PapelUsuario.AdminEscola:
            case PapelUsuario.Merendeira:
                if (escopo.UnidadeId is not { } unidadeDaEscola ||
                    escopo.EscolaId is not { } escolaDoEscopo)
                {
                    return Resultado<PaginaDTO<FeedbackResumoDTO>>.Proibido();
                }

                filtro = filtro with { UnidadeId = unidadeDaEscola, EscolaId = escolaDoEscopo };
                break;

            case PapelUsuario.Responsavel:
                filtro = filtro with { AutorId = escopo.UsuarioId };
                break;

            default:
                return Resultado<PaginaDTO<FeedbackResumoDTO>>.Proibido();
        }

        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _feedbacks.Listar(filtro, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<FeedbackResumoDTO>>.Ok(new PaginaDTO<FeedbackResumoDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(feedback => ParaResumo(feedback, escopo)).ToList()
        });
    }

    // ---------- Resposta e encerramento ----------

    public Resultado<FeedbackDTO> Responder(int id, ResponderFeedbackDTO dto, EscopoUsuario escopo)
    {
        var feedback = _feedbacks.Obter(id);

        if (feedback is null)
        {
            return Resultado<FeedbackDTO>.NaoEncontrado("Feedback não encontrado.");
        }

        if (NegacaoDeVisao(feedback, escopo) is { } negacao)
        {
            return Negar<FeedbackDTO>(negacao, "Feedback não encontrado.");
        }

        if (!PodeResponder(escopo.Papel))
        {
            return Resultado<FeedbackDTO>.Proibido();
        }

        var utc = _relogio.GetUtcNow().UtcDateTime;

        try
        {
            feedback.Responder(dto.Resposta, NomeDoAutor(escopo), utc);
        }
        catch (ArgumentException excecao)
        {
            return Resultado<FeedbackDTO>.Invalido(excecao.Message);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<FeedbackDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            _feedbacks.Update(feedback);
            _notificacoes.Notificar(
                feedback.AutorId,
                TipoNotificacao.FeedbackRespondido,
                "Feedback respondido",
                $"Seu feedback #{feedback.Id} recebeu uma resposta.",
                "feedback",
                feedback.Id,
                utc);
        });

        return Carregar(id, escopo);
    }

    public Resultado<FeedbackDTO> Encerrar(int id, EscopoUsuario escopo)
    {
        var feedback = _feedbacks.Obter(id);

        if (feedback is null)
        {
            return Resultado<FeedbackDTO>.NaoEncontrado("Feedback não encontrado.");
        }

        if (NegacaoDeVisao(feedback, escopo) is { } negacao)
        {
            return Negar<FeedbackDTO>(negacao, "Feedback não encontrado.");
        }

        if (feedback.AutorId != escopo.UsuarioId)
        {
            return Resultado<FeedbackDTO>.Proibido();
        }

        try
        {
            feedback.Encerrar(NomeDoAutor(escopo), _relogio.GetUtcNow().UtcDateTime);
        }
        catch (TransicaoInvalidaException excecao)
        {
            return Resultado<FeedbackDTO>.Conflito(excecao.Message);
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _feedbacks.Update(feedback));

        return Carregar(id, escopo);
    }

    // ---------- Apoio ----------

    private Resultado<FeedbackDTO> Carregar(int id, EscopoUsuario escopo)
    {
        var feedback = _feedbacks.Obter(id);

        if (feedback is null)
        {
            return Resultado<FeedbackDTO>.NaoEncontrado("Feedback não encontrado.");
        }

        if (NegacaoDeVisao(feedback, escopo) is { } negacao)
        {
            return Negar<FeedbackDTO>(negacao, "Feedback não encontrado.");
        }

        return Resultado<FeedbackDTO>.Ok(ParaDetalhe(feedback, escopo));
    }

    private Resultado<Aluno> ResolverAlunoParaFeedback(int alunoId, EscopoUsuario escopo)
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

    private static Resultado<T> Negar<T>(ResultadoStatus negacao, string mensagem) =>
        negacao == ResultadoStatus.NaoEncontrado
            ? Resultado<T>.NaoEncontrado(mensagem)
            : Resultado<T>.Proibido();

    private static bool PodeCriar(PapelUsuario papel) =>
        papel is PapelUsuario.Merendeira or PapelUsuario.Responsavel or PapelUsuario.AdminEscola;

    private static bool PodeResponder(PapelUsuario papel) =>
        papel is PapelUsuario.Nutricionista or PapelUsuario.AdminEscola;

    private ResultadoStatus? NegacaoDeVisao(Feedback feedback, EscopoUsuario escopo) =>
        escopo.Papel switch
        {
            PapelUsuario.AdminSecretaria =>
                feedback.UnidadeId == escopo.UnidadeId ? null : ResultadoStatus.NaoEncontrado,

            PapelUsuario.Nutricionista =>
                feedback.UnidadeId != escopo.UnidadeId ? ResultadoStatus.NaoEncontrado : null,

            PapelUsuario.AdminEscola => NegacaoPorEscola(feedback, escopo),

            PapelUsuario.Merendeira => NegacaoPorEscola(feedback, escopo),

            PapelUsuario.Responsavel =>
                feedback.AutorId == escopo.UsuarioId ? null : ResultadoStatus.Proibido,

            _ => ResultadoStatus.Proibido
        };

    private static ResultadoStatus? NegacaoPorEscola(Feedback feedback, EscopoUsuario escopo)
    {
        if (feedback.UnidadeId != escopo.UnidadeId)
        {
            return ResultadoStatus.NaoEncontrado;
        }

        return feedback.EscolaId == escopo.EscolaId ? null : ResultadoStatus.Proibido;
    }

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    private static bool TentarLerStatus(string? texto, out StatusFeedback? status)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<StatusFeedback>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            status = valor;
            return true;
        }

        status = null;
        return false;
    }

    private static FeedbackResumoDTO ParaResumo(Feedback feedback, EscopoUsuario escopo) => new()
    {
        Id = feedback.Id,
        Assunto = feedback.Assunto,
        Mensagem = feedback.Mensagem,
        Autor = feedback.AutorNome,
        PapelAutor = feedback.PapelAutor,
        UnidadeId = feedback.UnidadeId,
        EscolaId = feedback.EscolaId,
        AlunoId = feedback.AlunoId,
        Aluno = feedback.AlunoNome,
        Status = feedback.Status,
        CriadoEm = feedback.CriadoEmUtc,
        Resposta = feedback.Resposta,
        RespondidoPor = feedback.RespondidoPor,
        PodeResponder = PodeResponder(escopo.Papel) && feedback.Status == StatusFeedback.Aberto,
        PodeEncerrar = feedback.AutorId == escopo.UsuarioId &&
                       feedback.Status == StatusFeedback.Respondido
    };

    private FeedbackDTO ParaDetalhe(Feedback feedback, EscopoUsuario escopo) => new()
    {
        Id = feedback.Id,
        Assunto = feedback.Assunto,
        Mensagem = feedback.Mensagem,
        Autor = feedback.AutorNome,
        PapelAutor = feedback.PapelAutor,
        UnidadeId = feedback.UnidadeId,
        EscolaId = feedback.EscolaId,
        AlunoId = feedback.AlunoId,
        Aluno = feedback.AlunoNome,
        Status = feedback.Status,
        CriadoEm = feedback.CriadoEmUtc,
        Resposta = feedback.Resposta,
        RespondidoPor = feedback.RespondidoPor,
        AtualizadoEm = feedback.AtualizadoEmUtc,
        PodeResponder = PodeResponder(escopo.Papel) && feedback.Status == StatusFeedback.Aberto,
        PodeEncerrar = feedback.AutorId == escopo.UsuarioId &&
                       feedback.Status == StatusFeedback.Respondido
    };
}
