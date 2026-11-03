using Ntc.Domain;
using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Porta de leitura e escrita de cardápios (Spec 012). Recebe o <c>ConnectionContext</c>
/// por construtor (L4) e não abre conexão própria.
///
/// O filtro não carrega autorização: o recorte por papel é decidido no serviço e chega
/// aqui já traduzido em <see cref="FiltroCardapio"/> (Spec 024, F5).
/// </summary>
public interface ICardapioRepository
{
    /// <summary>
    /// Página de cardápios com total da consulta inteira (RNF4). Carrega as refeições
    /// (sem itens) para a listagem informar a composição sem uma consulta por linha.
    /// </summary>
    (List<Cardapio> Itens, int Total) Listar(FiltroCardapio filtro, int pagina, int tamanho);

    /// <summary>
    /// Página de cardápios publicados no recorte, com refeições, itens e alimentos — é a
    /// resposta da rota pública (RF9), que precisa dos totais.
    /// </summary>
    (List<Cardapio> Itens, int Total) ListarPublicados(
        FiltroCardapio filtro,
        int pagina,
        int tamanho);

    /// <summary>Detalhe: refeições, itens e o alimento de cada item resolvidos (RF11).</summary>
    Cardapio? ObterComRefeicoes(int id);

    /// <summary>Usado pela política de exclusão de unidade (Spec 012, RNF/restrições).</summary>
    bool ExisteCardapioDaUnidade(int unidadeId);

    /// <summary>Ids de alimentos usados em cardápios publicados recentes da unidade (Spec 013, RF8).</summary>
    IReadOnlyCollection<int> ObterAlimentosUsadosDesde(int unidadeId, DateOnly dataLimite);

    /// <summary>Id do cardápio publicado da unidade com período sobreposto, ou nulo (Spec 015, RF4).</summary>
    int? ExistePublicadoNoPeriodo(int unidadeId, PeriodoVigencia periodo, int? ignorarId);

    /// <summary>Linha do tempo de eventos de um cardápio, decrescente e paginada (Spec 015, RF10).</summary>
    (List<HistoricoCardapio> Itens, int Total) ListarHistorico(int cardapioId, int pagina, int tamanho);

    /// <summary>Feed de alterações no escopo a partir de uma data, paginado (Spec 015, RF11).</summary>
    (List<AlteracaoCardapioItem> Itens, int Total) ListarAlteracoes(
        int? unidadeId,
        DateTime desde,
        int pagina,
        int tamanho);

    Cardapio Add(Cardapio cardapio);

    void Update(Cardapio cardapio);

    void Delete(Cardapio cardapio);
}
