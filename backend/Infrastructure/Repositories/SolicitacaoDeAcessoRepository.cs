using Microsoft.EntityFrameworkCore;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Ntc.Database;

/// <summary>
/// Persistência da solicitação de acesso (Spec 033). O filtro de unidade é aplicado no
/// SQL (RNF3); a busca por nome, e-mail ou CPF também desce para o banco.
/// </summary>
public class SolicitacaoDeAcessoRepository : ISolicitacaoDeAcessoRepository
{
    private readonly ConnectionContext _context;

    public SolicitacaoDeAcessoRepository(ConnectionContext context)
    {
        _context = context;
    }

    public SolicitacaoDeAcesso Add(SolicitacaoDeAcesso solicitacao)
    {
        _context.SolicitacoesDeAcesso.Add(solicitacao);
        return solicitacao;
    }

    public void Update(SolicitacaoDeAcesso solicitacao) =>
        _context.SolicitacoesDeAcesso.Update(solicitacao);

    public SolicitacaoDeAcesso? Obter(int id) =>
        _context.SolicitacoesDeAcesso.FirstOrDefault(solicitacao => solicitacao.Id == id);

    public SolicitacaoDeAcesso? ObterPendenteDoUsuario(int usuarioId) =>
        _context.SolicitacoesDeAcesso.FirstOrDefault(solicitacao =>
            solicitacao.UsuarioId == usuarioId &&
            solicitacao.Status == StatusSolicitacaoAcesso.Pendente);

    public bool ExistePorUsuario(int usuarioId) =>
        _context.SolicitacoesDeAcesso.Any(solicitacao => solicitacao.UsuarioId == usuarioId);

    public (List<SolicitacaoDeAcesso> Itens, int Total) Listar(
        FiltroSolicitacaoAcesso filtro,
        int pagina,
        int tamanho)
    {
        var consulta = _context.SolicitacoesDeAcesso.AsQueryable();

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(solicitacao => solicitacao.UnidadeId == unidadeId);
        }

        if (filtro.Status is { } status)
        {
            consulta = consulta.Where(solicitacao => solicitacao.Status == status);
        }

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(solicitacao => solicitacao.TipoSolicitado == tipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim().ToLowerInvariant();

            consulta = consulta.Where(solicitacao =>
                _context.Usuarios.Any(usuario =>
                    usuario.Id == solicitacao.UsuarioId &&
                    ((usuario.Nome ?? string.Empty).ToLower().Contains(termo) ||
                     (usuario.Email ?? string.Empty).ToLower().Contains(termo) ||
                     (usuario.Cpf ?? string.Empty).Contains(termo))));
        }

        var total = consulta.Count();

        var itens = consulta
            .OrderByDescending(solicitacao => solicitacao.SolicitadoEmUtc)
            .ThenByDescending(solicitacao => solicitacao.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList();

        return (itens, total);
    }
}
