namespace Ntc.Domain.Entity;

/// <summary>
/// Invariantes de texto compartilhadas pelas entidades do domínio.
///
/// Uma única implementação por regra (Spec 004, RNF1): <see cref="Aluno"/>,
/// <see cref="Turma"/> e <see cref="Responsavel"/> chamam estes predicados em vez de
/// repetir trim, faixa de comprimento e validação de telefone. Mudar a regra de texto
/// ou de telefone exige tocar apenas este arquivo.
///
/// <c>internal</c> de propósito: é detalhe de implementação do domínio, não contrato
/// público consumido por controllers ou serviços.
/// </summary>
internal static class ValidacaoDeCampo
{
    /// <summary>
    /// Remove espaços das pontas, rejeita vazio e exige comprimento entre
    /// <paramref name="min"/> e <paramref name="max"/>, com mensagem que nomeia o campo
    /// (RNF1).
    /// </summary>
    public static string Texto(string? valor, string campo, int min, int max)
    {
        var texto = (valor ?? string.Empty).Trim();

        if (texto.Length == 0)
            throw new ArgumentException($"O campo {campo} é obrigatório.");

        if (texto.Length < min || texto.Length > max)
            throw new ArgumentException(
                $"O campo {campo} deve ter entre {min} e {max} caracteres.");

        return texto;
    }

    /// <summary>
    /// Telefone opcional: ausência vira <see cref="string.Empty"/>; quando informado,
    /// aceita apenas dígitos e exige 10 ou 11, devolvendo sempre sem máscara.
    /// </summary>
    public static string Telefone(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var digitos = new string(valor.Where(char.IsDigit).ToArray());

        if (digitos.Length != 10 && digitos.Length != 11)
            throw new ArgumentException($"O campo {campo} deve ter 10 ou 11 dígitos.");

        return digitos;
    }

    /// <summary>
    /// Medida nutricional por 100 g (Spec 011, RNF2): não aceita valor negativo nem mais
    /// de duas casas decimais, e nunca arredonda em silêncio. Uma única regra para as
    /// seis medidas — repetir o <c>if</c> em cada uma seria a mesma regra escrita seis
    /// vezes.
    /// </summary>
    public static decimal Medida(decimal valor, string campo)
    {
        if (valor < 0)
            throw new ArgumentException($"O campo {campo} não pode ser negativo.");

        if (decimal.Round(valor, 2) != valor)
            throw new ArgumentException(
                $"O campo {campo} aceita no máximo duas casas decimais.");

        return valor;
    }
}
