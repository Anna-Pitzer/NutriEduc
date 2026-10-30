---
name: dominio-cardapio
status: created
references: V6, ADR-004, L10
---

# Requirements — Domínio de cardápio

## Objetivo

Cardápio é o centro do produto e não existe no código: `frontend/src/services/CardapioApi.ts`
chama seis rotas de `/api/cardapios` que respondem 404. Esta spec cria o agregado,
as refeições, os itens, o período de validade e o cálculo nutricional — a base que
as specs 013, 014 e 015 estendem.

## Escopo

- Backend: `Cardapio`, `Refeicao`, `ItemRefeicao`, período de validade, cálculo
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
- **Pais (7):** verificar cardápios, pesquisar, filtrar por escola/unidade, por dia,
  por refeição, visualizar informações dos alimentos e valores nutricionais.
- **Merendeiras (3):** verificar cardápios, por dia e por refeição (em modo leitura).

## Requisitos funcionais

- RF1: `Cardapio` tem `Nome` (2–120), `UnidadeId` (obrigatório), período
  `DataInicio`/`DataFim` (`DataFim >= DataInicio`, máximo 366 dias) e `Situacao`
  `Rascunho` na criação.
- RF2: `Cardapio` contém zero ou mais `Refeicao`; cada refeição tem nome (2–60),
  ordem única dentro do cardápio e zero ou mais itens.
- RF3: `ItemRefeicao` referencia um `Alimento` (Spec 011) e uma quantidade em gramas
  (`> 0`, até 5000, duas casas). O mesmo alimento não pode aparecer duas vezes na
  mesma refeição; em refeições diferentes, pode.
- RF4: só é possível adicionar alimento ativo; alimento desativado depois de
  adicionado permanece no cardápio e continua sendo calculado.
- RF5: o cálculo nutricional é derivado, nunca persistido: por item,
  `valores do alimento × gramas / 100`; por refeição e por cardápio, a soma. Valores
  são `decimal` com duas casas, arredondamento `MidpointRounding.AwayFromZero`.
- RF6: `POST /api/cardapios/{id}/duplicar` cria um novo cardápio `Rascunho`, com as
  mesmas refeições e itens, exigindo novo período no corpo; registra
  `CardapioDuplicadoDeId`.
- RF7: `DELETE /api/cardapios/{id}` só é permitido em `Rascunho`. Cardápio publicado
  ou cancelado não é apagado (Spec 015 define cancelamento).
- RF8: criar, editar, duplicar e excluir é exclusivo de `Nutricionista`; leitura
  interna é liberada a `Nutricionista`, `Merendeira` e `AdminEscola` da unidade.
- RF9: `GET /api/cardapios/publicados` é **anônimo** e devolve apenas cardápios
  `Publicado` cujo período inclui a data consultada (padrão: hoje). Filtros:
  `escolaId`, `unidadeId`, `data`, `refeicao`.
- RF10: a resposta de detalhe inclui refeições, itens com nome do alimento, valores
  por item, totais por refeição e total do cardápio.
- RF11: edição de cardápio `Rascunho` substitui o conjunto de refeições de forma
  atômica: payload inválido não altera nada.
- RF12: frontend permite montar o cardápio por refeição, ver o total atualizado a
  cada item e duplicar a partir da listagem.

## Requisitos não funcionais

- RNF1: invariantes no agregado (`Cardapio`, `Refeicao`, `ItemRefeicao`), conforme
  ADR-004; controller e serviço não recalculam regra de formato.
- RNF2: coleções expostas como `IReadOnlyList` sobre campos privados (padrão da
  Spec 003).
- RNF3: cálculo em um único método de domínio, reutilizado por todos os endpoints;
  nenhuma segunda implementação no frontend (L9).
- RNF4: consultas paginadas (1–100) e com filtro no banco.
- RNF5: contrato `{ erro }` em 400/404/409 (ADR-005).

## Restrições

- Migration nova `DominioCardapio`.
- `Situacao` nasce com `Rascunho`, `Publicado` e `Cancelado`; apenas `Rascunho` é
  atribuível nesta spec — as transições pertencem à Spec 015.
- Nenhuma dependência nova no frontend.

## Dependências

- Spec 009 (unidade e autorização) e Spec 011 (alimento e valores nutricionais).
- Spec 014 é necessária para publicar, não para criar rascunho.

## Casos de borda

- `DataFim` anterior a `DataInicio` → 400.
- Período maior que 366 dias → 400.
- Quantidade zero, negativa, acima de 5000 ou com três casas → 400.
- Alimento inexistente ou inativo → 400.
- Alimento repetido na mesma refeição → 400.
- Duas refeições com a mesma ordem → 400.
- Cardápio inexistente → 404.
- Excluir cardápio publicado → 409 (use cancelamento, Spec 015).
- Unidade sem cardápio publicado na data → lista vazia, 200.
- Cardápio sem itens → total zero, não é erro.

## Critérios de aceitação

- [ ] `POST /api/cardapios` cria rascunho com período válido e devolve 201.
- [ ] Período inválido devolve 400 e não persiste.
- [ ] `PUT /api/cardapios/{id}` com item inválido não altera o cardápio.
- [ ] Totais por item, refeição e cardápio conferem com o cálculo manual em teste.
- [ ] Duplicar gera novo id, mesma composição e `Situacao = Rascunho`.
- [ ] `DELETE` em publicado devolve 409.
- [ ] `POST /api/cardapios` por `Merendeira` devolve 403.
- [ ] `GET /api/cardapios/publicados` responde sem sessão e só traz publicados.
- [ ] Filtro por `refeicao` e por `data` retorna apenas o esperado.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Montar um cardápio com duas refeições e três alimentos, conferir totais, duplicar
para outro período, consultar a rota pública sem sessão e confirmar que rascunho não
aparece nela. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
