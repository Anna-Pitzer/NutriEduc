---
name: autorizacao-endpoints-aluno
status: created
references: V5, ADR-002, L6
---

# Tasks — Autorização dos endpoints de aluno

## Pré-condição

- [ ] T0: Specs 002 (login por cookie) e 005 (integração) implementadas.
  - Validação: `dotnet test` verde e `POST /api/usuario/login` emitindo
    `NutriEduc.Auth`.

## Backend

- [ ] T1: adicionar `[Authorize]` em `AlunoController` (nível de classe), sem
  `[AllowAnonymous]` em nenhum endpoint.
  - Validação: `dotnet build` e teste manual: `GET /api/aluno` sem cookie → 401.

## Testes

- [ ] T2: adicionar `CreateAuthenticatedClientAsync()` em `ApiFactory`, registrando e
  autenticando um usuário com `HandleCookies = true`.
  - Validação: `GET /api/usuario/me` com o cliente devolve 200.
- [ ] T3: migrar `AlunoApiTests` para o cliente autenticado.
  - Validação: `dotnet test --filter AlunoApiTests` continua verde.
- [ ] T4: adicionar teste anônimo cobrindo os cinco verbos com 401 e corpo sem
  dados de aluno.
  - Validação: `dotnet test --filter AlunoApiTests` verde com o novo caso.

## Frontend

- [ ] T5: adicionar `credentials: "include"` nas cinco chamadas de `AlunoApi.ts`.
  - Validação: busca textual encontra cinco ocorrências.
- [ ] T6: tratar 401 nas telas de aluno com redirecionamento para `/login`.
  - Validação: `npm run build` e `npm run lint` sem erro; teste manual sem sessão.

## Fechamento

- [ ] T7: atualizar a tabela de endpoints do `README.md` indicando sessão exigida.
  - Validação: tabela coincide com o controller.
- [ ] T8: executar os gates completos.
  - Validação: `dotnet build backend/backend.csproj`; `dotnet test`;
  - Validação: `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Nenhum endpoint de aluno responde sem sessão.
- Chamadas do frontend carregam o cookie e tratam 401.
- Suíte verde com cobertura dos cinco verbos anônimos.
