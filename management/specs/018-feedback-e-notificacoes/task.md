---
name: feedback-e-notificacoes
status: created
references: V6, ADR-004, L7
---

# Tasks — Feedback e notificações

## Pré-condição

- [ ] T0: Specs 009, 015, 016 e 017 implementadas.
  - Validação: `dotnet test` verde; transições de cardápio, ocorrência e solicitação
  disponíveis em serviço.

## Domínio

- [ ] T1: criar `Feedback`, `StatusFeedback` e as transições `Responder`/`Encerrar`.
  - Validação: testes de limites, resposta dupla e encerramento inválido.
- [ ] T2: criar `Notificacao`, `TipoNotificacao` e `MarcarLida` idempotente.
  - Validação: testes de idempotência e de limites de título/mensagem.

## Persistência

- [ ] T3: mapear `Feedbacks` e `Notificacoes` com índices; migration
  `FeedbackNotificacoes`.
  - Validação: `dotnet ef migrations add FeedbackNotificacoes`; teste de modelo
  confirma o índice `(DestinatarioId, Lida, CriadoEmUtc)`.
- [ ] T4: criar `IFeedbackRepository` e `INotificacaoRepository` com consultas
  paginadas e escopo.
  - Validação: teste em SQLite confirma `naoLidas` e escopo de escola.

## Aplicação e API

- [ ] T5: criar `NotificacaoService` com a tabela de destinatários e emissão em lote.
  - Validação: testes por tipo de origem e de usuário desativado.
- [ ] T6: criar `FeedbackService` com resposta por papel, encerramento pelo autor e
  emissão de `FeedbackRespondido`.
  - Validação: testes de 403/409 e de notificação ao autor.
- [ ] T7: criar `FeedbackController` e `NotificacaoController`.
  - Validação: testes HTTP 200/400/403/404/409 e isolamento por usuário.
- [ ] T8: plugar as chamadas de notificação em 015, 016, 017 e 010.
  - Validação: testes de integração de cada origem confirmam a notificação criada.

## Frontend

- [ ] T9: criar `SinoNotificacoes` no cabeçalho com contador e marcação de leitura.
  - Validação: `npm run build`; contador atualiza ao abrir a lista.
- [ ] T10: criar telas de feedback por papel e a fila de resposta da nutrição.
  - Validação: enviar, responder e encerrar pela interface em cada papel.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com os controllers.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Feedback bidirecional com resposta e encerramento.
- Notificações emitidas por um único serviço e isoladas por destinatário.
- Gates verdes.
