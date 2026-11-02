---
name: dominio-cardapio
status: created
references: V6, ADR-004, L10
---

# Tasks — Domínio de cardápio

## Pré-condição

- [ ] T0: Specs 009 e 011 implementadas.
  - Validação: `dotnet test` verde; `GET /api/alimentos/{id}` devolve `valores`.

## Domínio

- [ ] T1: criar `PeriodoVigencia` com construtor sem parâmetros para o EF, setters
  privados e as duas validações (fim anterior, 366 dias inclusive).
  - Validação: testes de período invertido, 366 aceito e 367 recusado.
- [ ] T2: criar `ItemRefeicao` (gramas via `ValidacaoDeCampo.Medida`, limite 5000) e
  `Refeicao` (nome, ordem, nomes distintos sem caixa, máximo 50 itens,
  `Refeicao.Copiar`).
  - Validação: testes de 0 g, 5000 g, 5001 g, 3 casas, alimento repetido, 51 itens,
    ordem repetida e nome repetido.
- [ ] T3: criar `Cardapio` com `Validar`/`Aplicar` (valida antes de mutar), máximo 10
  refeições, `Situacao = Rascunho` na criação e `Cardapio.Copiar`.
  - Validação: teste de `Atualizar` inválido não altera período nem refeições;
  - Validação: teste de `Atualizar` em publicado lança `InvalidOperationException`;
  - Validação: teste de `Copiar` gera composição igual com instâncias novas.
- [ ] T4: criar `CalculoNutricional` (`DoItem` via `ValoresNutricionais.Criar`,
  `Somar`, `AwayFromZero`).
  - Validação: testes de 30 g/100 g/250 g e de soma de duas refeições.

## Persistência

- [ ] T5: mapear `Cardapio`, `Refeicao` e `ItemRefeicao`: `Periodo` como tipo
  complexo, chave composta `(RefeicaoId, AlimentoId)`, cascatas, índices e leitura de
  coleção por campo; migration `DominioCardapio`.
  - Validação: teste de modelo confirma tipo complexo, chave composta e índices;
  - Validação: `dotnet ef migrations add DominioCardapio` sem erro.
- [ ] T6: criar `ICardapioRepository`/`CardapioRepository` com `Listar(filtro)`,
  `ListarPublicados(filtro)`, `ObterComRefeicoes`, `ExisteCardapioDaUnidade`, `Add`,
  `Update` e `Delete`.
  - Validação: teste em SQLite confirma filtro por data, por refeição e por situação.
- [ ] T7: acrescentar `ObterPorIds` a `IAlimentoRepository` e ao repositório (leitura em
  lote de alimentos com valores).
  - Validação: teste confirma uma consulta para N ids e ausência de itens faltantes.

## Exclusão de unidade

- [ ] T8: acrescentar `temCardapios` a `PoliticaDeExclusao.ParaUnidade` e a leitura em
  `ExclusaoService`.
  - Validação: teste de domínio do impedimento e teste HTTP de exclusão de unidade com
  cardápio devolvendo 409.

## Aplicação e API

- [ ] T9: criar `CardapioService` com escopo, validação de unidade existente, regra de
  alimento inativo (só se já estava no cardápio), montagem dos totais e cópia.
  - Validação: testes de serviço para alimento inativo novo, duplicação, exclusão de
  publicado e merendeira de outra unidade.
- [ ] T10: criar `CardapioController` com as 6 rotas internas e a rota pública anônima.
  - Validação: testes HTTP 201/400/403/404/409 e da rota pública sem sessão;
  - Validação: rota pública sem `unidadeId` devolve 400.

## Frontend

- [ ] T11: reescrever `CardapioApi.ts` com os novos tipos e as 7 funções; remover
  `buscarCardapio`, `buscarHistoricoCardapios`, `salvarCardapio` e os tipos antigos.
  - Validação: `npm run build` após ajustar os consumidores que quebrarem.
- [ ] T12: criar `ListarCardapios`, `FormularioCardapio` e `DetalheCardapio`; remover
  `Cardapio.tsx`, `MontarCardapio.tsx` e `components/CardapiosDiv.tsx`; atualizar
  `routes.tsx`, `Navbar.tsx` e `HomeNutricao`.
  - Validação: `npm run build` e `npm run lint`; montar, salvar, editar, duplicar e
  excluir pela interface;
  - Validação: `grep` confirma que nenhuma rota de `/api/cardapios` ficou órfã no
  frontend e que `CardapiosDiv` não tem referência restante.
- [ ] T13: implementar leitura de publicados em `pages/pais/` e `pages/merendeiras/`,
  enviando o `unidadeId` do usuário.
  - Validação: filtro por dia e por refeição funciona; build e lint verdes.

## Fechamento

- [ ] T14: atualizar a tabela de endpoints do `README.md` com as 7 rotas.
  - Validação: `DocumentationTests` verde (compara README com os atributos dos
  controllers).
- [ ] T15: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- CRUD de cardápio, duplicação, cálculo e leitura pública funcionando sobre o banco.
- Nenhum vestígio do fluxo de `localStorage` nas telas de nutrição.
- Nenhuma rota de cardápio órfã no frontend.
- Exclusão de unidade com cardápio devolve 409, não 500.
- Gates verdes.
