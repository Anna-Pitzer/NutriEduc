using Ntc.Domain.Entity;

namespace Ntc.Domain.Interface;

public interface IEscolaRepository
{
    Escola Add(Escola escola);
    Escola? GetEscola(int id);
    List<Escola> Listar();
    bool ExisteNome(string nome, int? ignorarId = null);
    void Update(Escola escola);

    UnidadeEscolar AddUnidade(UnidadeEscolar unidade);
    UnidadeEscolar? GetUnidade(int id);
    List<UnidadeEscolar> ListarUnidades();

    /// <summary>
    /// Busca unidades pelo nome exato, sem diferenciar caixa. Devolve lista porque o
    /// repositório não decide regra: o nome é único na rede, então mais de um resultado
    /// indica dado antigo inconsistente.
    /// </summary>
    List<UnidadeEscolar> BuscarUnidadesPorNome(string nome);
    bool ExisteNomeUnidade(string nome, int? ignorarId = null);
    void UpdateUnidade(UnidadeEscolar unidade);

    /// <summary>Exclusão definitiva: o chamador já verificou que não há referências.</summary>
    void Delete(Escola escola);
    void DeleteUnidade(UnidadeEscolar unidade);

    /// <summary>True quando alguma unidade ainda guarda esta escola como origem (legado).</summary>
    bool ExisteUnidadeDaEscola(int escolaId);
}
