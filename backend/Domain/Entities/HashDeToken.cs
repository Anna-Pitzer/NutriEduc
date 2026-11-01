using System.Security.Cryptography;
using System.Text;

namespace Ntc.Domain.Entity;

/// <summary>
/// Resumo SHA-256 de um token de cadastro (Spec 024, F2).
///
/// Objeto de valor do domínio: a entidade <see cref="TokenDeCadastro"/> nunca aceita uma
/// string solta como hash, e a comparação com o valor informado acontece aqui, em tempo
/// constante. A geração do segredo continua na aplicação, porque depende do gerador
/// criptográfico da plataforma — o que pertence ao domínio é o resumo e a conferência.
/// </summary>
public readonly struct HashDeToken
{
    private readonly string? _valor;

    private HashDeToken(string valor)
    {
        _valor = valor;
    }

    /// <summary>Resume o valor informado. Valor vazio é recusado: hash de vazio não existe.</summary>
    public static HashDeToken Criar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException("Token vazio não pode ser resumido.", nameof(valor));
        }

        return new HashDeToken(Resumir(valor));
    }

    /// <summary>Confere o valor informado contra o resumo, em tempo constante.</summary>
    public bool Confere(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || _valor is null)
        {
            return false;
        }

        return SegredosConferem(Resumir(valor), _valor);
    }

    /// <summary>Compara dois segredos em claro em tempo constante (token de ambiente).</summary>
    public static bool SegredosConferem(string? informado, string? esperado)
    {
        if (string.IsNullOrWhiteSpace(informado) || string.IsNullOrWhiteSpace(esperado))
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(informado.Trim());
        var b = Encoding.UTF8.GetBytes(esperado);

        // FixedTimeEquals exige o mesmo tamanho; comparar antes não vaza o conteúdo, só o
        // comprimento.
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>
    /// Lê um resumo já persistido. Aceita o formato gravado por versões anteriores
    /// (hexadecimal de SHA-256), que continua sendo o mesmo formato atual.
    /// </summary>
    public static bool TentarLer(string? persistido, out HashDeToken hash)
    {
        if (string.IsNullOrWhiteSpace(persistido))
        {
            hash = default;
            return false;
        }

        hash = new HashDeToken(persistido.Trim());
        return true;
    }

    public override string ToString() => _valor ?? string.Empty;

    private static string Resumir(string valor)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valor.Trim()));

        return Convert.ToHexString(bytes);
    }
}
