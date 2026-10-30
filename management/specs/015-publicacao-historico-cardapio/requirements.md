---
name: publicacao-historico-cardapio
status: created
references: V6, ADR-004, L7
---

# Requirements — Publicação, versionamento e histórico de cardápios

## Objetivo

Um cardápio só é útil quando publicado, mas publicação sem controle vira risco
alimentar: hoje não existe situação, não existe bloqueio por incompatibilidade e
não existe rastro de quem alterou o quê. Esta spec fecha o ciclo de vida do
cardápio e entrega o histórico que merendeiras e nutrição precisam consultar.

## Escopo

- Backend: transições de situação, bloqueio de publicação por compatibilidade,
  versionamento por revisão, histórico append-only e feed de alterações.
- Frontend: ações de publicar/cancelar com confirmação, selo de situação, linha do
  tempo de histórico e tela de alterações para a merendeira.
- Fora de escopo: entrega de notificações (Spec 018), compatibilidade em si
  (Spec 014), geração (Spec 013), edição de rascunho (Spec 012).

### Cobertura de `implementacoes.md`

- **Nutrição (3):** visualizar histórico de cardápios; aprovar/publicar cardápios;
  alterar cardápios já publicados.
- **Merendeiras (3):** verificar histórico de cardápios; visualizar alterações
  realizadas nos cardápios; receber atualizações de cardápios (o feed desta spec é a
  fonte; a entrega é da Spec 018).

## Requisitos funcionais

- RF1: `SituacaoCardapio` passa a ter quatro valores: `Rascunho`, `Publicado`,
  `Cancelado` e `Substituido`. Toda transição não prevista devolve 409.
- RF2: `POST /api/cardapios/{id}/publicar` é exclusivo de `Nutricionista` e só é
  aceito a partir de `Rascunho`. Grava `PublicadoEmUtc` e `PublicadoPor`.
- RF3: a publicação é bloqueada quando a análise de compatibilidade (Spec 014)
  retorna qualquer item bloqueante; a resposta é 422 com `{ erro, detalhes }`
  listando alimento e restrição. Alertas não bloqueiam.
- RF4: não pode existir dois cardápios `Publicado` da mesma unidade com períodos
  sobrepostos; a segunda publicação devolve 409 com o id do conflitante.
- RF5: cardápio `Publicado` é imutável: `PUT` devolve 409 e não altera nada.
- RF6: `POST /api/cardapios/{id}/revisao` cria um novo `Cardapio` em `Rascunho`,
  cópia das refeições e itens, com `Versao = origem.Versao + 1` e
  `RevisaoDeId = origem.Id`. O original permanece `Publicado` até a revisão ser
  publicada.
- RF7: publicar uma revisão marca o original como `Substituido` e o novo como
  `Publicado`, na mesma transação.
- RF8: `POST /api/cardapios/{id}/cancelar` exige `motivo` (2–500) e é aceito em
  `Rascunho` e `Publicado`; em `Publicado`, exclusivo de `Nutricionista`.
- RF9: toda transição e toda edição de rascunho gravam um evento em
  `HistoricoCardapio`: `Criado`, `Editado`, `Duplicado`, `RevisaoCriada`,
  `Publicado`, `Cancelado`, `Substituido`, com autor, data UTC, versão e motivo
  quando aplicável. Nenhum evento é atualizado ou apagado.
- RF10: `GET /api/cardapios/{id}/historico` devolve a linha do tempo em ordem
  decrescente, paginada, para `Nutricionista` (global), `AdminEscola` (escola) e
  `Merendeira` (unidade).
- RF11: `GET /api/cardapios/alteracoes?desde=` devolve eventos de cardápios no
  escopo do usuário a partir de uma data ISO 8601, paginados, com nome do cardápio e
  da unidade; é a fonte da tela "alterações" da merendeira.
- RF12: o frontend exibe situação em lista e detalhe, oferece publicar/cancelar com
  confirmação, mostra o motivo de bloqueio e a linha do tempo; erros do backend são
  exibidos sem tradução própria.
- RF13: concorrência: duas publicações simultâneas do mesmo cardápio resultam em uma
  sucesso e uma 409, nunca em dois publicados.

## Requisitos não funcionais

- RNF1: transições e suas pré-condições vivem no agregado `Cardapio` (ADR-004); o
  serviço apenas carrega dados, chama o domínio e persiste.
- RNF2: histórico e transição são gravados na mesma transação; falha no histórico
  desfaz a transição.
- RNF3: `HistoricoCardapio` é append-only; nenhum endpoint de edição/exclusão.
- RNF4: concorrência otimista em `Situacao` e `Versao`; conflito → 409.
- RNF5: consultas paginadas (1–100) e indexadas por `(CardapioId, CriadoEmUtc)` e
  `(UnidadeId, CriadoEmUtc)`.
- RNF6: contrato `{ erro, detalhes? }` (ADR-005) em 400/409/422.

## Restrições

- Migration nova `PublicacaoHistoricoCardapio`.
- Altera o enum `SituacaoCardapio` criado na Spec 012 — alteração aditiva e
  explícita; nenhum outro contrato de 012 muda.
- Não altera o modelo de compatibilidade (Spec 014) nem o de geração (Spec 013).

## Dependências

- Specs 012 e 014 implementadas.
- Spec 018 consome `GET /api/cardapios/alteracoes`; não é pré-requisito desta spec.

## Casos de borda

- Publicar cardápio já publicado → 409.
- Publicar cardápio cancelado → 409.
- Publicar rascunho de unidade sem alunos → permitido; compatibilidade devolve aviso,
  não bloqueio.
- Revisar cardápio `Substituido` → 409 (só `Publicado` pode ser revisado).
- Cancelar já cancelado → 409.
- Cancelar rascunho por `AdminEscola` → 403.
- Motivo vazio, com 1 ou 501 caracteres → 400.
- Dois publicados sobrepostos na mesma unidade → 409 com id do conflitante.
- Evento de histórico de cardápio excluído → impossível: não há exclusão de cardápio
  publicado, e rascunho excluído cascateia o próprio histórico.
- Data `desde` inválida → 400.

## Critérios de aceitação

- [ ] Publicar rascunho compatível devolve 200 e situação `Publicado`.
- [ ] Publicar com bloqueante devolve 422 e lista alimento/restrição.
- [ ] Publicar com período sobreposto devolve 409 e informa o id conflitante.
- [ ] `PUT` em publicado devolve 409.
- [ ] Revisão cria novo id, versão incrementada e original intacto.
- [ ] Publicar a revisão marca o original como `Substituido`.
- [ ] Cancelar sem motivo devolve 400; com motivo grava evento.
- [ ] Histórico contém todos os eventos na ordem correta.
- [ ] `alteracoes` respeita escopo por unidade.
- [ ] Duas publicações concorrentes: uma 200, uma 409.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Criar rascunho, publicar, tentar editar, criar revisão, publicar revisão, cancelar
outro cardápio e conferir histórico e feed com cada papel. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
