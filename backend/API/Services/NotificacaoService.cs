using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Ponto único de notificação (Spec 018, RF8–RF12). A emissão é síncrona e acontece na mesma
/// unidade de trabalho da mudança de origem (RNF2); a leitura e a marcação de leitura são
/// sempre restritas ao destinatário da sessão (RF10/RF11). O conteúdo nunca inclui CPF,
/// e-mail, telefone ou dado clínico além do nome do aluno quando necessário (RF13).
/// </summary>
public sealed class NotificacaoService
{
    private readonly INotificacaoRepository _notificacoes;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;

    public NotificacaoService(
        INotificacaoRepository notificacoes,
        IUnidadeDeTrabalho unidadeDeTrabalho)
    {
        _notificacoes = notificacoes;
        _unidadeDeTrabalho = unidadeDeTrabalho;
    }

    // ---------- Emissão ----------

    public void Notificar(
        int destinatarioId,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string? referenciaTipo,
        int? referenciaId,
        DateTime utc) =>
        _notificacoes.Add(new Notificacao(
            destinatarioId,
            tipo,
            titulo,
            mensagem,
            referenciaTipo,
            referenciaId,
            utc));

    /// <summary>
    /// Emissão em lote para vários destinatários, sem notificar a mesma pessoa duas vezes
    /// (RF9). Usada pelas specs de origem (015, 016, 010) após o chamador resolver os ids.
    /// </summary>
    public void NotificarVarios(
        IEnumerable<int> destinatarios,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string? referenciaTipo,
        int? referenciaId,
        DateTime utc)
    {
        foreach (var destinatario in destinatarios.Distinct())
        {
            Notificar(destinatario, tipo, titulo, mensagem, referenciaTipo, referenciaId, utc);
        }
    }

    /// <summary>
    /// Notifica o solicitante e o decisor sobre a mudança de status de uma solicitação.
    /// Não notifica duas vezes a mesma pessoa.
    /// </summary>
    public void NotificarSolicitacao(
        int solicitanteId,
        int decisorId,
        int solicitacaoId,
        TipoSolicitacao tipoSolicitacao,
        StatusSolicitacao status,
        DateTime utc)
    {
        var titulo = $"Solicitação #{solicitacaoId}: {status}";

        Notificar(
            solicitanteId,
            TipoNotificacao.SolicitacaoStatus,
            titulo,
            $"Seu pedido de {tipoSolicitacao} foi atualizado para {status}.",
            "Solicitacao",
            solicitacaoId,
            utc);

        if (decisorId <= 0 || decisorId == solicitanteId)
        {
            return;
        }

        Notificar(
            decisorId,
            TipoNotificacao.SolicitacaoStatus,
            titulo,
            $"A solicitação #{solicitacaoId} está em {status}.",
            "Solicitacao",
            solicitacaoId,
            utc);
    }

    // ---------- Leitura ----------

    public Resultado<PaginaDTO<NotificacaoDTO>> Listar(
        bool? naoLidas,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);
        var (itens, total) = _notificacoes.Listar(
            escopo.UsuarioId, naoLidas, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<NotificacaoDTO>>.Ok(new PaginaDTO<NotificacaoDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(ParaDTO).ToList()
        });
    }

    public Resultado<NotificacaoDTO> MarcarLida(int id, EscopoUsuario escopo)
    {
        var notificacao = _notificacoes.Obter(id, escopo.UsuarioId);

        if (notificacao is null)
        {
            return Resultado<NotificacaoDTO>.NaoEncontrado("Notificação não encontrada.");
        }

        return _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            notificacao.MarcarComoLida();
            _notificacoes.Update(notificacao);
            return Resultado<NotificacaoDTO>.Ok(ParaDTO(notificacao));
        });
    }

    public Resultado<int> MarcarTodas(EscopoUsuario escopo) =>
        _unidadeDeTrabalho.ExecutarEmTransacao(() =>
        {
            var pendentes = _notificacoes.ContarNaoLidas(escopo.UsuarioId);
            _notificacoes.MarcarTodasComoLidas(escopo.UsuarioId);
            return Resultado<int>.Ok(pendentes);
        });

    private static NotificacaoDTO ParaDTO(Notificacao notificacao) => new()
    {
        Id = notificacao.Id,
        Tipo = notificacao.Tipo,
        Titulo = notificacao.Titulo,
        Mensagem = notificacao.Mensagem,
        ReferenciaTipo = notificacao.ReferenciaTipo,
        ReferenciaId = notificacao.ReferenciaId,
        Lida = notificacao.Lida,
        CriadoEm = notificacao.CriadoEmUtc
    };
}
