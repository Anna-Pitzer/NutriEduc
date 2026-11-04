---
name: solicitacoes
status: created
references: V6, V17, V19, ADR-020, ADR-021, ADR-004, L7
---

# Requirements — Solicitações

## Objetivo

Merendeiras e pais não têm canal formal para pedir mudança de cardápio, pedir
ingrediente, informar falta ou solicitar ajuste de restrição. Hoje o pedido existe
apenas fora do sistema, sem status, sem responsável e sem retorno. Esta spec cria o
fluxo com decisão registrada e efeito explícito.

## Escopo

- Backend: `Solicitacao` com seis tipos, ciclo de status, histórico, escopo por
  unidade, escola e vínculo (Specs 027 e 028) e aplicação do efeito apenas para
  solicitações de restrição.
- Frontend: telas de solicitação e acompanhamento para merendeira, pais e escola;
  filas de decisão para nutrição e para o ADM.
- Fora de escopo: notificação (Spec 018), alteração automática de cardápio (Spec 015
  é o dono da edição), cadastro do catálogo de restrições (Spec 010) e o payload do
  tipo `DadosCadastrais` (Spec 030).

### Cobertura de `implementacoes.md`

- **Merendeiras (5):** solicitar mudanças nos cardápios, acompanhar solicitações de
  mudança, solicitar ingredientes, informar falta de ingredientes e acompanhar
  solicitações de ingredientes.
- **Pais (4):** solicitar inclusão de alergias, solicitar alteração de restrição
  alimentar, acompanhar solicitações realizadas e visualizar status das solicitações.
- **Escolas (1):** acompanhar solicitações da escola.

## Decisões e interpretações

| # | Ponto | Decisão | Origem |
|---|---|---|---|
| D1 | Quem decide pedido de restrição ou alergia do responsável | `AdminSecretaria`, conforme a regra de negócio "encaminhadas para análise do ADM" | Regra do dono, Spec 028 |
| D2 | Quem decide cardápio ou ingrediente | `Nutricionista` da própria unidade | Spec 028 |
| D3 | Escopo da `Merendeira` | escola própria, não a unidade inteira | Spec 027 |
| D4 | Sexto tipo | `DadosCadastrais` é detalhado pela Spec 030; aqui ficam o agregado e o fluxo | Spec 030, L12 |

## Requisitos funcionais

- RF1: `Solicitacao` tem `Tipo` (`MudancaCardapio`, `Ingrediente`,
  `FaltaIngrediente`, `InclusaoAlergia`, `AlteracaoRestricao`, `DadosCadastrais`),
  `Descricao` (5–1000), `Status` inicial `Aberta`, `UnidadeId`, `EscolaId` derivado,
  `SolicitanteId` e `SolicitanteNome`. O tipo `DadosCadastrais` tem payload e regra de
  aplicação definidos pela Spec 030; esta spec é dona apenas do agregado e do fluxo.
- RF2: campos exigidos por tipo: `MudancaCardapio` exige `CardapioId`;
  `InclusaoAlergia` e `AlteracaoRestricao` exigem `AlunoId` e `RestricaoNome`;
  `Ingrediente` e `FaltaIngrediente` exigem `Ingrediente` (2–120); `DadosCadastrais` tem
  campos definidos pela Spec 030. Campo ausente devolve 400.
- RF3: quem solicita: `Merendeira` (escola própria, tipos de cardápio/ingrediente),
  `Responsavel` (aluno vinculado, tipos de restrição e dados cadastrais),
  `AdminEscola` (escola própria), `Nutricionista` (própria unidade).
  `AdminSecretaria` não solicita: decide, quando o tipo é de restrição ou cadastral.
- RF4: transições: `Aberta → EmAnalise`, `EmAnalise → Aprovada`, `EmAnalise →
  Recusada`, `Aprovada → Concluida`, `Aberta → Cancelada`. Qualquer outra → 409.
- RF5: a decisão é separada por natureza do pedido, sempre na própria unidade:
  `Nutricionista` decide `MudancaCardapio`, `Ingrediente` e `FaltaIngrediente`;
  `AdminSecretaria` decide `InclusaoAlergia`, `AlteracaoRestricao` e `DadosCadastrais`.
  Cada decisor executa `EmAnalise`, `Aprovada`, `Recusada` e `Concluida`; decisão por
  papel diferente do dono do tipo devolve 403. O solicitante cancela apenas em `Aberta`.
  Aprovação e recusa exigem `resposta` (2–500).
