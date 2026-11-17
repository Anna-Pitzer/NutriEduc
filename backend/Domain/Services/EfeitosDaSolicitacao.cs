using Ntc.Domain.Entity;

namespace Ntc.Domain.Services;

/// <summary>
/// Efeitos que a aprovação de uma solicitação produz no aluno (Spec 017, RF6; Spec 035).
///
/// A resolução do nome da restrição contra o catálogo ativo e a marcação de anafilaxia
/// vivem no domínio porque alteram o estado do agregado e registram histórico. Os casos de
/// uso apenas carregam o aluno e o catálogo e entregam os dois a esta regra — nenhum
/// serviço chama outro serviço (ADR-004, ADR-018).
/// </summary>
public static class EfeitosDaSolicitacao
{
    /// <summary>
    /// Resolve o nome contra o catálogo ativo, adiciona a restrição ao aluno e registra o
    /// evento na trilha. O chamador persiste o aluno na mesma transação da solicitação.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Restrição vazia, desconhecida, inativa ou já vinculada ao aluno.
    /// </exception>
    public static void AplicarRestricao(
        Aluno aluno,
        string restricao,
        IReadOnlyList<RestricaoAlimentar> catalogoAtivo,
        string autor)
    {
        var resolvidas = new CatalogoDeRestricoes(catalogoAtivo)
            .ResolverNomes(new[] { restricao ?? string.Empty });

        if (resolvidas.Count == 0)
        {
            throw new ArgumentException("Informe a restrição aprovada.");
        }

        var nome = resolvidas.Single();

        aluno.AdicionarRestricao(nome);

        aluno.RegistrarHistorico(
            Array.Empty<AlteracaoDeDado>(),
            new[] { nome },
            Array.Empty<string>(),
            autor);
    }

    /// <summary>
    /// Marca a anafilaxia confirmada na análise da solicitação e registra o evento com a
    /// origem. Idempotente: se o aluno já está marcado, não cria evento repetido.
    /// </summary>
    public static void MarcarAnafilaxia(
        Aluno aluno,
        string autor,
        string? origem = null)
    {
        if (aluno.Anafilaxia)
        {
            return;
        }

        aluno.MarcarAnafilaxia();

        aluno.RegistrarHistorico(
            new[] { new AlteracaoDeDado("Anafilaxia", "Não", "Sim") },
            Array.Empty<string>(),
            Array.Empty<string>(),
            autor,
            origem);
    }
}
