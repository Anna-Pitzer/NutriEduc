namespace Ntc.Domain.Entity;

/// <summary>
/// Escola da rede. O nome é o rótulo exibido e a chave de unicidade (comparação sem
/// diferenciar caixa); `Ativa` substitui a exclusão física (ADR-008).
/// </summary>
public class Escola
{
    private const int NomeMin = 2;
    private const int NomeMax = 120;

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public bool Ativa { get; private set; } = true;

    /// <summary>Somente para o EF Core.</summary>
    private Escola()
    {
    }

    public Escola(string nome)
    {
        DefinirNome(nome);
        Ativa = true;
    }

    public void DefinirNome(string nome)
    {
        var valor = (nome ?? string.Empty).Trim();

        if (valor.Length < NomeMin || valor.Length > NomeMax)
        {
            throw new ArgumentException(
                $"O nome da escola deve ter entre {NomeMin} e {NomeMax} caracteres.");
        }

        Nome = valor;
    }

    public void Desativar() => Ativa = false;

    public void Reativar() => Ativa = true;
}
