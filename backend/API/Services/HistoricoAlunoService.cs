using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

/// <summary>
/// Leitura da trilha de alterações do aluno (Spec 023, RF6 e RF7).
///
/// Um único serviço responde as duas rotas: a trilha completa em
/// <c>/api/aluno/{id}/historico</c> e o recorte de restrições em
/// <c>/api/aluno/{id}/restricoes/historico</c>, que mantém o JSON anterior. O escopo por
/// papel é decidido aqui, com o mesmo critério do restante do módulo de aluno.
/// </summary>
public sealed class HistoricoAlunoService
{
    private static readonly TipoEventoAluno[] TiposDeRestricao =
    {
        TipoEventoAluno.RestricaoAdicionada,
        TipoEventoAluno.RestricaoRemovida
    };

    private readonly IHistoricoAlunoRepository _historico;
    private readonly IAlunoRepository _alunos;

    public HistoricoAlunoService(
        IHistoricoAlunoRepository historico,
        IAlunoRepository alunos)
    {
        _historico = historico;
        _alunos = alunos;
    }

    /// <summary>Trilha completa: criação, campos alterados e restrições.</summary>
    public Resultado<PaginaDTO<HistoricoAlunoDTO>> Listar(
        int alunoId,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);

        if (_alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo)) is null)
        {
            return ForaDoEscopo<PaginaDTO<HistoricoAlunoDTO>>(escopo);
        }

        var (itens, total) = _historico.Listar(alunoId, tipos: null, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<HistoricoAlunoDTO>>.Ok(new PaginaDTO<HistoricoAlunoDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(ParaDTO).ToList()
        });
    }

    /// <summary>
    /// Recorte de restrições da trilha, no formato publicado pela Spec 010 (RF7). A ação
    /// vem do tipo do evento e o nome sai do lado preenchido do snapshot.
    /// </summary>
    public Resultado<PaginaDTO<HistoricoRestricaoDTO>> ListarRestricoes(
        int alunoId,
        int? pagina,
        int? tamanho,
        EscopoUsuario escopo)
    {
        var (paginaEfetiva, tamanhoEfetivo) = Paginacao.Validar(pagina, tamanho);

        if (_alunos.Obter(alunoId, FiltrosDeConsulta.ParaAluno(escopo)) is null)
        {
            return ForaDoEscopo<PaginaDTO<HistoricoRestricaoDTO>>(escopo);
        }

        var (itens, total) = _historico.Listar(
            alunoId, TiposDeRestricao, paginaEfetiva, tamanhoEfetivo);

        return Resultado<PaginaDTO<HistoricoRestricaoDTO>>.Ok(new PaginaDTO<HistoricoRestricaoDTO>
        {
            Pagina = paginaEfetiva,
            Tamanho = tamanhoEfetivo,
            Total = total,
            Itens = itens.Select(ParaRestricaoDTO).ToList()
        });
    }

    /// <summary>
    /// Aluno inexistente é 404 para o escopo de unidade; fora do escopo de escola é 403,
    /// sem revelar existência — mesma regra do restante do módulo de aluno.
    /// </summary>
    private static Resultado<T> ForaDoEscopo<T>(EscopoUsuario escopo)
    {
        return escopo.EscopoDeUnidade
            ? Resultado<T>.NaoEncontrado("Aluno não encontrado.")
            : Resultado<T>.Proibido();
    }

    private static HistoricoAlunoDTO ParaDTO(HistoricoAluno evento)
    {
        return new HistoricoAlunoDTO
        {
            Tipo = evento.Tipo,
            Campo = evento.Campo,
            ValorAnterior = evento.ValorAnterior,
            ValorNovo = evento.ValorNovo,
            Autor = evento.AutorNome,
            CriadoEm = DateTime.SpecifyKind(evento.CriadoEmUtc, DateTimeKind.Utc)
        };
    }

    private static HistoricoRestricaoDTO ParaRestricaoDTO(HistoricoAluno evento)
    {
        var removida = evento.Tipo == TipoEventoAluno.RestricaoRemovida;

        return new HistoricoRestricaoDTO
        {
            Restricao = removida ? evento.ValorAnterior ?? string.Empty : evento.ValorNovo ?? string.Empty,
            Acao = removida ? AcaoRestricao.Removida : AcaoRestricao.Adicionada,
            Autor = evento.AutorNome,
            CriadoEm = DateTime.SpecifyKind(evento.CriadoEmUtc, DateTimeKind.Utc)
        };
    }
}
