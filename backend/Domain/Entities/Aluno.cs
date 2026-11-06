using System.Globalization;
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
    private const string CampoRestricoes = "Restrições";
    private const int MaxObservacao = 500;
    private const int NomeMin = 2;
    private const int NomeMax = 100;

    private readonly List<string> _restricoes = new();
    private readonly List<HistoricoAluno> _historico = new();

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

    /// <summary>
    /// Trilha append-only de alterações do aluno (Spec 023, ADR-016): criação, campos
    /// alterados e restrições, sempre com autor e data, sem edição nem exclusão. A escrita
    /// só acontece por <see cref="RegistrarCriacao"/> e <see cref="RegistrarHistorico"/>.
    /// </summary>
    public IReadOnlyList<HistoricoAluno> Historico => _historico.AsReadOnly();

    public bool Anafilaxia { get; private set; }

    /// <summary>Preenchido pelo EF Core ao materializar; nunca nulo em objeto válido.</summary>
    public Responsavel Responsavel { get; private set; } = null!;

    /// <summary>
    /// Unidade institucional do aluno (Spec 009). Nulo em registros legados ou ainda
    /// não vinculados: nesse caso apenas Nutricionista e AdminSecretaria enxergam o
    /// aluno. `Turma.Escola` continua sendo apenas rótulo de exibição.
    /// </summary>
    public int? UnidadeId { get; private set; }

    /// <summary>
    /// Escola do aluno (Spec 027): fonte de verdade do vínculo institucional. Nulo
    /// apenas em registro legado; o construtor público nunca produz esse estado.
    /// </summary>
    public int? EscolaId { get; private set; }

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
        Responsavel responsavel,
        int escolaId)
    {
        ArgumentNullException.ThrowIfNull(turma);
        ArgumentNullException.ThrowIfNull(responsavel);

        var dados = ValidarDados(nome, nascimento, telefone, observacao);
        ValidarEscola(escolaId);

        // Buffer validado: nada é atribuído se alguma restrição violar o limite.
        var restricoesValidadas = NormalizarRestricoes(restricoes);

        AplicarDados(dados, turma, anafilaxia, responsavel, escolaId);

        foreach (var restricao in restricoesValidadas)
            _restricoes.Add(restricao);
    }

    /// <summary>
    /// Aplica as mesmas invariantes do construtor. Valida tudo — inclusive a coleção de
    /// restrições e a unidade — antes de tocar em qualquer propriedade.
    ///
    /// Devolve os três diffs calculados antes de aplicar (Spec 023, RF3): campos alterados
    /// com valor anterior e novo, restrições adicionadas e restrições removidas. Campo ou
    /// conjunto inalterado devolve lista vazia e não gera evento.
    /// </summary>
    public (
        IReadOnlyList<AlteracaoDeDado> Alteracoes,
        IReadOnlyList<string> Adicionadas,
        IReadOnlyList<string> Removidas) AtualizarDados(
        string nome,
        DateOnly nascimento,
        Turma turma,
        string? telefone,
        string? observacao,
        IEnumerable<string>? restricoes,
        bool anafilaxia,
        Responsavel responsavel,
        int? unidadeId,
        int escolaId)
    {
        ArgumentNullException.ThrowIfNull(turma);
        ArgumentNullException.ThrowIfNull(responsavel);

        var dados = ValidarDados(nome, nascimento, telefone, observacao);

        var restricoesValidadas = NormalizarRestricoes(restricoes);

        ValidarUnidade(unidadeId);
        ValidarEscola(escolaId);

        var adicionadas = restricoesValidadas
            .Where(nova => !ContemRestricao(_restricoes, nova))
            .ToList();

        var removidas = _restricoes
            .Where(atual => !ContemRestricao(restricoesValidadas, atual))
            .ToList();

        var alteracoes = new List<AlteracaoDeDado>();

        Comparar(alteracoes, "Nome", Nome, dados.Nome);
        Comparar(alteracoes, "Nascimento", TextoData(Nascimento), TextoData(dados.Nascimento));
        Comparar(alteracoes, "Escola", Turma.Escola, turma.Escola);
        Comparar(alteracoes, "Série", Turma.Serie, turma.Serie);
        Comparar(alteracoes, "Telefone", Telefone, dados.Telefone);
        Comparar(alteracoes, "Observação", Observacao, dados.Observacao);
        Comparar(alteracoes, "Responsável", Responsavel.Nome, responsavel.Nome);
        Comparar(alteracoes, "Telefone do responsável", Responsavel.Telefone, responsavel.Telefone);
        Comparar(alteracoes, "Anafilaxia", TextoBooleano(Anafilaxia), TextoBooleano(anafilaxia));
        Comparar(alteracoes, "Unidade", TextoUnidade(UnidadeId), TextoUnidade(unidadeId));
        Comparar(alteracoes, "Escola vinculada", TextoEscola(EscolaId), TextoEscola(escolaId));

        AplicarDados(dados, turma, anafilaxia, responsavel, escolaId);
        UnidadeId = unidadeId;

        _restricoes.Clear();
        _restricoes.AddRange(restricoesValidadas);

        return (alteracoes, adicionadas, removidas);
    }

    /// <summary>Evento único de criação do cadastro (Spec 023, RF2).</summary>
    public void RegistrarCriacao(string? autorNome)
    {
        _historico.Add(new HistoricoAluno(
            Id, TipoEventoAluno.Criado, null, null, Nome, autorNome, DateTime.UtcNow));
    }

    /// <summary>
    /// Registra um evento por campo alterado e um por restrição adicionada ou removida
    /// (Spec 023, RF3 e RF4). Chamado pelo serviço antes de persistir: como
    /// <see cref="Historico"/> é a navegação do agregado, aluno e eventos são gravados na
    /// mesma unidade de persistência (RNF2).
    /// </summary>
    public void RegistrarHistorico(
        IEnumerable<AlteracaoDeDado> alteracoes,
        IEnumerable<string> adicionadas,
        IEnumerable<string> removidas,
        string? autorNome,
        string? origem = null)
    {
        var agora = DateTime.UtcNow;

        foreach (var alteracao in alteracoes ?? Enumerable.Empty<AlteracaoDeDado>())
        {
            _historico.Add(new HistoricoAluno(
                Id, TipoEventoAluno.Alterado, alteracao.Campo,
                alteracao.ValorAnterior, alteracao.ValorNovo, autorNome, agora, origem));
        }

        foreach (var nome in adicionadas ?? Enumerable.Empty<string>())
        {
            _historico.Add(new HistoricoAluno(
                Id, TipoEventoAluno.RestricaoAdicionada, CampoRestricoes,
                string.Empty, nome, autorNome, agora, origem));
        }

        foreach (var nome in removidas ?? Enumerable.Empty<string>())
        {
            _historico.Add(new HistoricoAluno(
                Id, TipoEventoAluno.RestricaoRemovida, CampoRestricoes,
                nome, string.Empty, autorNome, agora, origem));
        }
    }

    public void AdicionarRestricao(string restricao)
    {
        var valor = NormalizarRestricao(restricao)
            ?? throw new ArgumentException("A restrição não pode ser vazia.");

        if (ContemRestricao(_restricoes, valor))
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

    /// <summary>
    /// Vincula o aluno a uma unidade institucional (Spec 009). A existência e a
    /// atividade da unidade são responsabilidade do serviço; aqui só se registra o id.
    /// </summary>
    public void DefinirUnidade(int? unidadeId)
    {
        ValidarUnidade(unidadeId);
        UnidadeId = unidadeId;
    }

    /// <summary>
    /// Move o aluno para outra escola da mesma unidade (Spec 027, RF11). A unidade de
    /// destino é derivada pelo serviço; a comparação de unidade é feita antes da mutação.
    /// </summary>
    public void TransferirEscola(int escolaId, int unidadeId, bool mesmaUnidade)
    {
        ValidarEscola(escolaId);

        if (unidadeId <= 0)
        {
            throw new ArgumentException("Unidade inválida.");
        }

        if (!mesmaUnidade)
        {
            throw new ArgumentException(
                "Transferência entre unidades é proibida nesta spec.");
        }

        EscolaId = escolaId;
        UnidadeId = unidadeId;
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
        return ValidacaoDeCampo.Texto(nome, "Nome", NomeMin, NomeMax);
    }

    private static string ValidarTelefone(string? valor)
    {
        return ValidacaoDeCampo.Telefone(valor, "Telefone");
    }

    /// <summary>
    /// Regra única de unidade: identificadores não positivos são inválidos. Vale tanto na
    /// criação (<see cref="DefinirUnidade"/>) quanto na atualização.
    /// </summary>
    private static void ValidarUnidade(int? unidadeId)
    {
        if (unidadeId is <= 0)
            throw new ArgumentException("Unidade inválida.");
    }

    private static void ValidarEscola(int escolaId)
    {
        if (escolaId <= 0)
            throw new ArgumentException("Escola inválida.");
    }

    private static string TextoEscola(int? escolaId) =>
        escolaId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Compara o valor atual com o validado e registra a alteração quando diferem. A
    /// comparação é textual e ordinal sobre a representação canônica do campo, para que
    /// campos inalterados nunca gerem evento (Spec 023, RF3).
    /// </summary>
    private static void Comparar(
        ICollection<AlteracaoDeDado> alteracoes,
        string campo,
        string valorAnterior,
        string valorNovo)
    {
        if (!string.Equals(valorAnterior, valorNovo, StringComparison.Ordinal))
            alteracoes.Add(new AlteracaoDeDado(campo, valorAnterior, valorNovo));
    }

    private static string TextoData(DateOnly data) =>
        data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string TextoBooleano(bool valor) => valor ? "Sim" : "Não";

    private static string TextoUnidade(int? unidadeId) =>
        unidadeId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static DateOnly ValidarNascimento(DateOnly valor)
    {
        if (valor < new DateOnly(1900, 1, 1))
            throw new ArgumentException("A data de nascimento não pode ser anterior a 1900.");

        if (valor > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("A data de nascimento não pode estar no futuro.");

        return valor;
    }

    /// <summary>
    /// Regra única de normalização de item (trim e rejeição de vazio), usada por
    /// <see cref="AdicionarRestricao"/> e pelo buffer do construtor.
    /// </summary>
    private static string? NormalizarRestricao(string? restricao)
    {
        var valor = (restricao ?? string.Empty).Trim();
        return valor.Length == 0 ? null : valor;
    }

    /// <summary>Comparação case-insensitive — regra única de duplicidade (RF2/RF5).</summary>
    private static bool ContemRestricao(IReadOnlyCollection<string> restricoes, string valor)
    {
        return restricoes.Any(r => string.Equals(r, valor, StringComparison.OrdinalIgnoreCase));
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
            var valor = NormalizarRestricao(item);

            if (valor is null || ContemRestricao(buffer, valor))
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
        Responsavel responsavel,
        int escolaId)
    {
        Nome = dados.Nome;
        Nascimento = dados.Nascimento;
        Turma = turma;
        Telefone = dados.Telefone;
        Observacao = dados.Observacao;
        Anafilaxia = anafilaxia;
        Responsavel = responsavel;
        EscolaId = escolaId;
    }
}
