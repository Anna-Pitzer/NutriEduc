---
name: dominio-aluno-invariantes
status: created
references: V1, ADR-004, L1
---

# Design — Invariantes do domínio Aluno

## Estrutura de validação

`backend/Domain/Entities/ValidacaoDeCampo.cs` concentra as duas regras de texto,
compartilhadas por `Aluno`, `Turma` e `Responsavel` (antes cada classe repetia o
predicado):

```csharp
internal static class ValidacaoDeCampo
{
    public static string Texto(string? valor, string campo, int min, int max);
    // trim, rejeição de vazio ("O campo X é obrigatório.") e faixa de comprimento

    public static string Telefone(string? valor, string campo);
    // ausência → string.Empty; só dígitos, 10 ou 11, sem máscara
}
```

`Aluno` mantém apenas as regras exclusivas dele: `ValidarNascimento` (1900 até hoje) e
`Observacao` (máximo 500 após trim).

## Construtor

```csharp
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
    var restricoesValidadas = NormalizarRestricoes(restricoes);   // Spec 003

    AplicarDados(dados, turma, anafilaxia, responsavel);

    foreach (var restricao in restricoesValidadas)
        _restricoes.Add(restricao);
}
```

`Turma` e `Responsavel` são objetos de valor (lição L10): cada um valida as próprias
invariantes no construtor e `Aluno` não duplica escola, série, nome ou telefone do
responsável.

## `AtualizarDados`

Mesmo corpo de validação do construtor, seguido de atribuição. Para garantir
"inválido não altera nada", a validação é extraída em um método privado que devolve
uma tupla de valores normalizados:

```csharp
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

    // 1. valida tudo antes de tocar em qualquer propriedade
    var dados = ValidarDados(nome, nascimento, telefone, observacao);

    // 2. normaliza a coleção em buffer; falha aqui não altera o estado atual
    var restricoesValidadas = NormalizarRestricoes(restricoes);

    // 3. só então atribui (ADR-004)
    AplicarDados(dados, turma, anafilaxia, responsavel);

    _restricoes.Clear();
    foreach (var restricao in restricoesValidadas)
        _restricoes.Add(restricao);
}
```

`NormalizarRestricoes` está detalhado na Spec 003: buffer local, ignora vazio e
duplicata, lança na 21ª, e só é aplicado depois de validado por inteiro.

Construtor e `AtualizarDados` compartilham `ValidarDados`, `NormalizarRestricoes` e
`AplicarDados` — uma única fonte de verdade por regra.

> **Correção obrigatória nesta revisão:** a versão anterior deste design omitia a
> sincronização de `Restricoes` em `AtualizarDados`, embora a Spec 001 passe
> `dto.Restricoes` para o método. Implementado como estava, `PUT /api/aluno/{id}`
> descartaria silenciosamente toda alteração de restrição alimentar — o pior defeito
> possível em uma aplicação de segurança alimentar. O buffer de `NormalizarRestricoes`
> garante que a coleção só é substituída depois de validada por inteiro.

> **Atualizado na auditoria de 2026-10-31:** o construtor passou a receber `Turma` e
> `Responsavel` (objetos de valor, D2) e as regras de texto/telefone foram unificadas em
> `ValidacaoDeCampo` — antes estavam repetidas em `Aluno`, `Turma` e `Responsavel`.

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
