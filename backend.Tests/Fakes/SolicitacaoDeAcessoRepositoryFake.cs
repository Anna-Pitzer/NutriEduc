using Backend.Tests.Support;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>Repositório em memória de solicitações de acesso para os testes de serviço.</summary>
public class SolicitacaoDeAcessoRepositoryFake : ISolicitacaoDeAcessoRepository
{
    private readonly List<SolicitacaoDeAcesso> _itens = new();
    private int _proximoId = 1;

    public SolicitacaoDeAcesso Add(SolicitacaoDeAcesso solicitacao)
    {
        PropriedadesPrivadas.Definir(solicitacao, nameof(SolicitacaoDeAcesso.Id), _proximoId++);
        _itens.Add(solicitacao);
        return solicitacao;
    }

    public void Update(SolicitacaoDeAcesso solicitacao)
    {
        var indice = _itens.FindIndex(item => item.Id == solicitacao.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Solicitação {solicitacao.Id} não existe no fake.");

        _itens[indice] = solicitacao;
    }

    public SolicitacaoDeAcesso? Obter(int id) =>
        _itens.FirstOrDefault(item => item.Id == id);

    public SolicitacaoDeAcesso? ObterPendenteDoUsuario(int usuarioId) =>
        _itens.FirstOrDefault(item =>
            item.UsuarioId == usuarioId &&
            item.Status == StatusSolicitacaoAcesso.Pendente);

    public bool ExistePorUsuario(int usuarioId) =>
        _itens.Any(item => item.UsuarioId == usuarioId);

    public (List<SolicitacaoDeAcesso> Itens, int Total) Listar(
        FiltroSolicitacaoAcesso filtro,
        int pagina,
        int tamanho)
    {
        var consulta = _itens.AsEnumerable();

        if (filtro.UnidadeId is { } unidadeId)
            consulta = consulta.Where(item => item.UnidadeId == unidadeId);

        if (filtro.Status is { } status)
            consulta = consulta.Where(item => item.Status == status);

        if (filtro.Tipo is { } tipo)
            consulta = consulta.Where(item => item.TipoSolicitado == tipo);

        var filtrados = consulta
            .OrderByDescending(item => item.SolicitadoEmUtc)
            .ThenByDescending(item => item.Id)
            .ToList();

        return (
            filtrados.Skip((pagina - 1) * tamanho).Take(tamanho).ToList(),
            filtrados.Count);
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<SolicitacaoDeAcesso> GetItens() => _itens.ToList();
}
