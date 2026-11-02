---
name: solicitacoes
status: created
references: V6, ADR-004, L7
---

# Requirements — Solicitações

## Objetivo

Merendeiras e pais não têm canal formal para pedir mudança de cardápio, pedir
ingrediente, informar falta ou solicitar ajuste de restrição. Hoje o pedido existe
apenas fora do sistema, sem status, sem responsável e sem retorno. Esta spec cria o
fluxo com decisão registrada e efeito explícito.

## Escopo

- Backend: `Solicitacao` com cinco tipos, ciclo de status, histórico, escopo por
  papel e aplicação do efeito apenas para solicitações de restrição.
- Frontend: telas de solicitação e acompanhamento para merendeira, pais e escola;
  fila de decisão para nutrição.
- Fora de escopo: notificação (Spec 018), alteração automática de cardápio (Spec 015
  é o dono da edição), cadastro do catálogo de restrições (Spec 010).

### Cobertura de `implementacoes.md`

- **Merendeiras (5):** solicitar mudanças nos cardápios, acompanhar solicitações de
  mudança, solicitar ingredientes, informar falta de ingredientes e acompanhar
  solicitações de ingredientes.
- **Pais (4):** solicitar inclusão de alergias, solicitar alteração de restrição
  alimentar, acompanhar solicitações realizadas e visualizar status das solicitações.
- **Escolas (1):** acompanhar solicitações da escola.

## Requisitos funcionais

- RF1: `Solicitacao` tem `Tipo` (`MudancaCardapio`, `Ingrediente`,
  `FaltaIngrediente`, `InclusaoAlergia`, `AlteracaoRestricao`), `Descricao` (5–1000),
  `Status` inicial `Aberta`, `UnidadeId`, `SolicitanteId` e `SolicitanteNome`.
- RF2: campos exigidos por tipo: `MudancaCardapio` exige `CardapioId`;
  `InclusaoAlergia` e `AlteracaoRestricao` exigem `AlunoId` e `RestricaoNome`;
  `Ingrediente` e `FaltaIngrediente` exigem `Ingrediente` (2–120). Campo ausente
  devolve 400.
- RF3: quem solicita: `Merendeira` (unidade própria, tipos de cardápio/ingrediente),
  `Responsavel` (aluno vinculado, tipos de restrição), `AdminEscola` (escola própria),
  `Nutricionista` (qualquer unidade). `AdminSecretaria` não solicita.
- RF4: transições: `Aberta → EmAnalise`, `EmAnalise → Aprovada`, `EmAnalise →
  Recusada`, `Aprovada → Concluida`, `Aberta → Cancelada`. Qualquer outra → 409.
- RF5: `Nutricionista` decide (`EmAnalise`, `Aprovada`, `Recusada`, `Concluida`);
  o solicitante pode cancelar apenas em `Aberta`. Aprovação e recusa exigem
  `resposta` (2–500).
- RF6: aprovar `InclusaoAlergia` ou `AlteracaoRestricao` aplica a mudança no aluno
  pela regra da Spec 010 (catálogo + histórico), na mesma transação da decisão. Se a
  restrição não existir no catálogo, a aprovação falha com 400 e nada muda.
- RF7: aprovar `MudancaCardapio` **não** altera o cardápio: registra a decisão e o
  cardápio é editado pela Spec 015. A resposta deixa isso explícito.
- RF8: `GET /api/solicitacoes` filtra por `tipo`, `status`, `unidadeId`, `alunoId`,
  `cardapioId`, `de`, `ate`, `pagina`, `tamanho` (1–100) e aplica escopo do papel no
  banco.
- RF9: `GET /api/solicitacoes/{id}` devolve detalhe com alvo resolvido (aluno,
  cardápio ou ingrediente) e histórico de status com autor e resposta.
- RF10: solicitante vê as próprias solicitações; `Responsavel` vê também as dos
  alunos vinculados; `AdminEscola` as da escola; `Merendeira` as da unidade;
  `Nutricionista` todas.
- RF11: o frontend exibe fila de decisão para nutrição com filtro por status, e
  acompanhamento com histórico para solicitante, escola e merendeira.
- RF12: nenhuma solicitação é apagada; cancelamento é status.

## Requisitos não funcionais

- RNF1: invariantes e transições no agregado (ADR-004); controller sem regra.
- RNF2: histórico append-only gravado na mesma transação da transição.
- RNF3: snapshots de solicitante e de nomes de aluno/cardápio/restrição no evento.
- RNF4: consultas paginadas e indexadas por `(UnidadeId, CriadoEmUtc)` e
  `(Status, Tipo)`.
- RNF5: contrato `{ erro }` em 400/403/404/409.

## Restrições

- Migration nova `Solicitacoes`.
- Não cria endpoint de edição de cardápio nem de aluno; reutiliza 012/015 e 010.
- Nenhuma dependência nova no frontend.

## Dependências

- Specs 009, 010, 012 e 015 implementadas (015 para o alvo de `MudancaCardapio`).
- Spec 018 consome os eventos de status.

## Casos de borda

- `RestricaoNome` fora do catálogo na aprovação → 400, solicitação permanece
  `EmAnalise`.
- `CardapioId` de outra unidade → 403.
- Pai solicitando para aluno não vinculado → 403.
- Aprovar solicitação já aprovada → 409.
- Cancelar solicitação em `EmAnalise` → 409.
- Solicitação sem `Ingrediente` → 400.
- Recusa sem resposta → 400.
- Aluno excluído depois da solicitação → histórico preserva o nome; detalhe mostra
  vínculo ausente.
- Duas aprovações concorrentes → uma 200 e uma 409 (concorrência otimista).

## Critérios de aceitação

- [ ] Cada tipo de solicitação valida seus campos obrigatórios.
- [ ] Pai solicita inclusão de alergia para aluno vinculado e acompanha o status.
- [ ] Nutrição aprova e a restrição aparece no aluno, com evento no histórico dele.
- [ ] Aprovar restrição inexistente devolve 400 e mantém `EmAnalise`.
- [ ] Aprovar mudança de cardápio não altera o cardápio.
- [ ] Escopo de listagem comprovado por papel e por unidade.
- [ ] Transição inválida devolve 409.
- [ ] Duas decisões concorrentes: uma 200 e uma 409.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Abrir uma solicitação de restrição (pai), uma de ingrediente (merendeira) e uma de
cardápio (escola); decidir com nutrição; confirmar efeito no aluno, ausência de efeito
no cardápio e histórico em todos os casos. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