- RF6: aprovar `InclusaoAlergia` ou `AlteracaoRestricao` (decisão do ADM) aplica a
  mudança no aluno pela regra da Spec 010 (catálogo + histórico), na mesma transação da
  decisão. Se a restrição não existir no catálogo, a aprovação falha com 400 e nada muda.
- RF7: aprovar `MudancaCardapio` **não** altera o cardápio: registra a decisão e o
  cardápio é editado pela Spec 015. A resposta deixa isso explícito.
- RF8: `GET /api/solicitacoes` filtra por `tipo`, `status`, `unidadeId`, `escolaId`,
  `alunoId`, `cardapioId`, `de`, `ate`, `pagina`, `tamanho` (1–100) e aplica escopo do
  papel no banco. Filtro de escopo fora do alcance da sessão devolve 403.
- RF9: `GET /api/solicitacoes/{id}` devolve detalhe com alvo resolvido (aluno,
  cardápio ou ingrediente) e histórico de status com autor e resposta.
- RF10: solicitante vê as próprias solicitações; `Responsavel` vê também as dos alunos
  vinculados; `AdminEscola` e `Merendeira` veem as da própria escola; `Nutricionista` e
  `AdminSecretaria` veem as da própria unidade. Nenhum papel enxerga outra unidade.
- RF11: o frontend exibe fila de decisão para a nutrição (cardápio/ingrediente) e para
  o ADM (restrição e dados cadastrais), com filtro por status, e acompanhamento com
  histórico para solicitante, escola e merendeira.
- RF12: nenhuma solicitação é apagada; cancelamento é status.

## Requisitos não funcionais

- RNF1: invariantes e transições no agregado (ADR-004); controller sem regra.
- RNF2: histórico append-only gravado na mesma transação da transição.
- RNF3: snapshots de solicitante e de nomes de aluno/cardápio/restrição no evento.
- RNF4: consultas paginadas e indexadas por `(UnidadeId, CriadoEmUtc)`,
  `(EscolaId, CriadoEmUtc)` e `(Status, Tipo)`.
- RNF5: contrato `{ erro }` em 400/403/404/409; recurso de outra unidade responde 404.

## Restrições

- Migration nova `Solicitacoes`.
- Não cria endpoint de edição de cardápio nem de aluno; reutiliza 012/015 e 010.
- Nenhuma dependência nova no frontend.

## Dependências

- Specs 009, 010, 012 e 015 implementadas (015 para o alvo de `MudancaCardapio`).
- Specs 027 (hierarquia), 028 (matriz) e 030 (tipo `DadosCadastrais`) implementadas.
- Spec 018 consome os eventos de status.

## Casos de borda

- `RestricaoNome` fora do catálogo na aprovação → 400, solicitação permanece
  `EmAnalise`.
- `CardapioId` de outra unidade → 404; da mesma unidade fora do escopo → 403.
- `Nutricionista` tentando decidir restrição ou dado cadastral → 403.
- `AdminSecretaria` tentando decidir cardápio ou ingrediente → 403.
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
- [ ] Nutrição decide cardápio/ingrediente; ADM decide restrição/dados; papel trocado
  devolve 403.
- [ ] ADM aprova a restrição e ela aparece no aluno, com evento no histórico dele.
- [ ] Aprovar restrição inexistente devolve 400 e mantém `EmAnalise`.
- [ ] Aprovar mudança de cardápio não altera o cardápio.
- [ ] Escopo de listagem comprovado por papel, por escola e por unidade.
- [ ] Transição inválida devolve 409.
- [ ] Duas decisões concorrentes: uma 200 e uma 409.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Abrir uma solicitação de restrição (pai), uma de ingrediente (merendeira) e uma de
cardápio (escola); decidir restrição com o ADM e cardápio/ingrediente com a nutrição;
confirmar efeito no aluno, ausência de efeito no cardápio e histórico em todos os casos. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.

## Revisão — 2026-11-04

Conformidade com as Specs 027, 028 e 030: escopo por escola e unidade, `EscolaId`
derivado e decisão separada por tipo (nutrição para cardápio/ingrediente, ADM para
restrição e dados cadastrais). O sexto tipo delega payload e aplicação à Spec 030 para
não declarar a mesma funcionalidade em duas specs.
