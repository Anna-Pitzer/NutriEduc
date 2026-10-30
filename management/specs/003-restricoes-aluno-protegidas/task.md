---
name: restricoes-aluno-protegidas
status: created
references: V1, ADR-006, L2
---

# Tasks — Restrições alimentares protegidas

- [x] T1: substituir `List<string> Restricoes { get; private set; }` por campo privado
  `_restricoes` + `IReadOnlyList<string> Restricoes` em
  `backend/Domain/Entities/Aluno.cs`.
  - Validação: `dotnet build backend/backend.csproj`.
- [x] T2: implementar `AdicionarRestricao` e `RemoverRestricao` com as invariantes
  (trim, vazio, duplicata case-insensitive, limite de 20).
  - Validação: testes unitários de todos os casos.
- [x] T3: ajustar o construtor para receber `IEnumerable<string>?` e delegar a
  `AdicionarRestricao`, ignorando itens inválidos.
  - Validação: teste com lista contendo item vazio não lança.
- [x] T4: configurar o mapeamento com backing field em
  `backend/Infrastructure/DataBase/ConnectionContext.cs`.
  - Validação: cadastro e leitura de aluno com 3 restrições preservam os valores.
- [x] T5: ajustar `AlunoService` e `DadosAlunoDTO` para o novo tipo
  (`Restricoes.ToList()` na saída).
  - Validação: `GET /api/aluno` devolve `restricoes` como array JSON.
- [x] T6: criar migration apenas se o mapeamento alterar o schema; caso contrário,
  registrar em comentário que não houve mudança.
  - Validação: `dotnet ef migrations list` sem migration pendente inesperada.
- [x] T7: executar gates (`dotnet build`, `dotnet test`).

## Definição de pronto

- Coleção imutável do ponto de vista externo.
- Contrato JSON inalterado.
- Testes cobrindo as seis linhas da tabela de `design.md`.

## Registro de execução

Verificado contra o código em 2026-10-30. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `_restricoes` + `public IReadOnlyList<string> Restricoes => _restricoes.AsReadOnly()` (`Aluno.cs:32,51`) |
| T2 | `AdicionarRestricao`/`RemoverRestricao` (`Aluno.cs:115-143`); testes `AlunoTests:197-252` |
| T3 | construtor aceita `IEnumerable<string>?` (`Aluno.cs:69`) — ver D1 |
| T4 | `PrimitiveCollection(...).HasField("_restricoes").UsePropertyAccessMode(Field)` (`ConnectionContext.cs:27-30`) — ver D2 |
| T5 | `aluno.Restricoes.ToList()` na saída (`AlunoService.cs:80`); `DadosAlunoDTO.Restricoes` continua `List<string>` |
| T6 | nenhuma migration nova: a coluna `Restricoes` já existia em `20260930171058_AdicionarRestricoesAluno` |
| T7 | `dotnet test`: 152 aprovados, 0 falhas (2026-10-30) |

**Desvios registrados**

- D1: o construtor não chama `AdicionarRestricao` item a item. Usa `NormalizarRestricoes`
  (`Aluno.cs:213-238`), que valida em buffer e só então aplica — mesma semântica
  (ignora vazio e duplicata, lança na 21ª) com atomicidade: nada é atribuído se falhar.
- D2: a validação do T4 foi feita pelo teste que constrói o modelo do EF
  (`ConnectionContextTests`), não por cadastro e leitura reais. Persistência em SQLite
  nunca foi exercitada (PEND-018).
- `dotnet ef migrations list` não foi executado: `dotnet-ef` não está instalado (PEND-020).
