namespace Ntc.Domain.Entity;

/// <summary>
/// Categoria do catálogo de alimentos (Spec 011, RF1): agrupa alimentos para navegação e
/// para o filtro da listagem. O nome é validado aqui — formato, limite e unicidade
/// derivada do nome normalizado — e a desativação não apaga: só impede novos alimentos.
/// </summary>
public class CategoriaAlimento
{
    private const int NomeMin = 2;
    private const int NomeMax = 60;

    /// <summary>Somente para o EF Core materializar a entidade. Não valida.</summary>
    private CategoriaAlimento()
    {
    }

    public CategoriaAlimento(string nome)
    {
        DefinirNome(nome);
        Ativa = true;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>Nome sem caixa e sem acento, persistido para o índice único.</summary>
    public string NomeNormalizado { get; private set; } = string.Empty;

    public bool Ativa { get; private set; } = true;

    /// <summary>
    /// Redefine o nome. A unicidade depende do índice único em <c>NomeNormalizado</c>; o
    /// serviço checa antes de gravar para devolver 409 com mensagem.
    /// </summary>
    public void DefinirNome(string nome)
    {
        var valor = ValidacaoDeCampo.Texto(nome, "Nome da categoria", NomeMin, NomeMax);

        Nome = valor;
        NomeNormalizado = RestricaoAlimentar.Normalizar(valor);
    }

    public void Desativar() => Ativa = false;

    public void Reativar() => Ativa = true;
}
