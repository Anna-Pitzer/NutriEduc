using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Fila e decisão de acesso (Spec 033, RF6–RF16). A aprovação é exclusiva do ADM da
/// unidade (matriz 028: <see cref="Recurso.SolicitacaoDeAcesso"/>) e grava, numa única
/// unidade de trabalho, papel, escopo, CRN, vínculos, estado, trilha e notificação.
/// </summary>
public sealed class AprovarAcessoService
{
    private readonly ISolicitacaoDeAcessoRepository _solicitacoes;
    private readonly IUsuarioRepository _usuarios;
    private readonly IEscolaRepository _escolas;
    private readonly IAlunoRepository _alunos;
    private readonly IVinculoRepository _vinculos;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly ITrilhaAcaoRepository _trilha;
    private readonly NotificacaoService _notificacoes;

    public AprovarAcessoService(
        ISolicitacaoDeAcessoRepository solicitacoes,
        IUsuarioRepository usuarios,
        IEscolaRepository escolas,
        IAlunoRepository alunos,
        IVinculoRepository vinculos,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        ITrilhaAcaoRepository trilha,
        NotificacaoService notificacoes)
    {
        _solicitacoes = solicitacoes;
        _usuarios = usuarios;
        _escolas = escolas;
        _alunos = alunos;
        _vinculos = vinculos;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _trilha = trilha;
        _notificacoes = notificacoes;
    }

    // ---------- Fila ----------

    public Resultado<PaginaDTO<SolicitacaoAcessoResumoDTO>> Listar(
        string? status,
        string? tipo,
        string? busca,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.AdminSecretaria)
        {
            return Resultado<PaginaDTO<SolicitacaoAcessoResumoDTO>>.Proibido();
        }

        if (escopo.UnidadeId is not { } unidadeId)
        {
            return Resultado<PaginaDTO<SolicitacaoAcessoResumoDTO>>.Proibido();
        }

        if (!TentarLerStatus(status, out var statusFiltro))
        {
            return Resultado<PaginaDTO<SolicitacaoAcessoResumoDTO>>.Invalido(
                $"Status inválido: '{status}'.");
        }

        if (!TentarLerTipo(tipo, out var tipoFiltro))
        {
            return Resultado<PaginaDTO<SolicitacaoAcessoResumoDTO>>.Invalido(
                $"Tipo inválido: '{tipo}'.");
        }

        var filtro = new FiltroSolicitacaoAcesso(statusFiltro, tipoFiltro, busca, unidadeId);
        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _solicitacoes.Listar(filtro, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<SolicitacaoAcessoResumoDTO>>.Ok(
            new PaginaDTO<SolicitacaoAcessoResumoDTO>
            {
                Pagina = paginaEfetiva,
                Tamanho = tamanhoEfetivo,
                Total = total,
                Itens = itens.Select(ParaResumo).ToList()
            });
    }

