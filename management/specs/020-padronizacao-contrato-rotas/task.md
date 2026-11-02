---
name: padronizacao-contrato-rotas
status: created
references: V9, V8, ADR-005, L6, L7
---

# Tasks — Padrão de contrato das rotas HTTP

## Pré-condição

- [x] T0: confirmar o baseline antes de tocar no contrato.
  - Validação: `dotnet build`; `dotnet test` → **161/161**; `npm run build`;
    `npm run lint` → 0 erros e 5 avisos.

## Contrato de erro

- [x] T1: criar `backend/API/Middleware/ContratoDeErro.cs` com
  `InvalidModelStateResponseFactory` (`{ erro }`) e `UseStatusCodePages` para
  `404`/`405`/`415`.
  - Validação: teste de integração com JSON truncado devolve
    `400 {"erro":"Requisição inválida."}`.
- [x] T2: registrar `AddContratoDeErro()` e `UseContratoDeErro()` em `Program.cs`, antes
  de `UseAuthentication`.
  - Validação: `dotnet build`; `GET /api/nao-existe` devolve `404 { erro }`.
- [x] T3: adicionar `ErrorMessage` em português nos atributos de `AtualizarPerfilDTO`.
  - Validação: `PUT /api/usuario/perfil/{id}` com `nome` vazio devolve `400` com
    mensagem em português.

## Criação com 201

- [x] T4: `AlunoService.CriarAluno` devolve `DadosAlunoDTO` (reutilizando `ParaDTO`) e
  `AlunoController` responde `Created` com `Location: /api/aluno/perfil/{id}`.
  - Validação: `POST /api/aluno/registrar` → `201`, `Location` presente, corpo com `id`.
- [x] T5: `UsuarioService.CriarUsuario` devolve `int?` (id criado ou `null` para e-mail
  duplicado) e `UsuarioController` responde `201` com `{ id, nome, email }` ou
  `400 { erro = "E-mail já cadastrado." }`.
  - Validação: `POST /api/usuario/registrar` → `201` + `Location`; duplicado → `400`.
- [x] T6: remover a resposta `string` de sucesso dos dois controllers.
  - Validação: nenhum `return Ok("` nos controllers.

## Autorização do perfil

- [x] T7: `GET /api/usuario/perfil/{id:int}` com `[Authorize]`, comparação `id da rota
  == id da sessão` (`403` quando divergir) e corpo sem `cpf` e sem `senha`.
  - Validação: anônimo → `401 { erro }`; outro usuário → `403 { erro }`; dono → `200`
    sem `cpf`/`senha`.

## Uniformidade de rota

- [x] T8: aplicar `{id:int}` em `GET /api/aluno/perfil/{id}`,
  `GET /api/usuario/perfil/{id}` e `PUT /api/usuario/perfil/{id}`.
  - Validação: `GET /api/aluno/perfil/abc` devolve `404` sem alcançar o service.

## Código morto

- [x] T9: apagar `backend/Application/AlterarSenhaDTO.cs` e remover
  `UsuarioService.TrocarSenha` com seus dois testes.
  - Validação: busca por `TrocarSenha` e `AlterarSenhaDTO` retorna zero resultados.

## Testes

- [x] T10: criar `backend.Tests/Integration/ContratoRotasTests.cs` com os casos do
  design (201, 401, 403, 400 de modelo, 400 de JSON, 405, 415, 404 de rota, corpo sem
  `cpf`/`senha`, `204` sem corpo).
  - Validação: `dotnet test --filter ContratoRotasTests` verde (11 casos).
- [x] T11: atualizar `AlunoApiTests` (200 → 201), `UsuarioApiTests` (200 → 201) e
  `ErroApiTests` (perfil de usuário sem sessão → `401`).
  - Validação: `dotnet test` verde, sem testes ignorados.

## Fechamento

- [x] T12: atualizar `README.md` — `201` nas duas criações, `405`, `415`, regra de dono
  do perfil — na mesma mudança.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [x] T13: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.
- [x] T14: registrar na spec a execução, as pendências fechadas (PEND-001 e PEND-012) e
  o contador de testes.
  - Validação: `pending-items.md` com PEND-001 e PEND-012 em `done`; contador em
    `arch.md`, `cross-session` e `inicial.md` atualizado.

