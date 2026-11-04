---
name: feedback-e-notificacoes
status: created
references: V6, V17, ADR-020, ADR-021, ADR-004, L7
---

# Requirements — Feedback e notificações

## Objetivo

Merendeiras, pais e escolas não têm canal de retorno: um pedido feito por telefone
não deixa resposta no sistema, e uma alteração de cardápio não chega a quem serve a
refeição. Esta spec fecha o ciclo de comunicação com feedback bidirecional e central
de notificações.

## Escopo

- Backend: `Feedback` com resposta, `Notificacao` por destinatário e um ponto único
  de emissão de notificações consumido pelas specs 010, 015, 016, 017 e 030. Escopo de
  leitura por unidade, escola e vínculo (Specs 027 e 028).
- Frontend: caixa de notificações no cabeçalho, telas de feedback para merendeira,
  pais, escola e nutrição.
- Fora de escopo: e-mail, SMS, push e qualquer canal externo; relatórios (Spec 019);
  fluxo de solicitações (Spec 017) e payload de dados cadastrais (Spec 030).

### Cobertura de `implementacoes.md`

- **Merendeiras (3):** enviar feedback, visualizar respostas/retornos das solicitações
  e receber notificações de alterações ou solicitações.
- **Pais (3):** enviar feedback, visualizar retorno dos feedbacks e receber
  notificações sobre alterações relevantes no cadastro do aluno.
- **Escolas (1):** acompanhar feedbacks da escola.

## Requisitos funcionais

- RF1: `Feedback` tem `Assunto` (5–120), `Mensagem` (5–1000), `AutorId`,
  `AutorNome`, `PapelAutor`, `UnidadeId`, `EscolaId` derivado, `Status` (`Aberto`,
  `Respondido`, `Encerrado`) e `CriadoEmUtc`. `EscolaId` é a escola do autor quando ele
  tem escola e a escola do aluno quando o feedback referencia um aluno.
- RF2: `POST /api/feedbacks` é permitido a `Merendeira`, `Responsavel` e
  `AdminEscola`. `Nutricionista` não envia: responde. `AdminSecretaria` não envia nem
  responde: apenas lê (matriz 028).
- RF3: `POST /api/feedbacks/{id}/responder` é exclusivo de `Nutricionista` (feedback
  da própria unidade) e `AdminEscola` (feedback da própria escola); exige `resposta`
  (5–1000) e leva o status a `Respondido`. `AdminSecretaria` apenas lê (matriz 028).
- RF4: o autor encerra o próprio feedback respondido (`Respondido → Encerrado`);
  nenhum outro papel encerra.
- RF5: `GET /api/feedbacks` aplica escopo: autor vê os próprios; `AdminEscola` e
  `Merendeira` os da própria escola; `Nutricionista` e `AdminSecretaria` os da própria
  unidade. Nenhum papel enxerga outra unidade. Filtro de `escolaId`/`unidadeId` fora do
  escopo devolve 403.
- RF6: `Notificacao` tem `DestinatarioId`, `Tipo`, `Titulo` (5–120), `Mensagem`
  (5–500), `ReferenciaTipo`/`ReferenciaId` opcionais, `Lida` e `CriadoEmUtc`.
- RF7: tipos mínimos: `CardapioPublicado`, `CardapioAlterado`, `SolicitacaoStatus`,
  `OcorrenciaStatus`, `AlunoAtualizado`, `FeedbackRespondido`.
- RF8: emissão é síncrona e na mesma transação da mudança de origem, por um único
  serviço (`NotificacaoService.Notificar`). Não existe job, fila ou polling.
- RF9: destinatários por tipo: cardápio → merendeiras ativas de todas as escolas da
  unidade do cardápio; solicitação → solicitante e o decisor do tipo (nutrição ou ADM);
  ocorrência → autor e responsáveis dos alunos envolvidos; aluno atualizado →
  responsáveis vinculados; feedback respondido → autor do feedback.
- RF10: `GET /api/notificacoes?naoLidas=` devolve apenas as do usuário autenticado,
  ordenadas por data decrescente, paginadas (1–100); `POST /{id}/ler` e
  `POST /ler-todas` marcam como lidas.
- RF11: notificação de outro usuário devolve 404 (não 403), para não revelar
  existência.
- RF12: o frontend mostra contador de não lidas no cabeçalho, lista com link para o
  recurso referenciado e marca como lida ao abrir.
- RF13: conteúdo de notificação nunca inclui CPF, e-mail, telefone ou dado clínico
  além do nome do aluno quando necessário.

## Requisitos não funcionais

- RNF1: emissão centralizada; nenhuma spec cria `Notificacao` diretamente.
- RNF2: falha ao notificar não pode desfazer a mudança de negócio já válida — a
  emissão faz parte da mesma transação, então falha nela desfaz tudo, e isso é
  intencional para não haver mudança sem aviso.
- RNF3: consultas por destinatário e índice `(DestinatarioId, Lida, CriadoEmUtc)`.
- RNF4: notificações antigas não são apagadas; retenção fica fora desta spec.
- RNF5: contrato `{ erro }` em 400/403/404/409; recurso de outra unidade responde 404.

## Restrições

- Migration nova `FeedbackNotificacoes`.
- Não altera o modelo das specs 015, 016 e 017; apenas é chamado por elas.
- Nenhuma dependência nova no frontend.

## Dependências

- Specs 009 (papéis), 015, 016, 017 e 030 implementadas ou com interface estável.
- Specs 027 (hierarquia) e 028 (matriz) implementadas: o destinatário e o escopo de
  leitura dependem da escola e da unidade derivadas.
- Implementar 018 **após** 015–017 e 030 para plugar as chamadas nos pontos de transição.

## Casos de borda

- Feedback com autor desativado → permanece visível e respondível.
- Responder feedback já respondido → 409.
- Encerrar feedback `Aberto` → 409.
- Notificação para usuário desativado → não é criada; a mudança de origem continua.
- Vários responsáveis pelo mesmo aluno → uma notificação por responsável.
- Feedback de outra unidade → 404; de outra escola da mesma unidade → 403.
- Merendeira de escola sem cardápio publicado → nenhuma notificação de cardápio.
- Marcar como lida duas vezes → idempotente, 200.
- `naoLidas=true` com zero → lista vazia, 200.
- Token de outro usuário manipulando id → 404.

## Critérios de aceitação

- [ ] Merendeira envia feedback e nutrição responde; autor vê a resposta e encerra.
- [ ] Escola vê apenas feedbacks da própria escola.
- [ ] Publicar cardápio cria notificação para cada merendeira ativa das escolas da
  unidade.
- [ ] Aprovar solicitação cria notificação para o solicitante e para o decisor do tipo.
- [ ] Aprovar alteração de dados cadastrais (Spec 030) notifica os responsáveis
  vinculados.
- [ ] Mudar status de ocorrência notifica autor e responsáveis dos alunos.
- [ ] Atualizar cadastro de aluno notifica responsáveis vinculados.
- [ ] Notificação de outro usuário devolve 404; feedback de outra unidade também.
- [ ] Contador de não lidas atualiza após marcar como lida.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Enviar feedback em cada papel, responder com nutrição e escola, publicar um cardápio,
aprovar uma solicitação e alterar um aluno; conferir a caixa de cada destinatário e a
ausência de vazamento entre usuários. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.

## Revisão — 2026-11-04

Conformidade com as Specs 027, 028 e 030: `EscolaId` derivado, escopo de leitura por
unidade/escola, resposta do ADM apenas em leitura, destinatários calculados pela escola
e pela unidade e notificação do decisor por tipo de solicitação.
