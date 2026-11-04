namespace Ntc.Domain.Entity;

/// <summary>
/// Escola da rede. Pertence a exatamente uma unidade (Spec 027) e o nome é único dentro
/// dela. `Ativa` substitui a exclusão física (ADR-008).
/// </summary>
public class Escola
{
    private const int NomeMin = 2;
    private const int NomeMax = 120;

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>
    /// Unidade à qual a escola pertence. Nulo apenas em registro legado anterior à
    /// Spec 027; o construtor público nunca produz esse estado.
    /// </summary>
    public int? UnidadeId { get; private set; }

    public bool Ativa { get; private set; } = true;

    /// <summary>Somente para o EF Core.</summary>
    private Escola()
    {
    }

    public Escola(string nome, int unidadeId)
    {
        if (unidadeId <= 0)
        {
            throw new ArgumentException("A escola exige uma unidade.");
        }

        DefinirNome(nome);
        UnidadeId = unidadeId;
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

    /// <summary>
    /// Move a escola de unidade. Bloqueado quando a escola possui alunos, porque o aluno
    /// carrega a unidade derivada e a mudança criaria divergência silenciosa (Spec 027, RF5).
    /// </summary>
    public void DefinirUnidade(int unidadeId, bool possuiAlunos)
    {
        if (unidadeId <= 0)
        {
            throw new ArgumentException("A escola exige uma unidade.");
        }

        if (UnidadeId == unidadeId)
        {
            return;
        }

        if (possuiAlunos)
        {
            throw new ArgumentException(
                "Escola com alunos não pode mudar de unidade.");
        }

        UnidadeId = unidadeId;
    }

    public void Desativar() => Ativa = false;

    public void Reativar() => Ativa = true;
}
