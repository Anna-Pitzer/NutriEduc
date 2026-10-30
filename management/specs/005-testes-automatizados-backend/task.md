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

- [x] T1: criar `backend.Tests/backend.Tests.csproj` com os pacotes da tabela de
  `design.md`.
  - Validação: `dotnet test backend.Tests/backend.Tests.csproj` executa 0 testes sem erro.
- [x] T2: expor `Program` para integração (`public partial class Program { }` em
  `backend/Program.cs`).
  - Validação: `dotnet build backend/backend.csproj`.
- [x] T3: implementar `Fakes/UsuarioRepositoryFake.cs` e `Fakes/AlunoRepositoryFake.cs`.
  - Validação: compilação e uso nos testes de serviço.
- [x] T4: `Domain/UsuarioTests.cs` com os casos de RF2.
  - Validação: `dotnet test --filter Domain.UsuarioTests` verde.
- [x] T5: `Domain/AlunoTests.cs` com invariantes e restrições.
  - Validação: `dotnet test --filter Domain.AlunoTests` verde.
- [x] T6: `Domain/HashSenhaTests.cs` (round-trip, salt distinto, malformado).
  - Validação: `dotnet test --filter HashSenhaTests` verde.
- [x] T7: `Services/UsuarioServiceTests.cs` e `Services/AlunoServiceTests.cs`.
  - Validação: `dotnet test --filter Services` verde.
- [x] T8: `Integration/AlunoApiTests.cs` e `Integration/UsuarioApiTests.cs`.
  - Validação: `dotnet test --filter Integration` verde.
- [x] T9: documentar no `README.md` o comando de teste.
  - Validação: comando do README executa a suíte.

## Definição de pronto

- `dotnet test` verde com contagem reportada.
- Nenhum teste de rede; nenhuma dependência de ordem.

## Registro de execução

Verificado contra o código em 2026-10-30. O `status` do frontmatter não foi alterado
(ver Spec 001, mesmo registro).

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `backend.Tests.csproj` criado e executando — ver D1 |
| T2 | `public partial class Program { }` (`Program.cs:67`) |
| T3 | `Fakes/UsuarioRepositoryFake.cs` e `Fakes/AlunoRepositoryFake.cs` |
| T4 | `Domain/UsuarioTests.cs` (25 métodos, incluindo senha e hash) |
| T5 | `Domain/AlunoTests.cs` (26 métodos) |
| T6 | `Domain/HashSenhaTests.cs` (8 métodos) |
| T7 | `Services/UsuarioServiceTests.cs` (14) e `Services/AlunoServiceTests.cs` (10) |
| T8 | `Integration/ApiFactory.cs`, `AlunoApiTests.cs` (6) e `UsuarioApiTests.cs` (6); `--filter Integration` → 12/12 |
| T9 | comando e filtro documentados em `README.md:177-185` |

Total executado em 2026-10-30: **152 testes, 152 aprovados, 0 falhas**.

**Pendente**

- Nenhuma task pendente.

**Desvios registrados**

- D1: `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 foi adicionado nesta execução;
  `Microsoft.EntityFrameworkCore.Sqlite` chega transitivamente pela referência a
  `backend.csproj`, o que basta ao SQLite em memória usado pela fábrica.
- D2: para tornar a API testável sem tocar `database.db`, `ConnectionContext` ganhou
  construtor que recebe `DbContextOptions<ConnectionContext>` e o `OnConfiguring` passou
  a respeitar `options.IsConfigured`. `UsuarioRepository` passou a receber o contexto
  por construtor (PEND-004), como `AlunoRepository` já fazia — sem isso a fábrica não
  isola o banco.
- Arquivos fora da matriz original, criados por causa da composição de `Aluno`:
  `Domain/TurmaTests.cs`, `Domain/ResponsavelTests.cs`,
  `Infrastructure/ConnectionContextTests.cs` e `Support/UsuarioFabrica.cs`.