## Definição de pronto

- Toda rota com `id` usa `{id:int}`.
- Sucesso com corpo é sempre objeto; criação devolve `201` + `Location`.
- Todo erro sob `/api/` responde `{ erro }` em português, incluindo modelo inválido,
  JSON malformado, `405`, `415` e rota inexistente.
- `GET /api/usuario/perfil/{id}` exige sessão, valida dono e não expõe `cpf`/`senha`.
- Nenhum DTO ou método órfão de troca de senha.
- `README.md` sincronizado e gates verdes.

## Registro de execução

Executado em 2026-10-30. Sem migration e sem mudança de schema.

**Concluído**

| Task | Evidência |
| --- | --- |
| T1 | `ContratoDeErro` com `InvalidModelStateResponseFactory`, `UseStatusCodePages` e `ContratoDeErroFilter` |
| T2 | `AddContratoDeErro()` após `AddControllers()`; `UseContratoDeErro()` antes de `ErrorHandlingMiddleware` e de `UseAuthentication` |
| T3 | `AtualizarPerfilDTO` com `ErrorMessage` em português nos quatro atributos |
| T4 | `AlunoService.CriarAluno` → `DadosAlunoDTO`; `AlunoController` → `Created(...)` |
| T5 | `UsuarioService.CriarUsuario` → `int?`; `UsuarioController` → `Created(...)` ou `400 { erro }` |
| T6 | Nenhum `return Ok("` restante nos controllers |
| T7 | `GetUser` com `[Authorize]`, `Forbid()` quando id ≠ sessão e corpo restrito a `id`, `nome`, `email`, `telefone`, `foto` |
| T8 | `{id:int}` aplicado nas três rotas |
| T9 | `AlterarSenhaDTO.cs` apagado; `TrocarSenha` e dois testes removidos |
| T10 | `ContratoRotasTests` com 11 casos |
| T11 | `AlunoApiTests` (201 + `Location`), `UsuarioApiTests` (201 + `Location`), `ErroApiTests` (401) |
| T12 | `README.md` com `201`/`Location`/`204`, `405`, `415`, `404` de rota e corpo do perfil |
| T13 | `dotnet build` 0 erros e 7 avisos (baseline); `dotnet test` **170/170** (138 unitários + 32 integração); `npm run build` OK; `npm run lint` 0 erros e 5 avisos |
| T14 | PEND-001 e PEND-012 em `done`; contadores atualizados em `arch.md`, `cross-session` e `inicial.md` |

**Desvios registrados**

- D1: `415` não é observável por `UseStatusCodePages`. O `[ApiController]` converte o
  erro de content-type em `ProblemDetails` via `ClientErrorResultFilter`, que já escreve
  corpo. A conversão passou a ser feita por `ContratoDeErroFilter`
  (`IAlwaysRunResultFilter`) no mesmo arquivo, mantendo uma única autoridade do formato.
- D2: JSON malformado chega ao `ModelState` com a **chave do parâmetro**
  (`dadosLoginDTO`), não `""`/`"$"`, e a mensagem padrão é em inglês
  (`The dadosLoginDTO field is required.`). A classificação compara a chave com os nomes
  dos parâmetros da action (`ActionDescriptor.Parameters`), sem inspecionar texto.
- D3: o `404` de usuário inexistente deixou de ser observável por HTTP: com sessão
  válida, qualquer id diferente do dono responde `403` antes da consulta. O `404`
  permanece no controller para o caso de o registro desaparecer entre as duas consultas.
  O teste `Usuario_Inexistente_Retorna404ComErro` virou
  `Usuario_PerfilSemSessao_Retorna401ComErro`.
- D4: o `README.md` ainda listava `/colors` na tabela de rotas do frontend, removida pela
  Spec 008; a linha saiu por ser fato falso na documentação viva.
- D5: dois testes unitários de `CriarUsuario` foram ajustados de `bool` para `int?`; o de
  duplicado passou a `CriarUsuario_ComEmailDuplicado_RetornaNull`.
- D6: o backend em execução (PID 5720) foi encerrado para liberar a cópia do build, com
  autorização do usuário. Nenhum `.bak` foi gerado.

**Pendências**

Nenhuma task pendente. O status no roadmap continua `planned`; a promoção é do pipeline.
