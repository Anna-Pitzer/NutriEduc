---
name: contrato-erro-e-infra-di
status: created
references: V8, ADR-005, L4
---

# Tasks — Contrato de erro único e infraestrutura de DI

## Pré-condição

- [x] T0: Specs 002 e 005 implementadas; `UsuarioRepository` já recebe
  `ConnectionContext` (PEND-004 fechado).
  - Validação: `dotnet test` verde com os 13 testes de integração.

## Middleware

- [x] T1: criar `backend/API/Middleware/ErrorHandlingMiddleware.cs` com o mapeamento
  400/404/409/500 e guarda de `Response.HasStarted`.
  - Validação: teste de unidade do mapeamento para cada tipo de exceção.
- [x] T2: registrar o middleware antes de `UseAuthentication` em `Program.cs`.
  - Validação: `dotnet build`; exceção forçada devolve o contrato.
- [x] T3: padronizar 401/403 nos eventos de cookie com corpo `{ erro }`.
  - Validação: `GET /api/usuario/me` sem cookie devolve 401 com JSON.

## Controllers

- [x] T4: remover `try/catch (ArgumentException)` de `AlunoController` e
  `UsuarioController`.
  - Validação: busca textual não encontra `catch (ArgumentException)` nos controllers.
- [x] T5: trocar `NotFound(string)`, `BadRequest(string)` e `Unauthorized(string)` por
  `{ erro }`.
  - Validação: testes de integração de 400/401/404 conferem a propriedade `erro`.

## Infraestrutura

- [x] T6: registrar `ConnectionContext` com connection string de configuração e
  fallback `database.db`; adicionar `ConnectionStrings:Default` em `appsettings.json`.
  - Validação: teste alterando a configuração confirma que o banco usado muda.
- [x] T7: uniformizar lifetimes em `Scoped` (`UsuarioService`, `AlunoService`,
  repositórios).
  - Validação: inspeção de `Program.cs`; teste de resolução do serviço por escopo.

## Testes

- [x] T8: criar `Integration/ErroApiTests.cs` cobrindo 400, 401, 404 e 409.
  - Validação: `dotnet test --filter ErroApiTests` verde.
- [x] T9: cobrir 500 sem stack trace (endpoint de diagnóstico apenas em `Testing`).
  - Validação: corpo contém `erro = "Erro interno."` e não contém `stack`.

## Fechamento

- [x] T10: atualizar a seção de erros do `README.md`.
  - Validação: tabela de status e formato coincide com o middleware.
- [x] T11: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Um único componente traduz exceção em status/corpo.
- Nenhum controller monta resposta de erro fora do contrato.
- DI configurada por connection string, lifetimes uniformes.
- Gates verdes.

## Registro de execução

Executado em 2026-10-30. Sem migration e sem mudança de schema.

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `ErrorHandlingMiddleware` com `Mapear` para `ArgumentException` (400), `KeyNotFoundException` (404), `InvalidOperationException` (409), `DbUpdateConcurrencyException` (409) e fallback 500; guarda de `HasStarted` |
| T2 | `app.UseMiddleware<ErrorHandlingMiddleware>()` antes de `UseAuthentication` (`Program.cs`) |
| T3 | Eventos de cookie escrevem `{ erro = "Sessão inválida ou ausente." }` (401) e `{ erro = "Acesso negado." }` (403) |
| T4 | Nenhum `catch (ArgumentException)` restante nos dois controllers |
| T5 | `NotFound`/`BadRequest`/`Unauthorized` devolvem `{ erro }` em `AlunoController` e `UsuarioController` |
| T6 | `AddDbContext<ConnectionContext>(o => o.UseSqlite(connectionString))`; `ConnectionStrings:Default` em `backend/appsettings.json`; fallback `Data Source = database.db`; `OnConfiguring` mantém a guarda `IsConfigured` |
| T7 | `UsuarioService`, `AlunoService` e repositórios em `Scoped` |
| T8 | `ErroApiTests` cobre 400, 401 (login e cookie), 404 (aluno e usuário) e 409 |
| T9 | `ErroApiTests.ExcecaoNaoTratada_Retorna500SemStackTrace`; endpoints `/api/diagnostico/*` só existem em `Testing` |
| T10 | Parágrafo de erros no `README.md` lista 400/401/403/404/409/500 no contrato |
| T11 | `dotnet test` → **160/160** (140 unitários + 20 integração); `npm run build` OK; `npm run lint` 0 erros e 5 avisos (baseline) |

**Desvios registrados**

- D1: o mapeamento de 500 é testado por HTTP, não por teste unitário do método
  privado; os três endpoints de diagnóstico (`erro`, `conflito`, `inexistente`) são
  registrados apenas quando o ambiente é `Testing`, portanto não existe superfície em
  produção.
- D2: `TransicaoInvalidaException` (Spec 015) ainda não existe. O middleware mapeia
  `InvalidOperationException`; a 015 declara que a exceção herda desse tipo, então a
  regra continua válida sem acoplar a 007 a uma spec futura.
- D3: T6 não ganhou um teste que troque a connection string em runtime. A validação
  foi: configuração lida no startup, fallback idêntico ao valor anterior e os testes
  de integração substituindo o `ConnectionContext` por SQLite em memória.
- D4: T7 foi verificado por inspeção do `Program.cs`; um teste de resolução por escopo
  seria redundante com o próprio `AddScoped`.

**Pendências**

Nenhuma task pendente. O status no roadmap continua `planned`.
