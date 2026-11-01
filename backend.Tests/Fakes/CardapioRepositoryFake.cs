using Backend.Tests.Support;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Repositório em memória de cardápios para os testes de serviço. Reproduz as regras
/// visíveis da consulta — filtro por unidade, situação, data e refeição —, porque é o que
/// o serviço precisa enxergar para decidir escopo.
/// </summary>
public class CardapioRepositoryFake : ICardapioRepository
{
    private readonly List<Cardapio> _cardapios = new();
    private int _proximoId = 1;

    /// <summary>
    /// Resolve o alimento de cada item, como o `Include` do repositório real faz. Sem isso
    /// o serviço montaria DTOs sem nome nem valores — um detalhe que o fake precisa
    /// reproduzir para o teste enxergar o mesmo contrato.
    /// </summary>
    public Func<int, Alimento?>? ResolverAlimento { get; set; }

    public (List<Cardapio> Itens, int Total) Listar(
        FiltroCardapio filtro,
        int pagina,
        int tamanho)
    {
        var filtrados = Filtrar(filtro)
            .OrderByDescending(cardapio => cardapio.Id)
            .ToList();

        return (Paginar(filtrados, pagina, tamanho), filtrados.Count);
    }

    public (List<Cardapio> Itens, int Total) ListarPublicados(
        FiltroCardapio filtro,
        int pagina,
        int tamanho)
    {
        var filtrados = Filtrar(filtro)
            .Where(cardapio => cardapio.Situacao == SituacaoCardapio.Publicado)
            .OrderBy(cardapio => cardapio.Periodo.Inicio)
            .ToList();

        foreach (var cardapio in filtrados)
            ResolverAlimentos(cardapio);

        return (Paginar(filtrados, pagina, tamanho), filtrados.Count);
    }

    public Cardapio? ObterComRefeicoes(int id)
    {
        var cardapio = _cardapios.FirstOrDefault(item => item.Id == id);

        if (cardapio is not null)
            ResolverAlimentos(cardapio);

        return cardapio;
    }

    private void ResolverAlimentos(Cardapio cardapio)
    {
        if (ResolverAlimento is null) return;

        foreach (var item in cardapio.Refeicoes.SelectMany(refeicao => refeicao.Itens))
        {
            var alimento = ResolverAlimento(item.AlimentoId);

            if (alimento is not null)
                PropriedadesPrivadas.Definir(item, nameof(ItemRefeicao.Alimento), alimento);
        }
    }

    public bool ExisteCardapioDaUnidade(int unidadeId) =>
        _cardapios.Any(cardapio => cardapio.UnidadeId == unidadeId);

    public Cardapio Add(Cardapio cardapio)
    {
        if (cardapio.Id == 0)
        {
            PropriedadesPrivadas.Definir(cardapio, nameof(Cardapio.Id), _proximoId++);
        }

        _cardapios.Add(cardapio);

        return cardapio;
    }

    public void Update(Cardapio cardapio)
    {
        var indice = _cardapios.FindIndex(item => item.Id == cardapio.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Cardápio {cardapio.Id} não existe no fake.");

        _cardapios[indice] = cardapio;
    }

    public void Delete(Cardapio cardapio)
    {
        _cardapios.RemoveAll(item => item.Id == cardapio.Id);
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<Cardapio> GetCardapios() => _cardapios.ToList();

    private IEnumerable<Cardapio> Filtrar(FiltroCardapio filtro)
    {
        var consulta = _cardapios.AsEnumerable();

        if (filtro.UnidadeId is { } unidadeId)
        {
            consulta = consulta.Where(cardapio => cardapio.UnidadeId == unidadeId);
        }

        if (filtro.Situacao is { } situacao)
        {
            consulta = consulta.Where(cardapio => cardapio.Situacao == situacao);
        }

        if (filtro.Data is { } data)
        {
            consulta = consulta.Where(cardapio => cardapio.Periodo.Contem(data));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Refeicao))
        {
            consulta = consulta.Where(cardapio => cardapio.Refeicoes.Any(refeicao =>
                string.Equals(refeicao.Nome, filtro.Refeicao.Trim(), StringComparison.OrdinalIgnoreCase)));
        }

        return consulta;
    }

    private static List<Cardapio> Paginar(List<Cardapio> itens, int pagina, int tamanho) =>
        itens.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
}