    public Resultado<SolicitacaoAcessoDTO> Obter(int id, EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.Obter(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (NegacaoDaUnidade(solicitacao, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Solicitação não encontrada.")
                : Resultado<SolicitacaoAcessoDTO>.Proibido();
        }

        return Resultado<SolicitacaoAcessoDTO>.Ok(ParaDetalhe(solicitacao));
    }

    // ---------- Decisão ----------

    public Resultado<SolicitacaoAcessoDTO> Aprovar(
        int id,
        AprovarAcessoDTO dto,
        EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.Obter(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (NegacaoDaUnidade(solicitacao, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Solicitação não encontrada.")
                : Resultado<SolicitacaoAcessoDTO>.Proibido();
        }

        // RF12: decisão repetida devolve 409; a concorrência real é tratada no banco.
        if (solicitacao.Status != StatusSolicitacaoAcesso.Pendente)
        {
            return Resultado<SolicitacaoAcessoDTO>.Conflito("A solicitação já foi decidida.");
        }

        // Política pura: forma do plano (papel, escola, CRN, vínculo).
        var politica = PoliticaDeAprovacao.Planejar(
            solicitacao.TipoSolicitado,
            dto.PapelConcedido,
            dto.EscolaId,
            solicitacao.CrnSolicitado,
            dto.AlunosIds);

        if (politica.Plano is null)
        {
            // RF14: papel administrativo é 403; demais erros de forma são 400.
            return politica.Erros.Contains(PoliticaDeAprovacao.MensagemPapelAdministrativo)
                ? Resultado<SolicitacaoAcessoDTO>.Proibido()
                : Resultado<SolicitacaoAcessoDTO>.Invalido(politica.Erros[0]);
        }

        var plano = politica.Plano;

        // RF13: papel corrigido exige justificativa 10–500.
        var corrigido = plano.Papel != PoliticaDeAprovacao.PapelPadrao(solicitacao.TipoSolicitado);

        if (corrigido)
        {
            var justificativa = (dto.JustificativaCorrecaoPapel ?? string.Empty).Trim();

            if (justificativa.Length is < SolicitacaoDeAcesso.JustificativaMin
                or > SolicitacaoDeAcesso.JustificativaMax)
            {
                return Resultado<SolicitacaoAcessoDTO>.Invalido(
                    "A correção de papel exige justificativa entre 10 e 500 caracteres.");
            }
        }

        var usuario = _usuarios.GetById(solicitacao.UsuarioId);

        if (usuario is null)
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido(
                "Usuário da solicitação não encontrado.");
        }

        // CRN único (Spec 031): só vira Usuario.Crn na aprovação.
        if (plano.Papel == PapelUsuario.Nutricionista &&
            _usuarios.GetByCrn(plano.Crn!) is { } porCrn &&
            porCrn.Id != usuario.Id)
        {
            return Resultado<SolicitacaoAcessoDTO>.Conflito(
                "Este CRN já está vinculado a outro usuário.");
        }

        // Validações institucionais por tipo (existência, atividade e unidade).
        Escola? escola = null;

        if (plano.Papel == PapelUsuario.Merendeira)
        {
            escola = _escolas.GetEscola(plano.EscolaId!.Value);

            if (escola is null)
            {
                return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Escola não encontrada.");
            }

            if (!escola.Ativa)
            {
                return Resultado<SolicitacaoAcessoDTO>.Invalido(
                    "Escola desativada não pode receber merendeira.");
            }

            if (escola.UnidadeId != solicitacao.UnidadeId)
            {
                return Resultado<SolicitacaoAcessoDTO>.Invalido(
                    "Escola fora da unidade da solicitação.");
            }
        }

        var alunos = new List<Aluno>();

        if (plano.ExigeAlunos)
        {
            var ids = (dto.AlunosIds ?? new List<int>()).Distinct().ToList();

            foreach (var alunoId in ids)
            {
                var aluno = _alunos.ObterPorId(alunoId);

                if (aluno is null)
                {
                    return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Aluno não encontrado.");
                }

                if (aluno.UnidadeId != solicitacao.UnidadeId)
                {
                    return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado(
                        "Aluno fora da unidade da solicitação.");
                }

                alunos.Add(aluno);
            }
        }

        // Aplicação atômica (RF7/RNF2).
        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            if (plano.Papel == PapelUsuario.Nutricionista)
            {
                usuario.DefinirCrn(plano.Crn);
                usuario.DefinirPapel(PapelUsuario.Nutricionista, null, solicitacao.UnidadeId);
            }
            else if (plano.Papel == PapelUsuario.Merendeira)
            {
                usuario.DefinirPapel(PapelUsuario.Merendeira, escola!.Id, escola.UnidadeId);
            }
            else
            {
                usuario.DefinirPapel(PapelUsuario.Responsavel, null, solicitacao.UnidadeId);
            }

            _usuarios.Update(usuario);

            foreach (var aluno in alunos)
            {
                if (!_vinculos.Existe(aluno.Id, usuario.Id))
                {
                    _vinculos.Add(new VinculoResponsavelAluno(aluno.Id, usuario.Id));
                }
            }

            solicitacao.Aprovar(
                plano.Papel,
                plano.EscolaId,
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                dto.JustificativaCorrecaoPapel,
                DateTime.UtcNow);

            _solicitacoes.Update(solicitacao);

            _trilha.Add(new TrilhaAcao(
                "AcessoAprovado",
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                "Usuario",
                usuario.Id,
                solicitacao.TipoSolicitado.ToString(),
                plano.Papel.ToString(),
                DateTime.UtcNow));

            _notificacoes.Notificar(
                usuario.Id,
                TipoNotificacao.SolicitacaoStatus,
                $"Solicitação de acesso #{solicitacao.Id}: Aprovada",
                "Seu cadastro foi aprovado.",
                "SolicitacaoAcesso",
                solicitacao.Id,
                DateTime.UtcNow);
        });

        return Obter(id, escopo);
    }

    public Resultado<SolicitacaoAcessoDTO> Recusar(
        int id,
        RecusarAcessoDTO dto,
        EscopoUsuario escopo)
    {
        var solicitacao = _solicitacoes.Obter(id);

        if (solicitacao is null)
        {
            return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Solicitação não encontrada.");
        }

        if (NegacaoDaUnidade(solicitacao, escopo) is { } negacao)
        {
            return negacao == ResultadoStatus.NaoEncontrado
                ? Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Solicitação não encontrada.")
                : Resultado<SolicitacaoAcessoDTO>.Proibido();
        }

        if (solicitacao.Status != StatusSolicitacaoAcesso.Pendente)
        {
            return Resultado<SolicitacaoAcessoDTO>.Conflito("A solicitação já foi decidida.");
        }

        var motivo = (dto.Motivo ?? string.Empty).Trim();

        if (motivo.Length is < SolicitacaoDeAcesso.MotivoMin or > SolicitacaoDeAcesso.MotivoMax)
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido(
                $"O motivo deve ter entre {SolicitacaoDeAcesso.MotivoMin} e " +
                $"{SolicitacaoDeAcesso.MotivoMax} caracteres.");
        }

        var usuario = _usuarios.GetById(solicitacao.UsuarioId);

        if (usuario is null)
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido(
                "Usuário da solicitação não encontrado.");
        }

        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            solicitacao.Recusar(motivo, escopo.UsuarioId, NomeDoAutor(escopo), DateTime.UtcNow);
            _solicitacoes.Update(solicitacao);

