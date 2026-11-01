namespace Ntc.Domain.Entity;

/// <summary>
/// Turma de uma escola: a unidade que define onde e em que série o aluno está.
///
/// Objeto de valor: não tem identidade própria — a identidade vem do
/// <see cref="Aluno"/> que o possui. Duas turmas com a mesma escola e série são
/// equivalentes, e por isso a comparação é feita pelos seus dados.
///
/// O <see cref="Aluno"/> delega a este objeto as invariantes de escola e série, de modo
/// que não existe regra duplicada entre os dois.
/// </summary>
public class Turma
{
    private const int EscolaMin = 2;
    private const int EscolaMax = 150;
    private const int SerieMin = 2;
    private const int SerieMax = 50;

    public string Escola { get; private set; } = string.Empty;

    public string Serie { get; private set; } = string.Empty;

    /// <summary>Somente para o EF Core materializar o objeto possuído. Não valida.</summary>
    private Turma()
    {
    }

    public Turma(string escola, string serie)
    {
        var escolaValidada = ValidarEscola(escola);
        var serieValidada = ValidarSerie(serie);

        Escola = escolaValidada;
        Serie = serieValidada;
    }

    /// <summary>
    /// Move o aluno para outra escola/série. Valida tudo antes de atribuir.
    /// </summary>
    public void Transferir(string escola, string serie)
    {
        var escolaValidada = ValidarEscola(escola);
        var serieValidada = ValidarSerie(serie);

        Escola = escolaValidada;
        Serie = serieValidada;
    }

    private static string ValidarEscola(string? escola)
    {
        return ValidacaoDeCampo.Texto(escola, "Escola", EscolaMin, EscolaMax);
    }

    private static string ValidarSerie(string? serie)
    {
        return ValidacaoDeCampo.Texto(serie, "Série", SerieMin, SerieMax);
    }
}
