---
name: publicacao-historico-cardapio
status: created
references: V6, ADR-004, L7
---

# Tasks — Publicação, versionamento e histórico

## Pré-condição

- [ ] T0: Specs 012 e 014 implementadas.
  - Validação: `dotnet test` verde; `GET /api/cardapios/{id}/compatibilidade`
  responde com bloqueantes.

## Domínio

- [ ] T1: ampliar `SituacaoCardapio` com `Substituido` e criar `EventoCardapio`.
  - Validação: compila; nenhum uso de string de situação no serviço.
- [ ] T2: criar `HistoricoCardapio` e `TransicaoInvalidaException`.
  - Validação: teste de construtor interno e de snapshot de autor/motivo.
- [ ] T3: implementar `Publicar`, `Cancelar`, `CriarRevisao` e `MarcarSubstituido`
  em `Cardapio`, com `RegistrarEvento` privado.
  - Validação: testes de todas as transições inválidas;
  - Validação: teste de revisão incrementa versão e mantém refeições.

## Persistência

- [ ] T4: mapear `HistoricoCardapios`, `RevisaoDeId` e `[ConcurrencyCheck]` em
  `Situacao`/`Versao`; migration `PublicacaoHistoricoCardapio`.
  - Validação: `dotnet ef migrations add PublicacaoHistoricoCardapio`; teste de modelo
  confirma os índices.
- [ ] T5: ampliar `ICardapioRepository` com `ExistePublicadoNoPeriodo`,
  `ListarHistorico` e `ListarAlteracoes(desde, escopo)`.
  - Validação: teste em SQLite confirma detecção de sobreposição e ordem do feed.

## Aplicação e API

- [ ] T6: criar `PublicacaoCardapioService` com a ordem de sete passos do design.
  - Validação: testes de 422 sem escrita, 409 com id conflitante e substituição de
  revisão.
- [ ] T7: adicionar as rotas de publicar, cancelar, revisão, histórico e alterações.
  - Validação: testes HTTP da matriz 200/400/403/409/422.
- [ ] T8: traduzir `DbUpdateConcurrencyException` para 409 no middleware de erro.
  - Validação: teste concorrente com duas publicações exige um 200 e um 409.

## Frontend

- [ ] T9: adicionar ações e selo de situação em `DetalheCardapio.tsx`, com motivo de
  bloqueio e linha do tempo.
  - Validação: `npm run build`; publicar, cancelar e revisar pela interface.
- [ ] T10: criar `pages/merendeiras/AlteracoesCardapios.tsx` consumindo
  `/api/cardapios/alteracoes`.
  - Validação: merendeira vê apenas a própria unidade; nutrição vê tudo.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Ciclo de vida completo (rascunho → publicado → substituído/cancelado) com
  histórico append-only e feed por escopo.
- Publicação bloqueada por incompatibilidade e por sobreposição.
- Gates verdes.
