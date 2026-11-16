using Microsoft.EntityFrameworkCore;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Persistência da solicitação (Spec 017). Compartilha o <see cref="ConnectionContext"/>
/// Scoped; a trilha de status é navegação do agregado e sobe/desce junto com ele.
/// </summary>
public class SolicitacaoRepository : ISolicitacaoRepository
{
    private readonly ConnectionContext _context;

    public SolicitacaoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public Solicitacao Add(Solicitacao solicitacao)
    {
        _context.Solicitacoes.Add(solicitacao);
        return solicitacao;
    }

    public void Update(Solicitacao solicitacao) =>
        _context.Solicitacoes.Update(solicitacao);

    public Solicitacao? ObterComHistorico(int id) =>
        _context.Solicitacoes
            .Include(solicitacao => solicitacao.Historico)
            .Include(solicitacao => solicitacao.Alteracoes)
            .FirstOrDefault(solicitacao => solicitacao.Id == id);

    public (List<Solicitacao> Itens, int Total) Listar(
        FiltroSolicitacao filtro,
        int pagina,
        int tamanho)
    {
        var consulta = _context.Solicitacoes.AsQueryable();

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(solicitacao => solicitacao.Tipo == tipo);
        }

        if (filtro.Status is { } status)
        {
            consulta = consulta.Where(solicitacao => solicitacao.Status == status);
        }

        if (filtro.TiposExcluidos is { Count: > 0 } excluidos)
        {
            consulta = consulta.Where(solicitacao => !excluidos.Contains(solicitacao.Tipo));
        }

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.UnidadeId == unidadeId);
        }

        if (filtro.EscolaId is { } escolaId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.EscolaId == escolaId);
        }

        if (filtro.AlunoId is { } alunoId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.AlunoId == alunoId);
        }

        if (filtro.CardapioId is { } cardapioId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.CardapioId == cardapioId);
        }

        if (filtro.De is { } de)
        {
            consulta = consulta.Where(solicitacao => solicitacao.CriadoEmUtc >= de);
        }

        if (filtro.Ate is { } ate)
        {
            consulta = consulta.Where(solicitacao => solicitacao.CriadoEmUtc <= ate);
        }

        if (filtro.SolicitanteId is { } solicitanteId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.SolicitanteId == solicitanteId);
        }

        if (filtro.ResponsavelUsuarioId is { } responsavelId)
        {
            // RF10: o responsável vê os próprios pedidos e os dos alunos vinculados.
            consulta = consulta.Where(solicitacao =>
                solicitacao.SolicitanteId == responsavelId ||
                (solicitacao.AlunoId != null &&
                 _context.Vinculos.Any(vinculo =>
                     vinculo.AlunoId == solicitacao.AlunoId &&
                     vinculo.ResponsavelUsuarioId == responsavelId)));
        }

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(solicitacao => solicitacao.CriadoEmUtc)
            .ThenByDescending(solicitacao => solicitacao.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }

    public int? IdDaAbertaParaCampo(int alunoId, CampoDeDadoDoAluno campo) =>
        _context.Solicitacoes
            .Where(solicitacao =>
                solicitacao.Tipo == TipoSolicitacao.DadosCadastrais &&
                solicitacao.AlunoId == alunoId &&
                (solicitacao.Status == StatusSolicitacao.Aberta ||
                 solicitacao.Status == StatusSolicitacao.EmAnalise) &&
                solicitacao.Alteracoes.Any(alteracao => alteracao.Campo == campo))
            .Select(solicitacao => (int?)solicitacao.Id)
            .FirstOrDefault();

    public int? IdDaAbertaParaRestricao(int alunoId, string restricaoNome)
    {
        var alvo = (restricaoNome ?? string.Empty).Trim();

        // SQLite compara texto sem remover acento (PEND-032): o recorte por aluno/tipo/status
        // roda no SQL e a comparação do nome é feita em memória (poucas solicitações abertas).
        return _context.Solicitacoes
            .Where(solicitacao =>
                (solicitacao.Tipo == TipoSolicitacao.InclusaoAlergia ||
                 solicitacao.Tipo == TipoSolicitacao.AlteracaoRestricao) &&
                solicitacao.AlunoId == alunoId &&
                (solicitacao.Status == StatusSolicitacao.Aberta ||
                 solicitacao.Status == StatusSolicitacao.EmAnalise))
            .AsEnumerable()
            .Where(solicitacao => string.Equals(
                (solicitacao.RestricaoNome ?? string.Empty).Trim(),
                alvo,
                StringComparison.OrdinalIgnoreCase))
            .Select(solicitacao => (int?)solicitacao.Id)
            .FirstOrDefault();
    }
}
