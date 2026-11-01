using Backend.Tests.Support;
using Ntc.Domain;
using Ntc.Domain.Entity;
using Ntc.Domain.Interface;

namespace Backend.Tests.Fakes;

/// <summary>
/// Catálogo em memória para os testes de serviço (Spec 011). Reproduz a busca em duas
/// etapas do repositório real — prefixo e, sem resultado, "contém" — porque essa é a
/// regra visível ao usuário (RF7).
/// </summary>
public class AlimentoRepositoryFake : IAlimentoRepository
{
    private readonly List<Alimento> _alimentos = new();
    private int _proximoId = 1;

    public (List<Alimento> Itens, int Total) Listar(
        FiltroAlimento filtro,
        int pagina,
        int tamanho)
    {
        var filtrados = _alimentos
            .Where(alimento => filtro.IncluirInativos || alimento.Ativo)
            .ToList();

        if (filtro.CategoriaId is { } categoriaId)
        {
            filtrados = filtrados
                .Where(alimento => alimento.CategoriaId == categoriaId)
                .ToList();
        }

        if (!string.IsNullOrEmpty(filtro.NomeNormalizado))
        {
            var termo = filtro.NomeNormalizado;

            var porPrefixo = filtrados
                .Where(alimento => alimento.NomeNormalizado.StartsWith(termo, StringComparison.Ordinal))
                .ToList();

            filtrados = porPrefixo.Count > 0
                ? porPrefixo
                : filtrados
                    .Where(alimento => alimento.NomeNormalizado.Contains(termo, StringComparison.Ordinal))
                    .ToList();
        }

        // A ordenação é regra visível (RF7): o fake reproduz a mesma escolha do
        // repositório real para os testes de serviço enxergarem a mesma página.
        var itens = filtro.Ordenacao switch
        {
            OrdenacaoAlimento.NomeDescendente => filtrados
                .OrderByDescending(alimento => alimento.Nome)
                .ThenBy(alimento => alimento.Id),

            OrdenacaoAlimento.Categoria => filtrados
                .OrderBy(alimento => alimento.Categoria?.Nome)
                .ThenBy(alimento => alimento.Nome)
                .ThenBy(alimento => alimento.Id),

            OrdenacaoAlimento.Recentes => filtrados
                .OrderByDescending(alimento => alimento.Id),

            _ => filtrados
                .OrderBy(alimento => alimento.Nome)
                .ThenBy(alimento => alimento.Id)
        };

        return (itens
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToList(), filtrados.Count);
    }

    public Alimento? Obter(int id) =>
        _alimentos.FirstOrDefault(alimento => alimento.Id == id);

    public Alimento? ObterComVinculos(int id) => Obter(id);

    public List<Alimento> ObterPorIds(IEnumerable<int> ids)
    {
        var solicitados = ids.ToHashSet();

        return _alimentos.Where(alimento => solicitados.Contains(alimento.Id)).ToList();
    }

    public bool ExisteNomeNormalizado(string nomeNormalizado, int? ignorarId) =>
        _alimentos.Any(alimento =>
            alimento.NomeNormalizado == nomeNormalizado &&
            (ignorarId == null || alimento.Id != ignorarId));

    public Alimento Add(Alimento alimento)
    {
        if (alimento.Id == 0)
        {
            PropriedadesPrivadas.Definir(alimento, nameof(Alimento.Id), _proximoId++);
        }

        _alimentos.Add(alimento);

        return alimento;
    }

    public void Update(Alimento alimento)
    {
        var indice = _alimentos.FindIndex(item => item.Id == alimento.Id);

        if (indice < 0)
            throw new InvalidOperationException($"Alimento {alimento.Id} não existe no catálogo fake.");

        _alimentos[indice] = alimento;
    }

    /// <summary>Somente para asserções dos testes.</summary>
    public List<Alimento> GetCatalogo() => _alimentos.ToList();
}
