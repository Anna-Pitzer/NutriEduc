namespace Ntc.Application;

/// <summary>
/// Contrato de saída de aluno (Spec 024, F1). Nenhuma rota devolve a entidade de domínio:
/// este é o único tipo que atravessa o HTTP, e ele nunca é aceito como entrada.
/// </summary>
public sealed record AlunoResposta
{
    public int Id { get; init; }
    public string Nome { get; init; } = string.Empty;
    public DateOnly Nascimento { get; init; }
    public string Escola { get; init; } = string.Empty;
    public string Serie { get; init; } = string.Empty;
    public string Telefone { get; init; } = string.Empty;
    public string Observacao { get; init; } = string.Empty;
    public List<string> Restricoes { get; init; } = new();
    public bool Anafilaxia { get; init; }
    public string NomeResponsavel { get; init; } = string.Empty;
    public string TelefoneResponsavel { get; init; } = string.Empty;
    public int? EscolaId { get; init; }
    public int? UnidadeId { get; init; }
}
