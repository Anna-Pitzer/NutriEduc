---
name: dominio-cardapio
status: created
references: V6, ADR-004, L10
---

# Tasks — Domínio de cardápio

## Pré-condição

- [ ] T0: Specs 009 e 011 implementadas.
  - Validação: `dotnet test` verde; `/api/alimentos/{id}` devolve valores nutricionais.

## Domínio

- [ ] T1: criar `PeriodoVigencia` (tipo complexo) com as duas validações de período.
  - Validação: testes de período invertido, 366 dias e 367 dias.
- [ ] T2: criar `ItemRefeicao` e `Refeicao` com quantidade, ordem e unicidade de
  alimento por refeição.
  - Validação: testes de 0 g, 5000 g, 5001 g, 3 casas e alimento repetido.
- [ ] T3: criar `Cardapio` com `_refeicoes` protegidas, `Atualizar` atômico e
  `Situacao = Rascunho` na criação.
  - Validação: teste de `Atualizar` inválido não altera nenhuma refeição;
  - Validação: teste de `Atualizar` em publicado lança `InvalidOperationException`.
- [ ] T4: criar `CalculoNutricional` com `DoItem`, `Somar` e arredondamento
  `AwayFromZero`.
  - Validação: testes de 30 g/100 g/250 g e de soma de duas refeições.

## Persistência

- [ ] T5: mapear `Cardapio`, `Refeicao` e `ItemRefeicao` com cascatas e índices;
  migration `DominioCardapio`.
  - Validação: `dotnet ef migrations add DominioCardapio`; teste de modelo confirma
  as chaves e o tipo complexo `Periodo`.
- [ ] T6: criar `ICardapioRepository`/`CardapioRepository` com `Listar(filtro)`,
  `ListarPublicados(filtro)`, `ObterComRefeicoes` e `Salvar`.
  - Validação: teste em SQLite confirma filtro por data e por refeição.

## Aplicação e API

- [ ] T7: criar `CardapioService` com escopo, montagem do agregado e cálculo.
  - Validação: testes de serviço para alimento inativo, duplicação e exclusão de
  publicado.
- [ ] T8: criar `CardapioController` com as rotas internas e a rota pública anônima.
  - Validação: testes HTTP 201/400/403/404/409 e da rota pública sem sessão.

## Frontend

- [ ] T9: ligar `CardapioApi.ts` às rotas reais e remover as chamadas inexistentes.
  - Validação: `grep` de `/api/cardapios` confirma que todas as rotas têm controller.
- [ ] T10: criar `ListarCardapios`, `FormularioCardapio` e `DetalheCardapio` em
  `pages/nutricao/cardapios/`.
  - Validação: `npm run build` e `npm run lint`; montar, salvar e duplicar pela
  interface.
- [ ] T11: implementar leitura de publicados em `pages/pais/` e `pages/merendeiras/`.
  - Validação: filtro por dia e por refeição funciona sem sessão.

## Fechamento

- [ ] T12: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T13: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- CRUD de cardápio, duplicação, cálculo e leitura pública funcionando.
- Nenhuma rota de cardápio órfã no frontend.
- Gates verdes.
