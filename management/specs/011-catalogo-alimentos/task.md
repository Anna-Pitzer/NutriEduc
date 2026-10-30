---
name: catalogo-alimentos
status: created
references: V6, ADR-004, L2, L9
---

# Tasks — Catálogo de alimentos

## Pré-condição

- [ ] T0: Specs 009, 010 e 007 implementadas.
  - Validação: `dotnet test` verde; `/api/restricoes` responde com catálogo.

## Domínio

- [ ] T1: criar `ValoresNutricionais` como `record` com `Criar` e as três validações
  (negativo, casas decimais, obrigatoriedade).
  - Validação: testes de valor negativo, 3 casas e valor zero.
- [ ] T2: criar `CategoriaAlimento` com nome normalizado, ativo e
  `Desativar`/`Reativar`.
  - Validação: testes de limite 2–60 e de nome duplicado (serviço).
- [ ] T3: criar `AlimentoRestricao` e `Alimento` com `_vinculos` protegidos,
  `ValidarDados` compartilhado entre construtor e `AtualizarDados`.
  - Validação: testes de construtor inválido sem atribuição parcial;
  - Validação: teste de vínculo duplicado; teste de `AtualizarDados` válido.

## Persistência

- [ ] T4: mapear `ValoresNutricionais` como tipo complexo, `Alimento` com índice
  único em `NomeNormalizado` e `AlimentoRestricao` com chave composta.
  - Validação: `dotnet ef migrations add CatalogoAlimentos`; teste de modelo
  confirma colunas `decimal(10,2)` e chave composta.
- [ ] T5: criar `IAlimentoRepository`/`AlimentoRepository` com `Listar(filtro)`,
  `ObterComVinculos`, `ExisteNomeNormalizado` e `Salvar`.
  - Validação: teste em SQLite confirma busca case-insensitive e paginação.

## Aplicação e API

- [ ] T6: criar `AlimentoService` validando categoria ativa, restrições existentes e
  duplicidade de nome antes de chamar o domínio.
  - Validação: testes de serviço para 400/409 sem persistência.
- [ ] T7: criar `AlimentoController` e `CategoriaAlimentoController` com as rotas e
  políticas do design.
  - Validação: testes HTTP de 201/400/403/404/409 e do formato `{ erro }`.
- [ ] T8: implementar `incluirInativos` com 403 fora de `Nutricionista`.
  - Validação: teste HTTP por papel.

## Frontend

- [ ] T9: criar `AlimentoApi.ts` com as rotas reais e remover as chamadas órfãs de
  `/api/alimentos` de `CardapioApi.ts`.
  - Validação: `grep` não encontra `/api/alimentos` em `CardapioApi.ts`.
- [ ] T10: criar `pages/nutricao/alimentos/` com listagem, busca, filtro de categoria
  e formulário com os seis valores e vínculos.
  - Validação: `npm run build` e `npm run lint`; criar, editar e desativar pela
  interface.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com os controllers.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Catálogo de alimentos funcional ponta a ponta, com composição e vínculos.
- Nenhuma rota órfã de alimento no frontend.
- Gates verdes.
