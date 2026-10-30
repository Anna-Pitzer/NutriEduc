---
name: contrato-erro-e-infra-di
status: created
references: V8, ADR-005, L4
---

# Tasks — Contrato de erro único e infraestrutura de DI

## Pré-condição

- [ ] T0: Specs 002 e 005 implementadas; `UsuarioRepository` já recebe
  `ConnectionContext` (PEND-004 fechado).
  - Validação: `dotnet test` verde com os 12 testes de integração.

## Middleware

- [ ] T1: criar `backend/API/Middleware/ErrorHandlingMiddleware.cs` com o mapeamento
  400/404/409/500 e guarda de `Response.HasStarted`.
  - Validação: teste de unidade do mapeamento para cada tipo de exceção.
- [ ] T2: registrar o middleware antes de `UseAuthentication` em `Program.cs`.
  - Validação: `dotnet build`; exceção forçada devolve o contrato.
- [ ] T3: padronizar 401/403 nos eventos de cookie com corpo `{ erro }`.
  - Validação: `GET /api/usuario/me` sem cookie devolve 401 com JSON.

## Controllers

- [ ] T4: remover `try/catch (ArgumentException)` de `AlunoController` e
  `UsuarioController`.
  - Validação: busca textual não encontra `catch (ArgumentException)` nos controllers.
- [ ] T5: trocar `NotFound(string)`, `BadRequest(string)` e `Unauthorized(string)` por
  `{ erro }`.
  - Validação: testes de integração de 400/401/404 conferem a propriedade `erro`.

## Infraestrutura

- [ ] T6: registrar `ConnectionContext` com connection string de configuração e
  fallback `database.db`; adicionar `ConnectionStrings:Default` em `appsettings.json`.
  - Validação: teste alterando a configuração confirma que o banco usado muda.
- [ ] T7: uniformizar lifetimes em `Scoped` (`UsuarioService`, `AlunoService`,
  repositórios).
  - Validação: inspeção de `Program.cs`; teste de resolução do serviço por escopo.

## Testes

- [ ] T8: criar `integration/ErroApiTests.cs` cobrindo 400, 401, 404 e 409.
  - Validação: `dotnet test --filter ErroApiTests` verde.
- [ ] T9: cobrir 500 sem stack trace (endpoint de diagnóstico apenas em `Testing`).
  - Validação: corpo contém `erro = "Erro interno."` e não contém `stack`.

## Fechamento

- [ ] T10: atualizar a seção de erros do `README.md`.
  - Validação: tabela de status e formato coincide com o middleware.
- [ ] T11: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Um único componente traduz exceção em status/corpo.
- Nenhum controller monta resposta de erro fora do contrato.
- DI configurada por connection string, lifetimes uniformes.
- Gates verdes.
