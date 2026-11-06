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

    /// <summary>
    /// Detalhe: refeições, itens, alimento de cada item e os registros da adaptação
    /// (substituições, pendências e confirmações) resolvidos (Spec 012, RF11; Spec 029, T5).
    /// </summary>
    Cardapio? ObterComRefeicoes(int id);

    /// <summary>
    /// Versão adaptada vigente de (origem, escola, conjunto): base da idempotência (Spec 029,
    /// RF3 e RNF5). Versões substituídas são históricas e não são devolvidas.
    /// </summary>
    Cardapio? ObterAdaptado(int origemId, int escolaId, string conjuntoChave);

    /// <summary>Versões adaptadas vigentes de um geral, para listagem e bloqueio de publicação (RF14).</summary>
    List<Cardapio> ListarAdaptados(int origemId);

    /// <summary>Quantidade de versões adaptadas vigentes da origem, para o teto de 50 (D6).</summary>
    int ContarAdaptados(int origemId);

    /// <summary>Geral publicado cujo período cobre a data, na unidade — versão de aluno sem restrição (RF15).</summary>
    Cardapio? ObterGeralPublicado(int unidadeId, DateOnly data);

    /// <summary>Adaptado publicado de (origem, escola, conjunto) cujo período cobre a data (RF15).</summary>
    Cardapio? ObterAdaptadoPublicado(
        int origemId,
        int escolaId,
        string conjuntoChave,
        DateOnly data);

    /// <summary>Usado pela política de exclusão de unidade (Spec 012, RNF/restrições).</summary>
    bool ExisteCardapioDaUnidade(int unidadeId);

    /// <summary>Ids de alimentos usados em cardápios publicados recentes da unidade (Spec 013, RF8).</summary>
    IReadOnlyCollection<int> ObterAlimentosUsadosDesde(int unidadeId, DateOnly dataLimite);

    /// <summary>
    /// Id do cardápio publicado com período sobreposto, ou nulo (Spec 015, RF4). A partir da
    /// Spec 029 o confronto é feito dentro do mesmo tipo: geral contra geral, adaptado
    /// contra adaptado da mesma escola e conjunto — o par geral/adaptado coexiste no período.
    /// </summary>
    int? ExistePublicadoNoPeriodo(
        int unidadeId,
        PeriodoVigencia periodo,
        int? ignorarId,
        TipoCardapio? tipo = null,
        int? escolaId = null,
        string? conjuntoChave = null);

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
