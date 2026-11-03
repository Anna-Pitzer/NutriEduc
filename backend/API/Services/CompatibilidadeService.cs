using Ntc.Application;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;
using Ntc.Domain.Services;

/// <summary>
/// Caso de uso da compatibilidade (Spec 014). Carrega cardápio, alimentos com vínculos e
/// alunos da unidade (uma consulta por agregado) e delega o cruzamento ao domínio, que é a
/// única fonte de severidade (ADR-011). O escopo por papel é decidido aqui: merendeira só vê
/// a própria unidade; responsável e admin escolar não acessam a visão agregada (RF8).
/// </summary>
public sealed class CompatibilidadeService
{
    private readonly ICardapioRepository _cardapios;
    private readonly IAlimentoRepository _alimentos;
    private readonly IAlunoRepository _alunos;

    public CompatibilidadeService(
        ICardapioRepository cardapios,
        IAlimentoRepository alimentos,
        IAlunoRepository alunos)
    {
        _cardapios = cardapios;
        _alimentos = alimentos;
        _alunos = alunos;
    }

    public Resultado<CompatibilidadeDTO> Analisar(int cardapioId, EscopoUsuario escopo)
    {
        if (escopo.Papel is not (PapelUsuario.Nutricionista or PapelUsuario.Merendeira))
        {
            return Resultado<CompatibilidadeDTO>.Proibido();
        }

        var cardapio = _cardapios.ObterComRefeicoes(cardapioId);

        if (cardapio is null)
        {
            return Resultado<CompatibilidadeDTO>.NaoEncontrado("Cardápio não encontrado.");
        }

        if (escopo.Papel == PapelUsuario.Merendeira && escopo.UnidadeId != cardapio.UnidadeId)
        {
            return Resultado<CompatibilidadeDTO>.Proibido();
        }

        var itens = MontarItens(cardapio);
        var vinculosPorAlimento = MontarVinculos(itens);
        var alunos = MontarAlunos(cardapio.UnidadeId);

        var resultado = AnaliseCompatibilidade.Analisar(itens, vinculosPorAlimento, alunos);

        return Resultado<CompatibilidadeDTO>.Ok(ParaDTO(resultado));
    }

    private static List<ItemAnalisado> MontarItens(Cardapio cardapio) =>
        cardapio.Refeicoes
            .OrderBy(refeicao => refeicao.Ordem)
            .SelectMany(refeicao => refeicao.Itens.Select(item => new ItemAnalisado(
                refeicao.Ordem,
                refeicao.Nome,
                item.AlimentoId,
                item.Alimento?.Nome ?? string.Empty)))
            .ToList();

    private Dictionary<int, IReadOnlyList<VinculoAnalisado>> MontarVinculos(
        IReadOnlyList<ItemAnalisado> itens)
    {
        var ids = itens.Select(item => item.AlimentoId).Distinct().ToList();
        var porAlimento = new Dictionary<int, IReadOnlyList<VinculoAnalisado>>();

        foreach (var alimento in _alimentos.ObterComVinculosPorIds(ids))
        {
            porAlimento[alimento.Id] = alimento.Vinculos
                .Where(vinculo => vinculo.Restricao is not null)
                .Select(vinculo => new VinculoAnalisado(
                    vinculo.RestricaoId,
                    vinculo.Restricao!.Nome,
                    vinculo.Restricao.Categoria))
                .ToList();
        }

        return porAlimento;
    }

    private List<AlunoAnalisado> MontarAlunos(int unidadeId) =>
        _alunos.Listar(new FiltroAluno(UnidadeId: unidadeId))
            .Select(aluno => new AlunoAnalisado(aluno.Id, aluno.Nome, aluno.Restricoes))
            .ToList();

    private static CompatibilidadeDTO ParaDTO(ResultadoCompatibilidade resultado) => new()
    {
        Compativel = resultado.Compativel,
        TotalAlunosAnalisados = resultado.TotalAlunosAnalisados,
        Bloqueantes = resultado.Bloqueantes.Select(ParaAchado).ToList(),
        Alertas = resultado.Alertas.Select(ParaAchado).ToList(),
        AlimentosPermitidos = resultado.AlimentosPermitidos.ToList(),
        AlimentosNaoPermitidos = resultado.AlimentosNaoPermitidos.ToList(),
        Avisos = resultado.Avisos.ToList()
    };

    private static AchadoCompatibilidadeDTO ParaAchado(AchadoCompatibilidade achado) => new()
    {
        Refeicao = achado.RefeicaoNome,
        AlimentoId = achado.AlimentoId,
        Alimento = achado.AlimentoNome,
        Restricao = achado.RestricaoNome,
        Severidade = achado.Severidade,
        Alunos = achado.AlunosAfetados
            .Select(aluno => new AlunoAfetadoCompatibilidadeDTO { Id = aluno.Id, Nome = aluno.Nome })
            .ToList()
    };
}
