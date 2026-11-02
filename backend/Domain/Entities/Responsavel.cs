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
        return ValidacaoDeCampo.Texto(nome, "Nome do responsável", NomeMin, NomeMax);
    }

    private static string ValidarTelefone(string? telefone)
    {
        return ValidacaoDeCampo.Telefone(telefone, "Telefone do responsável");
    }
}
