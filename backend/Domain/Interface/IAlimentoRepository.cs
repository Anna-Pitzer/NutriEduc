using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de leitura e escrita do catálogo de alimentos (Spec 011). Recebe o
/// <c>ConnectionContext</c> por construtor (L4) e nunca abre conexão própria.
///
/// O filtro não carrega autorização: a decisão de papel fica no serviço e chega aqui já
/// traduzida em <see cref="FiltroAlimento"/> (Spec 024, F5).
/// </summary>
public interface IAlimentoRepository
{
    /// <summary>
    /// Página de alimentos com total da consulta inteira (RNF4). A busca é por prefixo e,
    /// quando o prefixo não devolve nada, repete por "contém" (RF7).
    /// </summary>
    (List<Alimento> Itens, int Total) Listar(FiltroAlimento filtro, int pagina, int tamanho);

    /// <summary>Leitura simples, sem vínculos nem categoria carregados.</summary>
    Alimento? Obter(int id);

    /// <summary>Leitura de detalhe e de edição: categoria e vínculos resolvidos (RF8).</summary>
    Alimento? ObterComVinculos(int id);

    /// <summary>
    /// Leitura em lote pela composição do cardápio (Spec 012): uma consulta para todos os
    /// alimentos de um cardápio, em vez de uma por item.
    /// </summary>
    List<Alimento> ObterPorIds(IEnumerable<int> ids);

    /// <summary>Detecta a violação do índice único antes de gravar, para devolver 409.</summary>
    bool ExisteNomeNormalizado(string nomeNormalizado, int? ignorarId);

    Alimento Add(Alimento alimento);

    void Update(Alimento alimento);
}
