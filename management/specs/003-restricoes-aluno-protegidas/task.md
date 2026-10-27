---
name: restricoes-aluno-protegidas
status: created
references: V1, ADR-006, L2
---

# Tasks — Restrições alimentares protegidas

- [ ] T1: substituir `List<string> Restricoes { get; private set; }` por campo privado
  `_restricoes` + `IReadOnlyList<string> Restricoes` em
  `backend/Domain/Entities/Aluno.cs`.
  - Validação: `dotnet build backend/backend.csproj`.
- [ ] T2: implementar `AdicionarRestricao` e `RemoverRestricao` com as invariantes
  (trim, vazio, duplicata case-insensitive, limite de 20).
  - Validação: testes unitários de todos os casos.
- [ ] T3: ajustar o construtor para receber `IEnumerable<string>?` e delegar a
  `AdicionarRestricao`, ignorando itens inválidos.
  - Validação: teste com lista contendo item vazio não lança.
- [ ] T4: configurar o mapeamento com backing field em
  `backend/Infrastructure/DataBase/ConnectionContext.cs`.
  - Validação: cadastro e leitura de aluno com 3 restrições preservam os valores.
- [ ] T5: ajustar `AlunoService` e `DadosAlunoDTO` para o novo tipo
  (`Restricoes.ToList()` na saída).
  - Validação: `GET /api/aluno` devolve `restricoes` como array JSON.
- [ ] T6: criar migration apenas se o mapeamento alterar o schema; caso contrário,
  registrar em comentário que não houve mudança.
  - Validação: `dotnet ef migrations list` sem migration pendente inesperada.
- [ ] T7: executar gates (`dotnet build`, `dotnet test`).

## Definição de pronto

- Coleção imutável do ponto de vista externo.
- Contrato JSON inalterado.
- Testes cobrindo as seis linhas da tabela de `design.md`.
