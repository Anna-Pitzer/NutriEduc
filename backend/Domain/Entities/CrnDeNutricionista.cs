using System.Text.RegularExpressions;

namespace Ntc.Domain.Entity;

/// <summary>
/// Objeto de valor do registro profissional da nutricionista (Spec 031).
///
/// É identidade profissional, não texto livre: a entrada é normalizada uma única vez
/// (maiúsculas e separadores colapsados), validada contra a faixa de regiões e devolvida
/// na forma canônica <c>CRN-&lt;região&gt;-&lt;número&gt;</c>. Igualdade é definida pelo valor
/// normalizado, então <c>"crn-3-12345"</c> e <c>"CRN.3.12345"</c> representam o mesmo CRN.
///
/// A validação confere <b>formato</b>, nunca autenticidade (RF12): não há consulta ao
/// conselho. A faixa de regiões vive em <see cref="RegioesPadrao"/> — um único ponto de
/// mudança caso a lista oficial mude.
/// </summary>
public sealed record CrnDeNutricionista
{
    public const string MensagemFormatoInvalido = "CRN inválido. Exemplo aceito: CRN-3-12345.";
    public const string MensagemNutricionistaSemCrn =
        "Informe o CRN da nutricionista no formato CRN-<região>-<número>.";
    public const string MensagemSomenteNutricionista = "Somente nutricionistas possuem CRN.";

    private static readonly Regex FormatoDoNumero = new(@"^\d{3,8}$", RegexOptions.Compiled);

    /// <summary>Faixa oficial de regiões (1..11). Configurável num único ponto (RF2/D3).</summary>
    public static IReadOnlySet<int> RegioesPadrao { get; } =
        new HashSet<int>(Enumerable.Range(1, 11));

    /// <summary>Forma canônica: <c>CRN-&lt;região&gt;-&lt;número&gt;</c>.</summary>
    public string Valor { get; }

    private CrnDeNutricionista(string valor) => Valor = valor;

    /// <summary>
    /// Colapsa espaços, pontos e hífens em um único hífen e converte para maiúsculas. A
    /// entrada aceita <c>CRN 3 12345</c>, <c>crn-3-12345</c> e <c>CRN.3.12345</c> e devolve
    /// <c>CRN-3-12345</c>.
    /// </summary>
    public static string Normalizar(string? entrada) =>
        Regex.Replace((entrada ?? string.Empty).Trim().ToUpperInvariant(), @"[\s.\-]+", "-");

    /// <summary>
    /// Valida e produz o objeto de valor. <paramref name="regioes"/> ausente usa
    /// <see cref="RegioesPadrao"/>. Em caso de sucesso, <paramref name="crn"/> carrega a forma
    /// canônica e <paramref name="erro"/> é nulo.
    /// </summary>
    public static bool TryParse(
        string? entrada,
        IReadOnlySet<int>? regioes,
        out CrnDeNutricionista? crn,
        out string? erro)
    {
        crn = null;

        if (string.IsNullOrWhiteSpace(entrada))
        {
            erro = MensagemFormatoInvalido;
            return false;
        }

        var normalizado = Normalizar(entrada);
        var partes = normalizado.Split('-');

        if (partes.Length != 3 || partes[0] != "CRN")
        {
            erro = MensagemFormatoInvalido;
            return false;
        }

        if (!int.TryParse(partes[1], out var regiao) ||
            !(regioes ?? RegioesPadrao).Contains(regiao))
        {
            erro = MensagemFormatoInvalido;
            return false;
        }

        if (!FormatoDoNumero.IsMatch(partes[2]))
        {
            erro = MensagemFormatoInvalido;
            return false;
        }

        crn = new CrnDeNutricionista($"CRN-{regiao}-{partes[2]}");
        erro = null;
        return true;
    }

    /// <summary>Predicado de validade de formato, usado em <c>crnValido</c> e no diagnóstico.</summary>
    public static bool EhValido(string? entrada, IReadOnlySet<int>? regioes = null) =>
        TryParse(entrada, regioes, out _, out _);
}
