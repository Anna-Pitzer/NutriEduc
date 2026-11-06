using Backend.Tests.Support;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória de solicitações (Spec 017). Reproduz os filtros visíveis ao
/// serviço, inclusive o recorte do responsável por vínculo com o aluno.
/// </summary>
public class SolicitacaoRepositoryFake : ISolicitacaoRepository
{
    private readonly List<Solicitacao> _itens = new();
    private int _proximoId = 1;

    public HashSet<(int ResponsavelUsuarioId, int AlunoId)> Vinculos { get; } = new();

    public Solicitacao Add(Solicitacao solicitacao)
    {
        PropriedadesPrivadas.Definir(solicitacao, nameof(Solicitacao.Id), _proximoId++);
        _itens.Add(solicitacao);
        return solicitacao;
    }

    public void Update(Solicitacao solicitacao)
    {
        var indice = _itens.FindIndex(item => item.Id == solicitacao.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Solicitação {solicitacao.Id} não existe no fake.");

        _itens[indice] = solicitacao;
    }

    public Solicitacao? ObterComHistorico(int id) =>
        _itens.FirstOrDefault(item => item.Id == id);

    public (List<Solicitacao> Itens, int Total) Listar(
        FiltroSolicitacao filtro,
        int pagina,
        int tamanho)
    {
        var consulta = _itens.AsEnumerable();

        if (filtro.Tipo is { } tipo)
            consulta = consulta.Where(item => item.Tipo == tipo);

        if (filtro.Status is { } status)
            consulta = consulta.Where(item => item.Status == status);

        if (filtro.TiposExcluidos is { Count: > 0 } excluidos)
            consulta = consulta.Where(item => !excluidos.Contains(item.Tipo));

        if (filtro.UnidadeId is { } unidadeId)
            consulta = consulta.Where(item => item.UnidadeId == unidadeId);

        if (filtro.EscolaId is { } escolaId)
            consulta = consulta.Where(item => item.EscolaId == escolaId);

        if (filtro.AlunoId is { } alunoId)
            consulta = consulta.Where(item => item.AlunoId == alunoId);

        if (filtro.CardapioId is { } cardapioId)
            consulta = consulta.Where(item => item.CardapioId == cardapioId);

        if (filtro.De is { } de)
            consulta = consulta.Where(item => item.CriadoEmUtc >= de);

        if (filtro.Ate is { } ate)
            consulta = consulta.Where(item => item.CriadoEmUtc <= ate);

        if (filtro.SolicitanteId is { } solicitanteId)
            consulta = consulta.Where(item => item.SolicitanteId == solicitanteId);

        if (filtro.ResponsavelUsuarioId is { } responsavelId)
        {
            var vinculados = Vinculos
                .Where(vinculo => vinculo.ResponsavelUsuarioId == responsavelId)
                .Select(vinculo => vinculo.AlunoId)
                .ToHashSet();

            consulta = consulta.Where(item =>
                item.SolicitanteId == responsavelId ||
                (item.AlunoId is { } alunoId && vinculados.Contains(alunoId)));
        }

        var filtrados = consulta
            .OrderByDescending(item => item.CriadoEmUtc)
            .ThenByDescending(item => item.Id)
            .ToList();

        return (
            filtrados.Skip((pagina - 1) * tamanho).Take(tamanho).ToList(),
            filtrados.Count);
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<Solicitacao> GetItens() => _itens.ToList();

    public int? IdDaAbertaParaCampo(int alunoId, CampoDeDadoDoAluno campo) =>
        _itens
            .Where(item =>
                item.Tipo == TipoSolicitacao.DadosCadastrais &&
                item.AlunoId == alunoId &&
                (item.Status == StatusSolicitacao.Aberta ||
                 item.Status == StatusSolicitacao.EmAnalise) &&
                item.Alteracoes.Any(alteracao => alteracao.Campo == campo))
            .Select(item => (int?)item.Id)
            .FirstOrDefault();
}
