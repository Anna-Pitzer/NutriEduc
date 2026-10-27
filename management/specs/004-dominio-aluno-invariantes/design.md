---
name: dominio-aluno-invariantes
status: created
references: V1, ADR-004, L1
---

# Design — Invariantes do domínio Aluno

## Estrutura de validação

Um único predicado de texto evita repetição de `if` por campo:

```csharp
private static string ValidarTexto(string? valor, string campo, int min, int max)
{
    var texto = (valor ?? string.Empty).Trim();
    if (texto.Length < min || texto.Length > max)
        throw new ArgumentException(
            $"{campo} deve ter entre {min} e {max} caracteres.");
    return texto;
}

private static string? ValidarTelefone(string? valor, string campo)
{
    if (string.IsNullOrWhiteSpace(valor)) return null;

    var digitos = new string(valor.Where(char.IsDigit).ToArray());
    if (digitos.Length != 10 && digitos.Length != 11)
        throw new ArgumentException(
            $"{campo} deve ter 10 ou 11 dígitos.");

    return digitos;
}

private static DateOnly ValidarNascimento(DateOnly valor)
{
    if (valor < new DateOnly(1900, 1, 1) || valor > DateOnly.FromDateTime(DateTime.Today))
        throw new ArgumentException("A data de nascimento é inválida.");
    return valor;
}
```

## Construtor

```csharp
public Aluno(string nome, DateOnly nascimento, string escola, string serie,
    string? telefone, string? observacao, IEnumerable<string>? restricoes,
    bool anafilaxia, string nomeResponsavel, string? telefoneResponsavel)
{
    var nomeVal = ValidarTexto(nome, "Nome", 2, 100);
    var escolaVal = ValidarTexto(escola, "Escola", 2, 150);
    var serieVal = ValidarTexto(serie, "Série", 2, 50);
    var responsavelVal = ValidarTexto(nomeResponsavel, "Nome do responsável", 2, 100);
    var nascimentoVal = ValidarNascimento(nascimento);
    var telefoneVal = ValidarTelefone(telefone, "Telefone");
    var telefoneRespVal = ValidarTelefone(telefoneResponsavel, "Telefone do responsável");
    var observacaoVal = (observacao ?? string.Empty).Trim();
    if (observacaoVal.Length > 500)
        throw new ArgumentException("A observação deve ter no máximo 500 caracteres.");

    // Atribuição somente após todas as validações (ADR-004)
    Nome = nomeVal;
    Nascimento = nascimentoVal;
    Escola = escolaVal;
    Serie = serieVal;
    Telefone = telefoneVal;
    Observacao = observacaoVal;
    Anafilaxia = anafilaxia;
    NomeResponsavel = responsavelVal;
    TelefoneResponsavel = telefoneRespVal;

    if (restricoes != null)
        foreach (var restricao in restricoes)
            AdicionarRestricao(restricao);   // Spec 003
}
```

## `AtualizarDados`

Mesmo corpo de validação do construtor, seguido de atribuição. Para garantir
"inválido não altera nada", a validação é extraída em um método privado que devolve
uma tupla de valores normalizados:

```csharp
private static (string Nome, DateOnly Nascimento, string Escola, string Serie,
    string? Telefone, string Observacao, string NomeResponsavel, string? TelefoneResponsavel)
    ValidarDados(...) { /* regras acima */ }

// Valida as restrições em buffer local, sem tocar no estado: se algum item violar
// a Spec 003, a coleção atual permanece intacta.
private static List<string> NormalizarRestricoes(IEnumerable<string>? restricoes)
{
    var buffer = new List<string>();
    if (restricoes == null) return buffer;

    foreach (var item in restricoes)
    {
        var valor = (item ?? string.Empty).Trim();
        if (valor.Length == 0) continue;                          // tolerância do construtor
        if (buffer.Any(r => string.Equals(r, valor, StringComparison.OrdinalIgnoreCase)))
            continue;                                             // sem duplicata (Spec 003)
        if (buffer.Count >= MaxRestricoes)                        // Spec 003
            throw new ArgumentException(
                $"O aluno já possui {MaxRestricoes} restrições cadastradas.");
        buffer.Add(valor);
    }

    return buffer;
}

public void AtualizarDados(string nome, DateOnly nascimento, string escola, string serie,
    string? telefone, string? observacao, IEnumerable<string>? restricoes,
    bool anafilaxia, string nomeResponsavel, string? telefoneResponsavel)
{
    // 1. valida tudo antes de tocar em qualquer propriedade
    var v = ValidarDados(nome, nascimento, escola, serie, telefone, observacao,
        nomeResponsavel, telefoneResponsavel);

    // 2. normaliza a coleção em buffer; falha aqui não altera o estado atual
    var restricoesValidadas = NormalizarRestricoes(restricoes);

    // 3. só então atribui (ADR-004)
    Nome = v.Nome; Nascimento = v.Nascimento; Escola = v.Escola; Serie = v.Serie;
    Telefone = v.Telefone; Observacao = v.Observacao;
    NomeResponsavel = v.NomeResponsavel; TelefoneResponsavel = v.TelefoneResponsavel;
    Anafilaxia = anafilaxia;

    _restricoes.Clear();                       // Spec 003
    foreach (var restricao in restricoesValidadas)
        AdicionarRestricao(restricao);
}
```

Construtor e `AtualizarDados` compartilham `ValidarDados` — uma única fonte de verdade
por regra.

> **Correção obrigatória nesta revisão:** a versão anterior deste design omitia a
> sincronização de `Restricoes` em `AtualizarDados`, embora a Spec 001 passe
> `dto.Restricoes` para o método. Implementado como estava, `PUT /api/aluno/{id}`
> descartaria silenciosamente toda alteração de restrição alimentar — o pior defeito
> possível em uma aplicação de segurança alimentar. O buffer de `NormalizarRestricoes`
> garante que a coleção só é substituída depois de validada por inteiro.

## Impacto em tipos

`DadosAlunoDTO` usa `string` não anulável com `= ""`. Nenhuma mudança de contrato;
apenas `AlunoService` passa a receber `string?` nas propriedades opcionais
(`Telefone`, `Observacao`, `TelefoneResponsavel`).

## Testes obrigatórios (Spec 005)

Tabela de casos: cada campo obrigatório vazio, nome com 1 e 101 caracteres, escola
com 151, série com 51, nascimento futuro, nascimento em 1899, telefone com 9 e 12
dígitos, telefone com máscara, observação com 501 caracteres, `AtualizarDados`
inválido preservando estado, e `AtualizarDados` que substitui a coleção de restrições
e que preserva a coleção anterior quando a validação falha.

## Riscos

- Registros legados com dados inválidos passam a falhar ao serem reidratados? Não:
  o EF Core usa o construtor privado sem parâmetros, portanto a validação não roda na
  leitura. Documentar essa assimetria em comentário.
