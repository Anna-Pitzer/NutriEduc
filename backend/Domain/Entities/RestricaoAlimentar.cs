using System.Globalization;
using System.Text;

namespace Ntc.Domain.Entity;

/// <summary>Categoria do item de catálogo: alergia ou restrição alimentar.</summary>
public enum CategoriaRestricao
{
    Alergia,
    Restricao
}

/// <summary>Ação registrada no histórico de restrições do aluno (Spec 010, RF5).</summary>
public enum AcaoRestricao
{
    Adicionada,
    Removida
}

/// <summary>
/// Catálogo de restrições e alergias (Spec 010). O nome é validado aqui — formato, limite e
/// unicidade por categoria derivada do nome normalizado — e a desativação preserva os
/// vínculos existentes (ADR-008).
///
/// A normalização vive em <see cref="Normalizar"/> e é a única fonte para comparar nomes:
/// catálogo e aluno usam exatamente esta regra, porque SQLite não tem collation
/// acento-insensível nativa e a coluna <see cref="NomeNormalizado"/> carrega o índice único.
/// </summary>
public class RestricaoAlimentar
{
    private const int NomeMin = 2;
    private const int NomeMax = 60;

    private RestricaoAlimentar()
    {
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>Nome sem caixa e sem acento, persistido para o índice único.</summary>
    public string NomeNormalizado { get; private set; } = string.Empty;

    public CategoriaRestricao Categoria { get; private set; }

    public bool Ativa { get; private set; } = true;

    public RestricaoAlimentar(string nome, CategoriaRestricao categoria)
    {
        DefinirCategoria(categoria);
        DefinirNome(nome);
        Ativa = true;
    }

    /// <summary>
    /// Redefine o nome. A unicidade por categoria depende do índice
    /// <c>(NomeNormalizado, Categoria)</c>; o serviço checa antes para devolver mensagem.
    /// </summary>
    public void DefinirNome(string nome)
    {
        var valor = ValidacaoDeCampo.Texto(nome, "Nome da restrição", NomeMin, NomeMax);
        Nome = valor;
        NomeNormalizado = Normalizar(valor);
    }

    public void DefinirCategoria(CategoriaRestricao categoria)
    {
        if (!Enum.IsDefined(categoria))
            throw new ArgumentException("Categoria de restrição inválida.");

        Categoria = categoria;
    }

    public void Desativar() => Ativa = false;

    public void Reativar() => Ativa = true;

    /// <summary>
    /// Remove espaços das pontas, aplica caixa baixa invariante e elimina marcas de
    /// acentuação (FormD). Regra única de comparação de nomes (RF1, RF4).
    /// </summary>
    public static string Normalizar(string? valor)
    {
        var texto = (valor ?? string.Empty).Trim().ToLowerInvariant();

        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);

        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                construtor.Append(caractere);
        }

        return construtor.ToString().Normalize(NormalizationForm.FormC);
    }
}
