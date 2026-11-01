---
name: dominio-cardapio
status: created
references: V6, ADR-004, L10
---

# Requirements — Domínio de cardápio

## Objetivo

Cardápio é o centro do produto e não existe no backend: `frontend/src/services/CardapioApi.ts`
chama três rotas de `/api/cardapios` que respondem 404, e as telas
`pages/nutricao/cardapios/Cardapio.tsx` e `MontarCardapio.tsx` gravam o cardápio em
`localStorage`. Esta spec cria o agregado, as refeições, os itens, o período de validade
e o cálculo nutricional — a base que as specs 013, 014 e 015 estendem — e substitui o
fluxo de `localStorage` por persistência real.

## Escopo

- Backend: `Cardapio`, `Refeicao`, `ItemRefeicao`, `PeriodoVigencia`, cálculo
  nutricional, CRUD, duplicação e leitura pública de publicados.
- Frontend: `pages/nutricao/cardapios/` (lista, formulário, detalhe) e leitura por
  pais e merendeiras.
- Fora de escopo: geração automática (Spec 013), compatibilidade com restrições
  (Spec 014), publicação, versionamento e histórico (Spec 015), notificações
  (Spec 018).

### Cobertura de `implementacoes.md`

- **Nutrição (10):** criar, editar, excluir/cancelar, visualizar, duplicar, definir
  validade, definir refeições, adicionar e remover alimentos, calcular valores
  nutricionais.
- **Pais (7):** verificar cardápios, pesquisar, filtrar por unidade, por dia, por
  refeição, visualizar informações dos alimentos e valores nutricionais.
- **Merendeiras (3):** verificar cardápios, por dia e por refeição (em modo leitura).

Não é coberto nesta spec: filtrar **por escola**. O cardápio pertence a uma
`UnidadeEscolar` e não existe relação cardápio↔escola no modelo (Spec 009). Criar essa
relação é decisão de outra spec, não um filtro desta.

## Requisitos funcionais

- RF1: `Cardapio` tem `Nome` (2–120), `UnidadeId` (obrigatório, unidade existente),
  `Periodo` (`Inicio`/`Fim`) e `Situacao = Rascunho` na criação. O período aceita
  `Fim >= Inicio` e no máximo **366 dias inclusive**.
- RF2: `Cardapio` contém de zero a **10** refeições; cada refeição tem `Nome` (2–60),
  `Ordem` (1–10, única no cardápio) e de zero a **50** itens. Duas refeições do mesmo
  cardápio não podem ter nomes iguais na comparação sem caixa.
- RF3: `ItemRefeicao` referencia um `Alimento` (Spec 011) e uma quantidade em gramas
  (`> 0`, até 5000, no máximo duas casas). O mesmo alimento não pode aparecer duas
  vezes na mesma refeição; em refeições diferentes, pode.
- RF4: só é possível adicionar alimento ativo. Alimento desativado depois de
  adicionado permanece no cardápio e continua sendo calculado — isto é, ao reeditar,
  um item cujo alimento já estava no cardápio é aceito mesmo inativo; um alimento
  inativo que não estava lá é 400.
- RF5: o cálculo nutricional é derivado, nunca persistido: por item,
  `valores do alimento × gramas / 100`, arredondado a duas casas com
  `MidpointRounding.AwayFromZero`; por refeição e por cardápio, a soma dos itens **já
  arredondados**. Existe uma única implementação (`CalculoNutricional`), reutilizada
  por todos os endpoints.
- RF6: `POST /api/cardapios/{id}/duplicar` cria um novo cardápio `Rascunho`, com as
  mesmas refeições e itens, exigindo novo período no corpo, e registra
  `CardapioDuplicadoDeId`. A cópia é uma única fábrica de domínio, também usada pela
  revisão da Spec 015.
- RF7: `DELETE /api/cardapios/{id}` só é permitido em `Rascunho`; publicado ou
  cancelado devolve 409. A exclusão é física e remove refeições e itens em cascata.
  Consequência aceita e registrada na Spec 015: o histórico de um rascunho apagado
  desaparece com ele; o append-only vale para cardápios que chegaram a `Publicado`.
- RF8: criar, editar, duplicar e excluir é exclusivo de `Nutricionista`. Leitura
  interna é de `Nutricionista` (rede) e `Merendeira` (própria unidade). `AdminEscola`
  não acessa esta spec: o escopo dele é por escola e o cardápio é por unidade.
- RF9: `GET /api/cardapios/publicados` é **anônimo**, exige `unidadeId` (400 sem ele)
  e devolve apenas `Publicado` cujo período inclui a data consultada (padrão: hoje).
  Filtros opcionais: `data`, `refeicao`; resposta paginada.
- RF10: o filtro `refeicao` casa o nome da refeição **ignorando caixa**; acento
  continua significativo (`Almoço` não casa com `almoco`). O frontend envia o nome
  exato que recebeu do backend.
