---
name: hierarquia-unidade-escola-aluno
status: implemented
references: V16, ADR-020, L11
---

# Tasks — Hierarquia rede → unidade → escola → aluno

## Pré-condição

- [x] T0: Spec 009 implementada (papéis e autorização) e Spec 022 implementada (bootstrap).
  - Validação: login por papel devolve claims; bootstrap responde 404 sem token.

## Domínio

- [x] T1: adicionar `UnidadeId` obrigatório e unicidade por unidade em `Escola`; implementar
  `DefinirUnidade` com bloqueio por alunos.
  - Validação: testes de construtor, nome duplicado na mesma unidade e mudança bloqueada.
- [x] T2: adicionar `EscolaId` obrigatório em `Aluno` e derivar `UnidadeId` no construtor e em
  `AtualizarDados`; implementar `TransferirEscola` apenas na mesma unidade.
  - Validação: testes de aluno sem escola, unidade derivada e transferência entre unidades
    recusada.
- [x] T3: aplicar a matriz de vínculo institucional em `Usuario.DefinirPapel`.
  - Validação: papel que exige escola sem escola lança; `Responsavel` com escola lança.

## Persistência

- [x] T4: criar a migration `HierarquiaEscolaAluno` com FKs, índices e backfill idempotente.
  - Validação: migration aplica em banco vazio e em banco com dados legados; segunda execução
    não duplica nada.
- [x] T5: substituir em `AlunoRepository` o filtro por `Unidade.EscolaId` por `EscolaId` e
  adicionar o filtro de escola em `EscolaRepository`.
  - Validação: teste verifica que nenhuma consulta usa a navegação legada.

## Aplicação e API

- [x] T6: criar `HierarquiaService` com derivação e validação da cadeia; controllers sem regra.
  - Validação: testes de serviço cobrem RF3–RF5.
- [x] T7: alterar bootstrap para criar a unidade inicial ausente, mantendo o token de uso
  único por unidade.
  - Validação: bootstrap em banco limpo cria unidade + administrador; repetição devolve
    conflito.
- [x] T8: exigir `escolaId` em aluno, restringir `GET /api/escolas` à unidade da sessão e
  acrescentar `POST /api/aluno/{id}/transferir-escola`.
  - Validação: `HierarquiaApiTests` cobre 200/401/403/404 e o registro na trilha (Spec 023).
- [x] T9: criar `GET /api/diagnostico/hierarquia` com contagens de legado.
  - Validação: retorna 403 para papéis não administrativos e 200 para o ADM.

## Frontend

- [x] T10: select de unidade no cadastro de escola e select unidade → escola no cadastro de
  aluno; filtros de listagem por escola.
  - Validação: `npm run build` e `npm run lint` sem erro novo; fluxo manual de cadastro.

## Fechamento

- [x] T11: atualizar a tabela de endpoints do `README.md` e a seção da spec em `arch.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [x] T12: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- Todos os critérios de aceitação de `requirements.md` marcados com evidência.
- Nenhuma rota devolve aluno fora do escopo do papel.
- Diagnóstico de legado disponível e sem divergência de unidade entre aluno e escola.

## Evidências — 2026-11-04

- `backend.Tests`: 517 testes aprovados, 0 falhas (`dotnet test --no-build`).
- `backend`: build sem aviso/erro (`dotnet build --no-restore`).
- `frontend`: `npm run build` e `npm run lint` sem erro.
- `HierarquiaApiTests`: matriz 200/401/403/404, payload sem escola, derivação de unidade,
  payload divergente ignorado, transferência bloqueada com alunos e diagnóstico de legado.
- Endpoints novos documentados no `README.md`; arquitetura atualizada em `arch.md`.

