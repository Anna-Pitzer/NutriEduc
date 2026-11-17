using Microsoft.EntityFrameworkCore;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Painel;

namespace Ntc.Database;

/// <summary>
/// Leitura agregada do painel (Spec 025, RNF1/RNF2). Todas as contagens da secretaria são
/// <c>COUNT</c>/<c>GROUP BY</c> no SQL; nenhuma tabela é carregada para contar em memória.
/// O escopo chega resolvido em <c>unidadeId</c> ou <c>alunoId</c> — este porto não decide
/// permissão.
/// </summary>
public sealed class PainelRepository : IPainelRepository
{
    private readonly ConnectionContext _context;

    public PainelRepository(ConnectionContext context) => _context = context;

    public IndicadoresDaUnidade ContarIndicadores(int unidadeId, DateTime publicadoDesdeUtc)
    {
        var alunos = _context.Alunos.AsNoTracking()
            .Count(aluno => aluno.UnidadeId == unidadeId);

        var porEscola = _context.Alunos.AsNoTracking()
            .Where(aluno => aluno.UnidadeId == unidadeId)
            .GroupBy(aluno => aluno.EscolaId)
            .Select(grupo => new { EscolaId = grupo.Key, Total = grupo.Count() })
            .ToList();

        var nomesDeEscola = _context.Escolas.AsNoTracking()
            .Where(escola => escola.UnidadeId == unidadeId)
            .ToDictionary(escola => escola.Id, escola => escola.Nome);

        var alunosPorEscola = porEscola
            .Select(item => new ContagemPorEscola(
                item.EscolaId,
                item.EscolaId is { } escolaId &&
                nomesDeEscola.TryGetValue(escolaId, out var nome)
                    ? nome
                    : "Sem escola",
                item.Total))
            .OrderBy(item => item.Escola, StringComparer.Ordinal)
            .ToList();

        var usuarios = _context.Usuarios.AsNoTracking()
            .Where(usuario =>
                usuario.Ativo &&
                usuario.UnidadeId == unidadeId &&
                usuario.Papel != null)
            .GroupBy(usuario => usuario.Papel)
            .Select(grupo => new { Papel = grupo.Key, Total = grupo.Count() })
            .ToList()
            .Where(item => item.Papel is not null)
            .OrderBy(item => item.Papel)
            .Select(item => new ContagemPorPapel(item.Papel!.Value, item.Total))
            .ToList();

        var cardapios = _context.Cardapios.AsNoTracking()
            .Count(cardapio =>
                cardapio.UnidadeId == unidadeId &&
                cardapio.Situacao == SituacaoCardapio.Publicado &&
                cardapio.PublicadoEmUtc != null &&
                cardapio.PublicadoEmUtc >= publicadoDesdeUtc);

        var ocorrencias = _context.Ocorrencias.AsNoTracking()
            .Count(ocorrencia =>
                ocorrencia.UnidadeId == unidadeId &&
                (ocorrencia.Status == StatusOcorrencia.Aberta ||
                 ocorrencia.Status == StatusOcorrencia.EmAnalise));

        var solicitacoes = _context.Solicitacoes.AsNoTracking()
            .Count(solicitacao =>
                solicitacao.UnidadeId == unidadeId &&
                (solicitacao.Status == StatusSolicitacao.Aberta ||
                 solicitacao.Status == StatusSolicitacao.EmAnalise));

        return new IndicadoresDaUnidade(
            alunos,
            alunosPorEscola,
            usuarios,
            cardapios,
            ocorrencias,
            solicitacoes);
    }

    public List<HistoricoAluno> AlteracoesDeRestricao(
        int alunoId,
        DateTime inicioUtc,
        DateTime fimUtc) =>
        _context.HistoricosAlunos.AsNoTracking()
            .Where(evento =>
                evento.AlunoId == alunoId &&
                (evento.Tipo == TipoEventoAluno.RestricaoAdicionada ||
                 evento.Tipo == TipoEventoAluno.RestricaoRemovida) &&
                evento.CriadoEmUtc >= inicioUtc &&
                evento.CriadoEmUtc < fimUtc)
            .OrderByDescending(evento => evento.CriadoEmUtc)
            .ThenByDescending(evento => evento.Id)
            .ToList();

    public List<Ocorrencia> OcorrenciasDoAluno(
        int alunoId,
        DateTime inicioUtc,
        DateTime fimUtc) =>
        _context.Ocorrencias.AsNoTracking()
            .Where(ocorrencia =>
                ocorrencia.AlunoId == alunoId &&
                ocorrencia.CriadoEmUtc >= inicioUtc &&
                ocorrencia.CriadoEmUtc < fimUtc)
            .OrderByDescending(ocorrencia => ocorrencia.CriadoEmUtc)
            .ThenByDescending(ocorrencia => ocorrencia.Id)
            .ToList();
}
