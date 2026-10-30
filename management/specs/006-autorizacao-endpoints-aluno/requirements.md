---
name: autorizacao-endpoints-aluno
status: created
references: V5, ADR-002, L6
---

# Requirements — Autorização dos endpoints de aluno

## Objetivo

`AlunoController` não tem nenhum `[Authorize]`: listagem, consulta, criação,
atualização e exclusão de dados de crianças respondem a requisição anônima. Esta
spec fecha o acesso sem introduzir papéis — a autorização por perfil e por escola é
a Spec 009.

## Escopo

- Backend: exigir sessão em todos os endpoints de `AlunoController`.
- Frontend: enviar o cookie de sessão em todas as chamadas de `AlunoApi.ts`.
- Testes: autenticar os testes de integração de aluno e cobrir o 401 anônimo.
- Fora de escopo: papéis, escopo por escola/unidade (Spec 009), formato de erro
  global (Spec 007), IDOR de `UsuarioController` (Spec 009).

## Requisitos funcionais

- RF1: os cinco endpoints de `AlunoController` (`POST /registrar`, `GET /`,
  `GET /perfil/{id}`, `PUT /{id}`, `DELETE /{id}`) exigem sessão válida e devolvem
  401 sem cookie ou com cookie inválido/expirado.
- RF2: com sessão válida, o comportamento atual é preservado: 200, 400 para payload
  inválido, 404 para id inexistente e 204 para exclusão.
- RF3: `frontend/src/services/AlunoApi.ts` envia `credentials: "include"` nas cinco
  chamadas.
- RF4: a tela de alunos trata 401 redirecionando para o login, sem exibir mensagem
  de erro genérica.
- RF5: nenhuma rota, payload ou assinatura de método muda.
- RF6: a autorização é declarada uma única vez no controller, não repetida endpoint
  a endpoint.
- RF7: os testes de integração de aluno passam a autenticar antes de chamar a API;
  um teste dedicado prova o 401 anônimo em cada verbo.
- RF8: `GET /api/aluno` anônimo não devolve nenhum dado de aluno no corpo.

## Requisitos não funcionais

- RNF1: usa o esquema de cookie existente (`NutriEduc.Auth`), sem token novo.
- RNF2: 401 não redireciona para HTML; o evento `OnRedirectToLogin` já devolve o
  status 401.
- RNF3: o corpo do 401 pode ser vazio nesta spec; o contrato `{ erro }` para 401 é
  responsabilidade da Spec 007.
- RNF4: nenhuma regra de autorização no frontend é tratada como controle; o backend
  é a fonte de verdade.

## Restrições

- Não alterar `Program.cs` além do necessário para registrar a autorização já
  existente (`AddAuthorization` já está registrado).
- Não introduzir policy, papel ou claim nesta spec.
- Não alterar as specs 001–005 nem seus contratos.

## Dependências

- Spec 002 (login por cookie) e Spec 005 (infraestrutura de testes de integração).
- Spec 009 amplia esta autorização com papéis e escopo; a ordem é 006 → 009.

## Casos de borda

- Sem cookie → 401, corpo sem dados de aluno.
- Cookie inválido/adulterado → 401.
- Cookie expirado → 401.
- Sessão válida de qualquer usuário → acesso permitido (ainda sem papel).
- `POST /api/aluno/registrar` anônimo com payload válido → 401, sem persistir.
- `DELETE /api/aluno/{id}` anônimo → 401, sem excluir.
- Sessão removida entre duas chamadas → segunda devolve 401.
- Proxy do Vite: cookie é same-origin, `SameSite=Lax` continua válido.

## Critérios de aceitação

- [x] `GET /api/aluno` sem sessão devolve 401 e corpo sem alunos.
- [x] `GET /api/aluno/perfil/{id}` sem sessão devolve 401.
- [x] `POST /api/aluno/registrar` sem sessão devolve 401 e não persiste.
- [x] `PUT /api/aluno/{id}` sem sessão devolve 401.
- [x] `DELETE /api/aluno/{id}` sem sessão devolve 401.
- [x] Com sessão, os cinco endpoints mantêm os status previstos na RF2.
- [x] `AlunoApi.ts` tem `credentials: "include"` nas cinco chamadas.
- [x] Tela de aluno redireciona para login ao receber 401.
- [x] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Executar os cinco verbos sem cookie e confirmar 401; repetir após login e confirmar
os status de RF2. No navegador, abrir a tela de alunos sem sessão e confirmar o
redirecionamento. Gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj`, `cd frontend && npm run build && npm run lint`.
