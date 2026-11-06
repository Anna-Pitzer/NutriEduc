namespace Ntc.Domain.Entity;

/// <summary>
/// Confirmação explícita de que a nutricionista manteve, no cardápio geral, um alimento
/// incompatível com restrições de alunos (Spec 029, RF12 e RF13).
///
/// É append-only: registra autor, data UTC, alimento, escolas e conjuntos afetados. Não é um
/// aceite reutilizável — cada edição que reintroduza a incompatibilidade exige nova
/// confirmação.
/// </summary>
public class ConfirmacaoDeIncompatibilidade
{
    public int Id { get; private set; }

    public int CardapioId { get; private set; }

    public string AutorNome { get; private set; } = string.Empty;

    public DateTime CriadoEmUtc { get; private set; }

    public int AlimentoId { get; private set; }

    public string AlimentoNome { get; private set; } = string.Empty;

    /// <summary>Escolas afetadas, separadas por vírgula.</summary>
    public string Escolas { get; private set; } = string.Empty;

    /// <summary>Conjuntos de restrições afetados, separados por vírgula.</summary>
    public string Conjuntos { get; private set; } = string.Empty;

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private ConfirmacaoDeIncompatibilidade()
    {
    }

    internal ConfirmacaoDeIncompatibilidade(
        string autorNome,
        int alimentoId,
        string alimentoNome,
        string escolas,
        string conjuntos,
        DateTime criadoEmUtc)
    {
        if (alimentoId <= 0)
            throw new ArgumentException("A confirmação exige um alimento válido.");

        if (criadoEmUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A confirmação deve usar data UTC.");

        AutorNome = ValidacaoDeCampo.Texto(autorNome, "autor da confirmação", 2, 100);
        AlimentoId = alimentoId;
        AlimentoNome = ValidacaoDeCampo.Texto(alimentoNome, "alimento confirmado", 2, 100);
        Escolas = ValidacaoDeCampo.Texto(escolas, "escolas afetadas", 1, 500);
        Conjuntos = ValidacaoDeCampo.Texto(conjuntos, "conjuntos afetados", 1, 500);
        CriadoEmUtc = criadoEmUtc;
    }
}