- RF11: a resposta de detalhe inclui refeições ordenadas por `Ordem`, itens com id e
  nome do alimento, valores por item, total por refeição e total do cardápio.
- RF12: edição de cardápio `Rascunho` substitui o conjunto de refeições de forma
  atômica: valida tudo antes de alterar qualquer estado; payload inválido não altera
  nada.
- RF13: o frontend permite listar, montar por refeição, ver o total atualizado a cada
  item, editar, duplicar e excluir rascunho. Todos os totais exibidos vêm do backend;
  o formulário não recalcula.

## Requisitos não funcionais

- RNF1: invariantes no agregado (`Cardapio`, `Refeicao`, `ItemRefeicao`,
  `PeriodoVigencia`), conforme ADR-004; controller e serviço não recalculam regra de
  formato.
- RNF2: coleções expostas como `IReadOnlyList` sobre campos privados (padrão da
  Spec 003 e da Spec 011).
- RNF3: cálculo em um único método de domínio (RF5); nenhuma segunda implementação no
  frontend (L9).
- RNF4: consultas paginadas (1–100) usando `Paginacao` (dono único criado na
  Spec 011) e filtro no banco.
- RNF5: contrato `{ erro }` em 400/404/409 (ADR-005), pelo `RespostaController`.
- RNF6: `decimal` no SQLite é gravado como REAL/INTEGER pela afinidade NUMERIC da
  coluna, sem escala nem exatidão decimal: nenhuma soma, média ou ordenação numérica
  pode depender do banco. Todo cálculo acontece em C#, sobre os valores carregados.

## Restrições

- Migration nova `DominioCardapio`.
- `Situacao` nasce com `Rascunho`, `Publicado` e `Cancelado`; apenas `Rascunho` é
  atribuível nesta spec — as transições pertencem à Spec 015.
- `UnidadeId` e `AlimentoId` usam `DeleteBehavior.Restrict`. Isso cria uma nova
  dependência de exclusão: `PoliticaDeExclusao.ParaUnidade` e `ExclusaoService`
  precisam conhecer "unidade com cardápios" nesta mesma entrega, senão excluir uma
  unidade com cardápio estoura no banco (500) em vez de 409.
- `IAlimentoRepository` ganha `ObterPorIds` (leitura em lote dos alimentos de um
  cardápio). Alteração aditiva: nenhum comportamento existente muda.
- Nenhuma dependência nova no frontend.

## Dependências

- Spec 009 (unidade e autorização) e Spec 011 (alimento, valores nutricionais e
  `Paginacao`).
- Spec 013 consome `RefeicaoPadrao(Nome, Ordem)` do `ParametroGeracao` — o formato de
  refeição desta spec é compatível por construção.
- Spec 014 é necessária para publicar, não para criar rascunho.
- Spec 015 amplia `Situacao` com `Substituido`, acrescenta `RevisaoDeId` e reutiliza a
  fábrica de cópia da RF6.

## Casos de borda

- `Fim` anterior a `Inicio` → 400.
- Período de 367 dias inclusive → 400.
- Quantidade zero, negativa, acima de 5000 ou com três casas → 400.
- Alimento inexistente ou inativo (não presente antes) → 400.
- Alimento repetido na mesma refeição → 400.
- Duas refeições com a mesma `Ordem` ou mesmo nome sem caixa → 400.
- 11 refeições ou 51 itens na mesma refeição → 400.
- Cardápio inexistente → 404.
- Excluir cardápio publicado → 409.
- Unidade sem cardápio publicado na data → lista vazia, 200.
- Cardápio sem itens → total zero, não é erro.
- `GET /api/cardapios/publicados` sem `unidadeId` → 400.
- `refeicao=almoco` com refeição nomeada `Almoço` → lista vazia, 200 (RF10).

## Critérios de aceitação

- [ ] `POST /api/cardapios` cria rascunho com período válido e devolve 201 com `Location`.
- [ ] Período inválido devolve 400 e não persiste.
- [ ] `PUT /api/cardapios/{id}` com item inválido não altera o cardápio.
- [ ] Totais por item, refeição e cardápio conferem com o cálculo manual em teste.
- [ ] Duplicar gera novo id, mesma composição e `Situacao = Rascunho`.
- [ ] `DELETE` em publicado devolve 409; em rascunho remove refeições e itens.
- [ ] `POST /api/cardapios` por `Merendeira` devolve 403.
- [ ] `GET /api/cardapios/publicados` responde sem sessão, só com publicados e só com `unidadeId`.
- [ ] Filtro por `refeicao` e por `data` retorna apenas o esperado.
- [ ] Excluir unidade com cardápio devolve 409, não 500.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Montar um cardápio com duas refeições e três alimentos, conferir totais, duplicar
para outro período, consultar a rota pública sem sessão e confirmar que rascunho não
aparece nela. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
