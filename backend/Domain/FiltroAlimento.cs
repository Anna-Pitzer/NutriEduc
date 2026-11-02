namespace Ntc.Domain;

/// <summary>
/// Ordenação da listagem do catálogo de alimentos (Spec 011, RF7).
///
/// Só ordena por texto e por identidade. No SQLite a coluna <c>decimal(10,2)</c> tem
/// afinidade NUMERIC e o valor é gravado como REAL/INTEGER — sem escala nem exatidão
/// decimal (PEND-029) —, então ordenar por medida compararia floats, não decimais. A
/// Spec 012 registra a mesma limitação; quando for necessário, a ordenação por medida
/// terá de ocorrer em memória sobre o conjunto filtrado, nunca no banco.
/// </summary>
public enum OrdenacaoAlimento
{
    /// <summary>Nome de A a Z (padrão).</summary>
    Nome,

    /// <summary>Nome de Z a A.</summary>
    NomeDescendente,

    /// <summary>Nome da categoria e, dentro dela, nome do alimento.</summary>
    Categoria,

    /// <summary>Cadastrados por último primeiro.</summary>
    Recentes
}

/// <summary>
/// Filtro de consulta do catálogo de alimentos (Spec 011, RF7). Não contém nenhuma
/// palavra de autorização: o recorte por papel é decidido no serviço, como em
/// <see cref="FiltroAluno"/> (Spec 024, F5).
/// </summary>
/// <param name="NomeNormalizado">Busca já normalizada; nulo não filtra por nome.</param>
/// <param name="CategoriaId">Categoria específica; nulo lista todas.</param>
/// <param name="IncluirInativos">Inclui alimentos desativados; exclusivo de Nutricionista.</param>
/// <param name="Ordenacao">Ordem da página; o padrão preserva o comportamento anterior.</param>
public sealed record FiltroAlimento(
    string? NomeNormalizado = null,
    int? CategoriaId = null,
    bool IncluirInativos = false,
    OrdenacaoAlimento Ordenacao = OrdenacaoAlimento.Nome);
