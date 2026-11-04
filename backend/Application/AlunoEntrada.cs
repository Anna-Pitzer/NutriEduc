namespace Ntc.Application;

/// <summary>
/// Contrato de entrada de aluno (Spec 024, F1): só o que o cliente envia. Criar e
/// atualizar compartilham a mesma forma porque o identificador vem da rota — o que não
/// pode existir é um contrato de entrada devolvido como saída.
/// </summary>
public sealed record AlunoEntrada
{
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

    /// <summary>
    /// Escola obrigatória (Spec 027): a unidade é derivada de `Escola.UnidadeId` e não
    /// é aceita no payload. Só aluno vinculado a uma escola participa da análise de
    /// compatibilidade.
    /// </summary>
    public int? EscolaId { get; init; }
}
