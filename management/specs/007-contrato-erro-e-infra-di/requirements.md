---
name: contrato-erro-e-infra-di
status: created
references: V8, ADR-005, L4
---

# Requirements — Contrato de erro único e infraestrutura de DI

## Objetivo

Hoje cada controller decide o formato do erro: `AlunoController` devolve
`{ erro }`, `UsuarioController` devolve `string` em `NotFound`/`BadRequest` e
`Unauthorized`. Ao mesmo tempo, `AddDbContext<ConnectionContext>()` é registrado sem
options e o caminho do banco fica fixo no `OnConfiguring`, enquanto `UsuarioService`
é `Transient` e os repositórios são `Scoped`.

## Escopo

- Backend: middleware global que traduz exceções no contrato `{ erro }`; remoção dos
  `try/catch` repetidos; 401/403 também no contrato; DI com lifetimes uniformes e
  `DbContext` configurado por connection string.
- Testes: integração do formato de erro e das novas respostas.
- Fora de escopo: papéis e escopo de dados (Spec 009), autorização dos endpoints de
  aluno (Spec 006), `UsuarioRepository` — **já corrigido** na execução da Spec 005
  (PEND-004 fechado).

## Requisitos funcionais

- RF1: toda exceção de domínio `ArgumentException` vira 400 com
  `{ "erro": "<mensagem>" }`, sem stack trace.
- RF2: toda exceção não tratada vira 500 com
  `{ "erro": "Erro interno." }`, sem detalhe técnico no corpo; a exceção é registrada
  em log com identificador de correlação.
- RF3: `KeyNotFoundException` vira 404 e `TransicaoInvalidaException` (ou
  `InvalidOperationException` de transição) vira 409, ambos no mesmo formato.
- RF4: `DbUpdateConcurrencyException` vira 409 no mesmo formato.
- RF5: respostas 401 e 403 emitidas pelo esquema de cookie usam o mesmo formato JSON.
- RF6: `NotFound`, `BadRequest`, `Unauthorized` e `Conflict` escritos diretamente
  pelos controllers usam `{ erro }`, nunca `string` crua.
- RF7: os `try/catch (ArgumentException)` de `AlunoController` e `UsuarioController`
  são removidos; o middleware é a única tradução.
- RF8: `ConnectionContext` é registrado com
  `AddDbContext<ConnectionContext>(o => o.UseSqlite(connectionString))`, lendo
  `ConnectionStrings:Default` e usando `Data Source = database.db` como fallback.
- RF9: `OnConfiguring` continua respeitando `options.IsConfigured` (já implementado na
  Spec 005) e não sobrescreve a configuração injetada.
- RF10: `UsuarioService`, `AlunoService` e os repositórios são todos `Scoped`, no
  mesmo ciclo de vida do `DbContext`.
- RF11: nenhum `catch` vazio ou `catch (Exception)` que engula erro permanece.
- RF12: os testes de integração cobrem 400, 404, 409 e 500 no formato `{ erro }`.

## Requisitos não funcionais

- RNF1: um único componente traduz exceção em status e corpo; nenhuma duplicação.
- RNF2: logging estruturado com `ILogger`, nível `Error` para 500 e `Warning` para
  400/409 previsíveis.
- RNF3: o middleware não vaza `StackTrace`, `InnerException` nem caminho de arquivo.
- RNF4: mudança de configuração não altera o comportamento padrão (`database.db`).
- RNF5: nenhum endpoint muda de rota ou payload de sucesso.

## Restrições

- Não implementar papéis, claims ou policies (Spec 009).
- Não alterar o esquema do banco nem criar migration.
- Não alterar `UsuarioRepository` além do que já foi feito; ele já recebe o contexto.

## Dependências

- Spec 002 (login/cookie) e Spec 005 (testes de integração).
- Spec 006 pode vir antes ou depois; ambas não mudam rotas.

## Casos de borda

- `ArgumentException` com mensagem vazia → corpo com mensagem genérica de validação.
- Exceção dentro do próprio middleware de autenticação → 500 no contrato.
- Resposta já iniciada quando a exceção ocorre → não tentar reescrever o corpo;
  registrar log e encerrar.
- `NotFound` com id inexistente em `UsuarioController` → `{ erro }`, não string.
- Connection string ausente na configuração → fallback `database.db`.
- `Development` e `Production` produzem o mesmo contrato; só o log muda de detalhe.

## Critérios de aceitação

- [x] `POST /api/aluno/registrar` inválido devolve 400 com `{ erro }`.
- [x] `GET /api/aluno/perfil/{id}` inexistente devolve 404 com `{ erro }`.
- [x] `GET /api/usuario/perfil/{id}` inexistente devolve 404 com `{ erro }` (não string).
- [x] `POST /api/usuario/login` inválido devolve 401 com `{ erro }`.
- [x] Exceção não tratada devolve 500 com `{ erro: "Erro interno." }` e sem stack.
- [x] Nenhum `try/catch (ArgumentException)` permanece nos controllers.
- [x] `ConnectionContext` é resolvido com a connection string configurada.
- [x] `UsuarioService` é `Scoped`, como `AlunoService` e os repositórios.
- [x] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Exercitar cada status (400, 401, 404, 409, 500) e conferir corpo e ausência de stack;
alternar a connection string em configuração e confirmar que o banco usado muda.
Gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj`, `cd frontend && npm run build && npm run lint`.
