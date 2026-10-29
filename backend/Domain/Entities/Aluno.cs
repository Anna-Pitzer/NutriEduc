using System.Linq;

namespace Ntc.Domain.Entity;

/// <summary>
/// Aluno da rede escolar.
///
/// Composição: o aluno não guarda texto solto de escola, série ou contato — ele possui
/// uma <see cref="Turma"/> e um <see cref="Responsavel"/> e delega a eles as próprias
/// invariantes. Nenhuma regra de escola, série ou contato é duplicada aqui.
///
/// Invariantes: todo o estado é validado antes de qualquer atribuição — um payload
/// inválido não deixa o objeto parcialmente alterado. Construtor e
/// <see cref="AtualizarDados"/> compartilham <c>ValidarDados</c>, de modo que existe uma
/// única fonte de verdade por regra.
///
/// Coleção de restrições: exposta como <see cref="IReadOnlyList{T}"/> sobre um campo
/// privado. A escrita só acontece por <see cref="AdicionarRestricao"/> e
/// <see cref="RemoverRestricao"/>.
///
/// Assimetria intencional: o construtor privado sem parâmetros existe para o EF Core
/// materializar registros já gravados e NÃO valida. Registros legados inválidos continuam
/// legíveis; a validação ocorre apenas em criação e atualização.
/// </summary>
public class Aluno
{
    private const int MaxRestricoes = 20;
    private const int MaxObservacao = 500;
    private const int NomeMin = 2;
    private const int NomeMax = 100;

    private readonly List<string> _restricoes = new();

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public DateOnly Nascimento { get; private set; }

    /// <summary>Preenchida pelo EF Core ao materializar; nunca nula em objeto válido.</summary>
    public Turma Turma { get; private set; } = null!;

    /// <summary>
    /// Armazenado sem máscara. Ausência é representada por <see cref="string.Empty"/> —
    /// não por <c>null</c> — para preservar o schema atual das colunas.
    /// </summary>
    public string Telefone { get; private set; } = string.Empty;

    public string Observacao { get; private set; } = string.Empty;

    public IReadOnlyList<string> Restricoes => _restricoes.AsReadOnly();

    public bool Anafilaxia { get; private set; }

    /// <summary>Preenchido pelo EF Core ao materializar; nunca nulo em objeto válido.</summary>
    public Responsavel Responsavel { get; private set; } = null!;

    /// <summary>Somente para o EF Core. Não valida e não deve ser usado pela aplicação.</summary>
    private Aluno()
    {
    }

    public Aluno(
        string nome,
        DateOnly nascimento,
        Turma turma,
        string? telefone,
        string? observacao,
        IEnumerable<string>? restricoes,
        bool anafilaxia,
        Responsavel responsavel)
    {
        ArgumentNullException.ThrowIfNull(turma);
        ArgumentNullException.ThrowIfNull(responsavel);

        var dados = ValidarDados(nome, nascimento, telefone, observacao);

        // Buffer validado: nada é atribuído se alguma restrição violar o limite.
        var restricoesValidadas = NormalizarRestricoes(restricoes);

        AplicarDados(dados, turma, anafilaxia, responsavel);

        foreach (var restricao in restricoesValidadas)
            _restricoes.Add(restricao);
    }

    /// <summary>
    /// Aplica as mesmas invariantes do construtor. Valida tudo — inclusive a coleção de
    /// restrições — antes de tocar em qualquer propriedade.
    /// </summary>
    public void AtualizarDados(
        string nome,
        DateOnly nascimento,
        Turma turma,
        string? telefone,
        string? observacao,
        IEnumerable<string>? restricoes,
        bool anafilaxia,
        Responsavel responsavel)
    {
        ArgumentNullException.ThrowIfNull(turma);
        ArgumentNullException.ThrowIfNull(responsavel);

        var dados = ValidarDados(nome, nascimento, telefone, observacao);

        var restricoesValidadas = NormalizarRestricoes(restricoes);

        AplicarDados(dados, turma, anafilaxia, responsavel);

        _restricoes.Clear();
        foreach (var restricao in restricoesValidadas)
            _restricoes.Add(restricao);
    }

