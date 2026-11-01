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

Verificado contra o código em 2026-10-30 e reauditado em 2026-10-31. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `_restricoes` + `public IReadOnlyList<string> Restricoes => _restricoes.AsReadOnly()` (`backend/Domain/Entities/Aluno.cs`); teste `Restricoes_NaoExpoeListaMutavel` |
| T2 | `AdicionarRestricao`/`RemoverRestricao` (`Aluno.cs`); testes de normalização, duplicata, vazio e limite em `AlunoTests` |
| T3 | construtor aceita `IEnumerable<string>?` e usa `NormalizarRestricoes` (`Aluno.cs`) — ver D1 |
| T4 | `PrimitiveCollection(...).HasField("_restricoes").UsePropertyAccessMode(Field)` (`backend/Infrastructure/DataBase/ConnectionContext.cs`); `ConnectionContextTests` constrói o modelo e `Post_ComTresRestricoes_GetDevolveOsValoresPersistidos` grava e relê via SQLite em memória — ver D2 |
| T5 | `aluno.Restricoes.ToList()` na saída (`backend/API/Services/AlunoService.cs`); `DadosAlunoDTO.Restricoes` continua `List<string>` |
| T6 | nenhuma migration nova: a coluna `Restricoes` já existia em `20260930171058_AdicionarRestricoesAluno` |
| T7 | 2026-10-31 (auditoria): `dotnet test` 247/247, 0 falhas |

**Desvios registrados**

- D1: o construtor não chama `AdicionarRestricao` item a item. Usa `NormalizarRestricoes`,
  que valida em buffer e só então aplica — mesma semântica (ignora vazio e duplicata,
  lança na 21ª) com atomicidade: nada é atribuído se falhar. As regras de trim e de
  duplicidade são compartilhadas com `AdicionarRestricao` (`NormalizarRestricao` e
  `ContemRestricao`).
- D2 (atualizado na auditoria de 2026-10-31): o T4 é validado em duas camadas —
  `ConnectionContextTests` constrói o modelo do EF e
  `AlunoApiTests.Post_ComTresRestricoes_GetDevolveOsValoresPersistidos` grava e relê via
  SQLite em memória. O arquivo `database.db` real continua não exercitado (PEND-018).
- `dotnet ef migrations list` não foi executado: `dotnet-ef` não estava instalado à época
  (PEND-020, concluído depois com EF Tools 10.0.12); nenhuma migration nova era necessária.
