---
name: acoes-do-nutricionista
status: created
references: Spec 038
---

# Tasks — Ações do nutricionista

> **Estado:** implementação e validação executadas em 2026-11-17, a pedido do proprietário,
> antes do registro formal desta spec. A promoção de status no roadmap é do pipeline.

## Backend

- [x] **T1 — Bloquear criação e ações administrativas do nutricionista em solicitações.**
  - Arquivos: `backend/API/Services/SolicitacaoService.cs`.
  - Ação: remover `Nutricionista` de `PodeCriar`; barrar `Analisar`, `Concluir` e
    `MarcarAnafilaxia`; permitir decisão direta de `Aberta` com `EmAnalise` registrado.
  - Critério: `403` nas ações proibidas; aprovação e recusa funcionam sem clique de análise.
  - Evidência: `SolicitacaoServiceTests` (criação, análise e conclusão proibidas; histórico
    da decisão direta) e `SolicitacaoApiTests` verdes.

- [x] **T2 — Tornar ocorrência somente leitura para o nutricionista.**
  - Arquivos: `backend/API/Services/OcorrenciaService.cs`.
  - Ação: remover `Nutricionista` de `PodeCriar` e `PodeMudarStatus`; barrar `Cancelar`.
  - Critério: `403` em criação, status e cancelamento; `GET` inalterado.
  - Evidência: `OcorrenciaApiTests` (novos casos `Nutricionista_NaoRegistraOcorrencia_403`
    e `Nutricionista_NaoMudaStatus_403`) verdes.

- [x] **T3 — Alinhar a matriz declarativa.**
  - Arquivos: `backend/Application/Seguranca/MatrizDePermissoes.cs`.
  - Ação: `(Ocorrencia, Nutricionista)` de `RW` para `R` em `Documentada` e `Nivel`.
  - Critério: teste de concordância entre as duas tabelas verde.
  - Evidência: `MatrizPermissoesTests` verde.

## Frontend

- [x] **T4 — Refletir as regras nas telas.**
  - Arquivos: `frontend/src/pages/solicitacoes/SolicitacoesPage.tsx` e
    `frontend/src/pages/ocorrencias/OcorrenciasPage.tsx`.
  - Ação: ocultar criação, análise, conclusão, anafilaxia, status e cancelamento conforme o
    papel; manter aprovar e recusar para o nutricionista.
  - Critério: nutricionista vê apenas decisão em solicitações e leitura em ocorrências.
  - Evidência: `npm run build` e `npm run lint` verdes.

## Testes

- [x] **T5 — Atualizar e adicionar testes.**
  - Arquivos: `SolicitacaoServiceTests.cs`, `OcorrenciaApiTests.cs`,
    `SolicitacaoAnafilaxiaApiTests.cs` e `NotificacaoEventosApiTests.cs`.
  - Ação: substituir cenários que criavam como nutricionista; adicionar casos `403`; manter
    a cobertura de notificação e anafilaxia com papéis permitidos.
  - Critério: suíte completa verde.
  - Evidência: 862/862 testes em 2026-11-17.

## Validação

- [x] `dotnet build backend.Tests/backend.Tests.csproj -c Release --no-restore` — 0 erros.
- [x] `dotnet test backend.Tests/backend.Tests.csproj -c Release --no-build` — 862/862.
- [x] `cd frontend && npm run build` — ok.
- [x] `cd frontend && npm run lint` — 0 erros.
