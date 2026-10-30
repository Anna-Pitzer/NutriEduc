---
name: autorizacao-endpoints-aluno
status: created
references: V5, ADR-002, L6
---

# Tasks — Autorização dos endpoints de aluno

## Pré-condição

- [x] T0: Specs 002 (login por cookie) e 005 (integração) implementadas.
  - Validação: `dotnet test` verde e `POST /api/usuario/login` emitindo
    `NutriEduc.Auth`.

## Backend

- [x] T1: adicionar `[Authorize]` em `AlunoController` (nível de classe), sem
  `[AllowAnonymous]` em nenhum endpoint.
  - Validação: `dotnet build` e teste manual: `GET /api/aluno` sem cookie → 401.

## Testes

- [x] T2: adicionar `CreateAuthenticatedClientAsync()` em `ApiFactory`, registrando e
  autenticando um usuário com `HandleCookies = true`.
  - Validação: `GET /api/usuario/me` com o cliente devolve 200.
- [x] T3: migrar `AlunoApiTests` para o cliente autenticado.
  - Validação: `dotnet test --filter AlunoApiTests` continua verde.
- [x] T4: adicionar teste anônimo cobrindo os cinco verbos com 401 e corpo sem
  dados de aluno.
  - Validação: `dotnet test --filter AlunoApiTests` verde com o novo caso.

## Frontend

- [x] T5: adicionar `credentials: "include"` nas cinco chamadas de `AlunoApi.ts`.
  - Validação: busca textual encontra cinco ocorrências.
- [x] T6: tratar 401 nas telas de aluno com redirecionamento para `/login`.
  - Validação: `npm run build` e `npm run lint` sem erro; teste manual sem sessão.

## Fechamento

- [x] T7: atualizar a tabela de endpoints do `README.md` indicando sessão exigida.
  - Validação: tabela coincide com o controller.
- [x] T8: executar os gates completos.
  - Validação: `dotnet build backend/backend.csproj`; `dotnet test`;
  - Validação: `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Nenhum endpoint de aluno responde sem sessão.
- Chamadas do frontend carregam o cookie e tratam 401.
- Suíte verde com cobertura dos cinco verbos anônimos.

## Registro de execução

Executado em 2026-10-30. Sem migration: a mudança é de borda (atributo + cliente +
serviço), sem alteração de schema.

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `using Microsoft.AspNetCore.Authorization` e `[Authorize]` na classe (`AlunoController.cs:1-8`) |
| T2 | `CreateAuthenticatedClientAsync()` registra usuário único e faz login com `HandleCookies` (`ApiFactory.cs:47-79`) |
| T3 | `AlunoApiTests` não usa mais cliente anônimo; cada teste obtém o seu via fábrica |
| T4 | `Endpoints_SemSessao_Retornam401SemDados` cobre POST/GET/GET-perfil/PUT/DELETE e confere que o corpo não contém `nome` |
| T5 | `credentials: "include"` nas 5 chamadas de `AlunoApi.ts` |
| T6 | `ErroRequisicao` + `naoAutorizado` (`erroApi.ts:19-33`); as duas telas chamam `logout()` ao receber 401; `logout` memoizado com `useCallback` (`authContext.tsx:123`) |
| T7 | Tabela de alunos com coluna `Autenticação = Sim` (`README.md:165-171`) |
| T8 | `dotnet test` → **153/153** (140 unitários + 13 integração); `npm run build` OK; `npm run lint` 0 erros e 5 avisos (baseline) |

**Desvios registrados**

- D1: `extrairErro` devolvia só a mensagem e perdia o status. Em vez de duplicar a
  leitura do corpo, foi criado `ErroRequisicao`, que carrega `status` e é reconhecido
  por `naoAutorizado`. Nenhum outro serviço foi alterado.
- D2: `logout` era recriado a cada render e gerava dois avisos novos de
  `exhaustive-deps` nas telas. Foi memoizado com `useCallback` em `authContext` e
  incluído nas dependências dos efeitos.
- D3: uma única `Fact` percorre os cinco verbos anônimos, em vez de cinco testes
  separados; o objetivo é provar o atributo no controller, e a mensagem de falha
  identifica o verbo pela linha.
- D4: o teste anônimo verifica status e ausência de dados no corpo, não a ausência de
  persistência; o `[Authorize]` bloqueia antes do service, então não há caminho de
  escrita.

**Pendências**

Nenhuma task pendente. O status no roadmap continua `planned`: a promoção para
`verified`/`implemented` é feita pelos comandos do pipeline.
