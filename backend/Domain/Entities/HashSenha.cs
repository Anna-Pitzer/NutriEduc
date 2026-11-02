using System.Security.Cryptography;
using System.Text;

namespace Ntc.Domain.Entity;

/// <summary>
/// Hash de senha versionado no formato <c>pbkdf2-sha256$&lt;iteracoes&gt;$&lt;salt&gt;$&lt;derivado&gt;</c>,
/// com salt e derivado em base64.
///
/// Tipo de valor: <see cref="Confere"/> não altera estado e duas instâncias com o mesmo
/// conteúdo são equivalentes. O salt aleatório por usuário garante que a mesma senha
/// produza hashes diferentes (RF2/RF4 da Spec 002).
///
/// O prefixo versiona o formato: uma migração futura de algoritmo cria um novo prefixo
/// sem invalidar os hashes já gravados (RNF3).
/// </summary>
public readonly struct HashSenha
{
    private const string Prefixo = "pbkdf2-sha256";
    private const int IteracoesPadrao = 100_000;
    private const int TamanhoSalt = 16;
    private const int TamanhoDerivado = 32;

    private readonly int _iteracoes;
    private readonly byte[]? _salt;
    private readonly byte[]? _derivado;

    private HashSenha(int iteracoes, byte[] salt, byte[] derivado)
    {
        _iteracoes = iteracoes;
        _salt = salt;
        _derivado = derivado;
    }

    /// <summary>Deriva um hash novo com salt aleatório. Não valida o comprimento da senha.</summary>
    public static HashSenha Criar(string senha)
    {
        if (string.IsNullOrEmpty(senha))
            throw new ArgumentException("A senha não pode ser vazia.", nameof(senha));

        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var derivado = Derivar(senha, salt, IteracoesPadrao);

        return new HashSenha(IteracoesPadrao, salt, derivado);
    }

    /// <summary>
    /// Compara em tempo constante. Devolve <c>false</c> — nunca lança — para senha vazia
    /// ou quando a instância não foi criada por <see cref="Criar"/> nem por <see cref="TentarLer"/>.
    /// </summary>
    public bool Confere(string? senha)
    {
        if (string.IsNullOrEmpty(senha) || _salt is null || _derivado is null)
            return false;

        var derivado = Derivar(senha, _salt, _iteracoes);

        return CryptographicOperations.FixedTimeEquals(derivado, _derivado);
    }

    /// <summary>
    /// Lê um valor persistido. Devolve <c>false</c> — nunca lança — para valor nulo, vazio,
    /// de outro formato ou truncado; nesse caso o registro é tratado como legado.
    /// </summary>
    public static bool TentarLer(string? valor, out HashSenha hash)
    {
        hash = default;

        if (string.IsNullOrEmpty(valor))
            return false;

        var partes = valor.Split('$');

        if (partes.Length != 4)
            return false;

        if (!string.Equals(partes[0], Prefixo, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes <= 0)
            return false;

        byte[] salt;
        byte[] derivado;

        try
        {
            salt = Convert.FromBase64String(partes[2]);
            derivado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || derivado.Length == 0)
            return false;

        hash = new HashSenha(iteracoes, salt, derivado);
        return true;
    }

    /// <summary>Serializa para persistência. Nunca chamado em instância não inicializada.</summary>
    public override string ToString()
    {
        if (_salt is null || _derivado is null)
            throw new InvalidOperationException("Hash de senha não inicializado.");

        return string.Join(
            '$',
            Prefixo,
            _iteracoes.ToString(),
            Convert.ToBase64String(_salt),
            Convert.ToBase64String(_derivado));
    }

    private static byte[] Derivar(string senha, byte[] salt, int iteracoes)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(senha),
            salt,
            iteracoes,
            HashAlgorithmName.SHA256,
            TamanhoDerivado);
    }
}
