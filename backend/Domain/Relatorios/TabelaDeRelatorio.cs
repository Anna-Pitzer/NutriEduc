namespace Ntc.Domain.Relatorios;

/// <summary>
/// Tabela pronta para exibição e exportação (ADR-012): colunas, linhas e rodapé já
/// formatados. Nenhuma camada acima recalcula ou reordena.
/// </summary>
public sealed record TabelaDeRelatorio(
    string Titulo,
    IReadOnlyList<string> Colunas,
    IReadOnlyList<IReadOnlyList<string>> Linhas,
    string? Rodape = null)
{
    public static TabelaDeRelatorio Vazia(string titulo, IReadOnlyList<string> colunas, string aviso) =>
        new(titulo, colunas, Array.Empty<IReadOnlyList<string>>(), aviso);
}
