---
name: testes-automatizados-backend
status: created
references: V4, L7
---

# Tasks — Testes automatizados do backend

## Ordem de execução

T1, T2 e T3 criam a infraestrutura de teste (`backend.Tests.csproj`, `Program` parcial,
fakes) e devem rodar **antes da Spec 001** — sem elas, a task T3 da Spec 001 não tem
como executar `dotnet test`. Os itens T4–T9 vão **depois da Spec 001**, porque cobrem
`PUT /api/aluno/{id}`, que só passa a existir lá (requisito RF6).

A versão anterior desta task não declarava essa divisão e, combinada com a task T3 da
Spec 001, criava um ciclo em que nenhuma das duas specs podia vir primeiro.

- [ ] T1: criar `backend.Tests/backend.Tests.csproj` com os pacotes da tabela de
  `design.md`.
  - Validação: `dotnet test backend.Tests/backend.Tests.csproj` executa 0 testes sem erro.
- [ ] T2: expor `Program` para integração (`public partial class Program { }` em
  `backend/Program.cs`).
  - Validação: `dotnet build backend/backend.csproj`.
- [ ] T3: implementar `Fakes/UsuarioRepositoryFake.cs` e `Fakes/AlunoRepositoryFake.cs`.
  - Validação: compilação e uso nos testes de serviço.
- [ ] T4: `Domain/UsuarioTests.cs` com os casos de RF2.
  - Validação: `dotnet test --filter Domain.UsuarioTests` verde.
- [ ] T5: `Domain/AlunoTests.cs` com invariantes e restrições.
  - Validação: `dotnet test --filter Domain.AlunoTests` verde.
- [ ] T6: `Domain/HashSenhaTests.cs` (round-trip, salt distinto, malformado).
  - Validação: `dotnet test --filter HashSenhaTests` verde.
- [ ] T7: `Services/UsuarioServiceTests.cs` e `Services/AlunoServiceTests.cs`.
  - Validação: `dotnet test --filter Services` verde.
- [ ] T8: `Integration/AlunoApiTests.cs` e `Integration/UsuarioApiTests.cs`.
  - Validação: `dotnet test --filter Integration` verde.
- [ ] T9: documentar no `README.md` o comando de teste.
  - Validação: comando do README executa a suíte.

## Definição de pronto

- `dotnet test` verde com contagem reportada.
- Nenhum teste de rede; nenhuma dependência de ordem.
