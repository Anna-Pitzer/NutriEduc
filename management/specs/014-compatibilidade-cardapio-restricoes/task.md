---
name: compatibilidade-cardapio-restricoes
status: created
references: V6, ADR-004, L5
---

# Tasks — Compatibilidade entre cardápio e restrições

## Pré-condição

- [ ] T0: Specs 009, 010, 011 e 012 implementadas.
  - Validação: `dotnet test` verde; cardápio com refeições e itens consultável.

## Domínio

- [ ] T1: criar os records de entrada e saída (`ItemAnalisado`, `VinculoAnalisado`,
  `AlunoAnalisado`, `AchadoCompatibilidade`, `ResultadoCompatibilidade`).
  - Validação: compila; nenhum tipo de EF vaza para `Domain/Services`.
- [ ] T2: implementar `AnaliseCompatibilidade.Analisar` com severidade, agrupamento
  por restrição e ordenação com desempate.
  - Validação: testes de bloqueante/alerta/permitido e de dois alunos na mesma
  restrição.
- [ ] T3: implementar o caso de unidade sem alunos e o de cardápio sem itens.
  - Validação: testes de `compativel = true` com aviso e de listas vazias.

## Aplicação e API

- [ ] T4: criar `CompatibilidadeService` com as três consultas e o escopo por papel.
  - Validação: teste de 403 para merendeira de outra unidade e para responsável.
- [ ] T5: adicionar `GET /api/cardapios/{id}/compatibilidade` em `CardapioController`.
  - Validação: testes HTTP de contrato, 404 e ordenação estável em duas chamadas.

## Frontend

- [ ] T6: adicionar painel de compatibilidade em `DetalheCardapio.tsx` com
  bloqueantes, alertas, permitidos e não permitidos.
  - Validação: `npm run build`; cardápio com bloqueante exibe selo e lista.
- [ ] T7: exibir selo de incompatibilidade na listagem de cardápios.
  - Validação: listagem não recalcula nada; consome apenas a contagem da API.

## Fechamento

- [ ] T8: atualizar a tabela de endpoints do `README.md`.
  - Validação: rota documentada coincide com o controller.
- [ ] T9: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Regra de severidade em um único lugar, testada e reutilizada por 013 e 015.
- Painel de compatibilidade funcional e sem cálculo no frontend.
- Gates verdes.
