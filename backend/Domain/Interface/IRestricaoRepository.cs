using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

/// <summary>
/// Catálogo de restrições e histórico por aluno (Spec 010). Recebe o
/// <c>ConnectionContext</c> por construtor (L4) e nunca abre conexão própria.
/// </summary>
public interface IRestricaoRepository
{
    /// <summary>Consulta única do caso de uso: devolve as entradas ativas pelos nomes normalizados.</summary>
    List<RestricaoAlimentar> ObterAtivasPorNomes(IEnumerable<string> nomesNormalizados);

    /// <summary>Catálogo para leitura; <paramref name="incluirInativas"/> respeita a política.</summary>
    List<RestricaoAlimentar> Listar(CategoriaRestricao? categoria, bool incluirInativas);

    RestricaoAlimentar? Obter(int id);

    /// <summary>Detecta a violação do índice único antes de gravar, para devolver mensagem.</summary>
    bool ExisteNomeNormalizado(string nomeNormalizado, CategoriaRestricao categoria, int? ignorarId);

    RestricaoAlimentar Add(RestricaoAlimentar restricao);

    void Update(RestricaoAlimentar restricao);
}
