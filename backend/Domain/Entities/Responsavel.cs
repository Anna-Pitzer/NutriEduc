namespace Ntc.Domain.Entity;

/// <summary>
/// Contato responsável pelo aluno.
///
/// Objeto de valor: não tem identidade própria — a identidade vem do
/// <see cref="Aluno"/> que o possui. O <see cref="Aluno"/> delega a este objeto as
/// invariantes de nome e telefone, de modo que não existe regra duplicada entre os dois.
///
/// O telefone é armazenado sem máscara. Ausência é representada por
/// <see cref="string.Empty"/> — nunca por <c>null</c> — para preservar o schema atual
/// das colunas.
/// </summary>
public class Responsavel
{
    private const int NomeMin = 2;
    private const int NomeMax = 100;

    public string Nome { get; private set; } = string.Empty;

    public string Telefone { get; private set; } = string.Empty;

    /// <summary>Somente para o EF Core materializar o objeto possuído. Não valida.</summary>
    private Responsavel()
    {
    }

    public Responsavel(string nome, string? telefone)
    {
        var nomeValidado = ValidarNome(nome);
        var telefoneValidado = ValidarTelefone(telefone);

        Nome = nomeValidado;
        Telefone = telefoneValidado;
    }

    public bool PossuiTelefone => Telefone.Length > 0;

    /// <summary>Atualiza o contato. Valida tudo antes de atribuir.</summary>
    public void AtualizarContato(string nome, string? telefone)
    {
        var nomeValidado = ValidarNome(nome);
        var telefoneValidado = ValidarTelefone(telefone);

        Nome = nomeValidado;
        Telefone = telefoneValidado;
    }

    private static string ValidarNome(string? nome)
    {
        var texto = (nome ?? string.Empty).Trim();

        if (texto.Length == 0)
            throw new ArgumentException("O campo Nome do responsável é obrigatório.");

        if (texto.Length < NomeMin || texto.Length > NomeMax)
            throw new ArgumentException(
                $"O campo Nome do responsável deve ter entre {NomeMin} e {NomeMax} caracteres.");

        return texto;
    }

    private static string ValidarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
            return string.Empty;

        var digitos = new string(telefone.Where(char.IsDigit).ToArray());

        if (digitos.Length != 10 && digitos.Length != 11)
            throw new ArgumentException("O campo Telefone do responsável deve ter 10 ou 11 dígitos.");

        return digitos;
    }
}