            _trilha.Add(new TrilhaAcao(
                "AcessoRecusado",
                escopo.UsuarioId,
                NomeDoAutor(escopo),
                "Usuario",
                usuario.Id,
                solicitacao.TipoSolicitado.ToString(),
                motivo,
                DateTime.UtcNow));

            _notificacoes.Notificar(
                usuario.Id,
                TipoNotificacao.SolicitacaoStatus,
                $"Solicitação de acesso #{solicitacao.Id}: Recusada",
                "Seu cadastro foi recusado.",
                "SolicitacaoAcesso",
                solicitacao.Id,
                DateTime.UtcNow);
        });

        return Obter(id, escopo);
    }

    // ---------- Reconsideração (RF10/D8) ----------

    public Resultado<SolicitacaoAcessoDTO> Criar(
        CriarSolicitacaoAcessoDTO dto,
        EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.AdminSecretaria)
        {
            return Resultado<SolicitacaoAcessoDTO>.Proibido();
        }

        if (dto.UsuarioId is not { } usuarioId || usuarioId <= 0)
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido("Informe o usuário.");
        }

        if (dto.Tipo is not { } tipo || !Enum.IsDefined(tipo))
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido(
                "Informe um tipo de cadastro válido: Nutricionista, Merendeira ou Responsavel.");
        }

        if (dto.UnidadeId is not { } unidadeId || unidadeId <= 0)
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido("Informe a unidade.");
        }

        if (escopo.UnidadeId != unidadeId)
        {
            return Resultado<SolicitacaoAcessoDTO>.Proibido();
        }

        var usuario = _usuarios.GetById(usuarioId);

        if (usuario is null)
        {
            return Resultado<SolicitacaoAcessoDTO>.NaoEncontrado("Usuário não encontrado.");
        }

        // RF11: no máximo uma solicitação pendente por usuário.
        if (_solicitacoes.ObterPendenteDoUsuario(usuarioId) is not null)
        {
            return Resultado<SolicitacaoAcessoDTO>.Conflito(
                "Já existe uma solicitação pendente para este usuário.");
        }

        var erroDeCrn = PoliticaDeAprovacao.ErroDeCrn(tipo, dto.Crn);

        if (erroDeCrn is not null)
        {
            return Resultado<SolicitacaoAcessoDTO>.Invalido(erroDeCrn);
        }

        var solicitacao = new SolicitacaoDeAcesso(usuarioId, tipo, unidadeId, dto.Crn, DateTime.UtcNow);

        _unidadeDeTrabalho.ExecutarEmTransacao(() => _solicitacoes.Add(solicitacao));

        return Obter(solicitacao.Id, escopo);
    }

    // ---------- Diagnóstico de legado (RF15/T13) ----------

    public Resultado<List<DiagnosticoSemSolicitacaoDTO>> DiagnosticarSemSolicitacao(EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.AdminSecretaria)
        {
            return Resultado<List<DiagnosticoSemSolicitacaoDTO>>.Proibido();
        }

        // Contas sem papel (legado pré-033) que nunca tiveram solicitação. Conta sem papel
        // não possui unidade, então a listagem é global; o ADM escolhe a unidade ao criar a
        // solicitação manualmente.
        var legadas = _usuarios
            .ListarSemPapel()
            .Where(usuario => !_solicitacoes.ExistePorUsuario(usuario.Id))
            .Select(usuario => new DiagnosticoSemSolicitacaoDTO(
                usuario.Id, usuario.Nome ?? string.Empty, usuario.Email ?? string.Empty))
            .ToList();

        return Resultado<List<DiagnosticoSemSolicitacaoDTO>>.Ok(legadas);
    }

    // ---------- Apoio ----------

    private static ResultadoStatus? NegacaoDaUnidade(
        SolicitacaoDeAcesso solicitacao,
        EscopoUsuario escopo)
    {
        if (escopo.Papel != PapelUsuario.AdminSecretaria)
        {
            return ResultadoStatus.Proibido;
        }

        return solicitacao.UnidadeId == escopo.UnidadeId
            ? null
            : ResultadoStatus.NaoEncontrado;
    }

    private string NomeDoAutor(EscopoUsuario escopo) =>
        _usuarios.GetById(escopo.UsuarioId)?.Nome ?? "sistema";

    private SolicitacaoAcessoResumoDTO ParaResumo(SolicitacaoDeAcesso solicitacao)
    {
        var usuario = _usuarios.GetById(solicitacao.UsuarioId);

        return new SolicitacaoAcessoResumoDTO
        {
            Id = solicitacao.Id,
            TipoSolicitado = solicitacao.TipoSolicitado,
            Status = solicitacao.Status,
            UnidadeId = solicitacao.UnidadeId,
            UsuarioId = solicitacao.UsuarioId,
            Nome = usuario?.Nome ?? string.Empty,
            Email = usuario?.Email ?? string.Empty,
            Cpf = usuario?.Cpf ?? string.Empty,
            SolicitadoEm = solicitacao.SolicitadoEmUtc
        };
    }

    private SolicitacaoAcessoDTO ParaDetalhe(SolicitacaoDeAcesso solicitacao)
    {
        var resumo = ParaResumo(solicitacao);

        return new SolicitacaoAcessoDTO
        {
            Id = resumo.Id,
            TipoSolicitado = resumo.TipoSolicitado,
            Status = resumo.Status,
            UnidadeId = resumo.UnidadeId,
            UsuarioId = resumo.UsuarioId,
            Nome = resumo.Nome,
            Email = resumo.Email,
            Cpf = resumo.Cpf,
            SolicitadoEm = resumo.SolicitadoEm,
            CrnSolicitado = solicitacao.CrnSolicitado,
            PapelConcedido = solicitacao.PapelConcedido,
            EscolaConcedidaId = solicitacao.EscolaConcedidaId,
            Motivo = solicitacao.Motivo,
            JustificativaCorrecaoPapel = solicitacao.JustificativaCorrecaoPapel,
            DecididoEm = solicitacao.DecididoEmUtc,
            DecididoPorNome = solicitacao.DecididoPorNome
        };
    }

    private static bool TentarLerStatus(string? texto, out StatusSolicitacaoAcesso? status)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<StatusSolicitacaoAcesso>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            status = valor;
            return true;
        }

        status = null;
        return false;
    }

    private static bool TentarLerTipo(string? texto, out TipoCadastroSolicitado? tipo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            tipo = null;
            return true;
        }

        if (Enum.TryParse<TipoCadastroSolicitado>(texto.Trim(), ignoreCase: true, out var valor) &&
            Enum.IsDefined(valor))
        {
            tipo = valor;
            return true;
        }

        tipo = null;
        return false;
    }
}