    public void AdicionarRestricao(string restricao)
    {
        var valor = (restricao ?? string.Empty).Trim();

        if (valor.Length == 0)
            throw new ArgumentException("A restrição não pode ser vazia.");

        if (_restricoes.Any(r => string.Equals(r, valor, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Restrição já cadastrada para este aluno.");

        if (_restricoes.Count >= MaxRestricoes)
            throw new ArgumentException(
                $"O aluno já possui {MaxRestricoes} restrições cadastradas.");

        _restricoes.Add(valor);
    }

    public bool RemoverRestricao(string restricao)
    {
        var alvo = (restricao ?? string.Empty).Trim();
        var indice = _restricoes.FindIndex(r =>
            string.Equals(r, alvo, StringComparison.OrdinalIgnoreCase));

        if (indice < 0)
            return false;

        _restricoes.RemoveAt(indice);
        return true;
    }

    private readonly record struct DadosValidados(
        string Nome,
        DateOnly Nascimento,
        string Telefone,
        string Observacao);

    private static DadosValidados ValidarDados(
        string? nome,
        DateOnly nascimento,
        string? telefone,
        string? observacao)
    {
        var observacaoValidada = (observacao ?? string.Empty).Trim();

        if (observacaoValidada.Length > MaxObservacao)
            throw new ArgumentException(
                $"A observação deve ter no máximo {MaxObservacao} caracteres.");

        return new DadosValidados(
            ValidarNome(nome),
            ValidarNascimento(nascimento),
            ValidarTelefone(telefone),
            observacaoValidada);
    }

    private static string ValidarNome(string? nome)
    {
        var texto = (nome ?? string.Empty).Trim();

        if (texto.Length == 0)
            throw new ArgumentException("O campo Nome é obrigatório.");

        if (texto.Length < NomeMin || texto.Length > NomeMax)
            throw new ArgumentException(
                $"O campo Nome deve ter entre {NomeMin} e {NomeMax} caracteres.");

        return texto;
    }

    private static string ValidarTelefone(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var digitos = new string(valor.Where(char.IsDigit).ToArray());

        if (digitos.Length != 10 && digitos.Length != 11)
            throw new ArgumentException("O campo Telefone deve ter 10 ou 11 dígitos.");

        return digitos;
    }

    private static DateOnly ValidarNascimento(DateOnly valor)
    {
        if (valor < new DateOnly(1900, 1, 1))
            throw new ArgumentException("A data de nascimento não pode ser anterior a 1900.");

        if (valor > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("A data de nascimento não pode estar no futuro.");

        return valor;
    }

    /// <summary>
    /// Normaliza a coleção recebida em um buffer local, sem tocar no estado atual.
    /// Itens vazios e duplicatas são ignorados (tolerância na construção — Spec 003 RF5);
    /// exceder o limite lança, para que o chamador jamais perca dados silenciosamente.
    /// </summary>
    private static List<string> NormalizarRestricoes(IEnumerable<string>? restricoes)
    {
        var buffer = new List<string>();

        if (restricoes == null)
            return buffer;

        foreach (var item in restricoes)
        {
            var valor = (item ?? string.Empty).Trim();

            if (valor.Length == 0)
                continue;

            if (buffer.Any(r => string.Equals(r, valor, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (buffer.Count >= MaxRestricoes)
                throw new ArgumentException(
                    $"O aluno já possui {MaxRestricoes} restrições cadastradas.");

            buffer.Add(valor);
        }

        return buffer;
    }

    private void AplicarDados(
        DadosValidados dados,
        Turma turma,
        bool anafilaxia,
        Responsavel responsavel)
    {
        Nome = dados.Nome;
        Nascimento = dados.Nascimento;
        Turma = turma;
        Telefone = dados.Telefone;
        Observacao = dados.Observacao;
        Anafilaxia = anafilaxia;
        Responsavel = responsavel;
    }
}
