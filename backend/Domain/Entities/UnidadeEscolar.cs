namespace Ntc.Domain.Entity;

/// <summary>
/// Unidade organizacional da rede — a secretaria, não a escola. É a unidade usada pelo
/// bootstrap para atribuir a origem de um administrador e pelo escopo de merendeira e
/// aluno. `Ativa` substitui a exclusão física (ADR-008).
/// </summary>
public class UnidadeEscolar
{
    private const int NomeMin = 2;
    private const int NomeMax = 120;

    public int Id { get; private set; }

    /// <summary>
    /// Escola de origem, quando a unidade nasceu como subdivisão de uma escola (dado
    /// legado da Spec 009). O cadastro novo não preenche: a unidade é da rede. Mantido
    /// porque o escopo de `AdminEscola` sobre alunos ainda o utiliza.
    /// </summary>
    public int? EscolaId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public bool Ativa { get; private set; } = true;

    /// <summary>Somente para o EF Core.</summary>
    private UnidadeEscolar()
    {
    }

    public UnidadeEscolar(string nome)
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
                $"O nome da unidade deve ter entre {NomeMin} e {NomeMax} caracteres.");
        }

        Nome = valor;
    }

    public void Desativar() => Ativa = false;

    public void Reativar() => Ativa = true;
}
