---
name: feedback-e-notificacoes
status: created
references: V6, V17, ADR-020, ADR-021, ADR-004, L7
---

# Tasks — Feedback e notificações

> **Estado (2026-11-12):** implementação concluída e gates verdes; os checkboxes são projeção
> do motor SDD e serão marcados por `/spec-implement` quando o PEND-028 for corrigido.

## Pré-condição

- [ ] T0: Specs 009, 015, 016, 017, 027, 028 e 030 implementadas.
  - Validação: `dotnet test` verde; transições de cardápio, ocorrência, solicitação e
  dados cadastrais disponíveis em serviço.

## Domínio

- [ ] T1: criar `Feedback` (com `EscolaId` derivado), `StatusFeedback` e as transições
  `Responder`/`Encerrar`.
  - Validação: testes de limites, resposta dupla, encerramento inválido e `EscolaId`
  derivado.
- [ ] T2: criar `Notificacao`, `TipoNotificacao` e `MarcarLida` idempotente.
  - Validação: testes de idempotência e de limites de título/mensagem.

## Persistência

- [ ] T3: mapear `Feedbacks` (com `EscolaId`), `Notificacoes` e índices; migration
  `FeedbackNotificacoes`.
  - Validação: `dotnet ef migrations add FeedbackNotificacoes`; teste de modelo
  confirma o índice `(DestinatarioId, Lida, CriadoEmUtc)`.
- [ ] T4: criar `IFeedbackRepository` e `INotificacaoRepository` com consultas
  paginadas e escopo.
  - Validação: teste em SQLite confirma `naoLidas` e escopo de escola.

## Aplicação e API

- [ ] T5: criar `NotificacaoService` com a tabela de destinatários e emissão em lote.
  - Validação: testes por tipo de origem e de usuário desativado.
- [ ] T6: criar `FeedbackService` com resposta por papel na própria unidade/escola,
  encerramento pelo autor e emissão de `FeedbackRespondido`; ADM apenas lê.
  - Validação: testes de 403/409, de resposta fora do escopo e de notificação ao autor.
- [ ] T7: criar `FeedbackController` e `NotificacaoController`.
  - Validação: testes HTTP 200/400/403/404/409 e isolamento por usuário.
- [ ] T8: plugar as chamadas de notificação em 015, 016, 017, 030 e 010.
  - Validação: testes de integração de cada origem confirmam a notificação criada,
  inclusive para o decisor do tipo de solicitação.

## Frontend

- [ ] T9: criar `SinoNotificacoes` no cabeçalho com contador e marcação de leitura.
  - Validação: `npm run build`; contador atualiza ao abrir a lista.
- [ ] T10: criar telas de feedback por papel e a fila de resposta da nutrição.
  - Validação: enviar, responder e encerrar pela interface em cada papel.

## Conformidade

- [ ] T11: cobrir as células da matriz da Spec 028 para feedback (ADM `R`, Escola `RW`,
  Nutricionista `RW`, Merendeira `RC`, Responsável `RC`).
  - Validação: `MatrizPermissoesTests` passa para o recurso `Feedback`; resposta do ADM
  devolve 403.

## Fechamento

- [ ] T12: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com os controllers.
- [ ] T13: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Feedback bidirecional com resposta e encerramento, sempre no escopo da unidade.
- Notificações emitidas por um único serviço, isoladas por destinatário e pelo tipo de
  solicitação.
- Gates verdes.
