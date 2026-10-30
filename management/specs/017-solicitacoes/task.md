---
name: solicitacoes
status: created
references: V6, ADR-004, L7
---

# Tasks — Solicitações

## Pré-condição

- [ ] T0: Specs 009, 010, 012 e 015 implementadas.
  - Validação: `dotnet test` verde; aluno com restrições e cardápio publicado
  consultáveis.

## Domínio

- [ ] T1: criar enums, `HistoricoSolicitacao` e `Solicitacao` com validação de campos
  por tipo.
  - Validação: testes de cada tipo com campo obrigatório ausente.
- [ ] T2: implementar as cinco transições com tabela explícita e validação de
  resposta.
  - Validação: testes de transições válidas/inválidas e de resposta curta/longa.

## Persistência

- [ ] T3: mapear `Solicitacoes` e `HistoricoSolicitacoes`, com índices e
  `[ConcurrencyCheck]` em `Status`; migration `Solicitacoes`.
  - Validação: `dotnet ef migrations add Solicitacoes`; teste de modelo confirma
  índices.
- [ ] T4: criar `ISolicitacaoRepository`/`SolicitacaoRepository` com `Listar(filtro,
  escopo)`, `ObterComHistorico` e `Salvar`.
  - Validação: teste em SQLite confirma escopo de responsável por vínculo.

## Aplicação e API

- [ ] T5: criar `SolicitacaoService` com autorização de criação por tipo/papel,
  decisão exclusiva de nutrição e transação explícita.
  - Validação: testes de 403 por escopo e de decisão por papel não autorizado.
- [ ] T6: implementar o efeito por tipo, chamando `AlunoService.AdicionarRestricao`
  nos tipos de restrição e não tocando no cardápio.
  - Validação: teste de restrição aplicada; teste de restrição inexistente → 400 e
  status `EmAnalise`; teste de cardápio inalterado.
- [ ] T7: criar `SolicitacaoController` com as rotas e políticas do design.
  - Validação: testes HTTP 200/400/403/404/409 e concorrência.

## Frontend

- [ ] T8: criar telas de solicitação e acompanhamento para merendeira, pais e escola.
  - Validação: `npm run build`; cada papel cria o tipo permitido e vê só o seu escopo.
- [ ] T9: criar `FilaSolicitacoes.tsx` para nutrição, com resposta obrigatória em
  aprovar/recusar.
  - Validação: decisão aparece no histórico do solicitante.

## Fechamento

- [ ] T10: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T11: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Cinco tipos de solicitação com campos, escopo, decisão e histórico funcionando.
- Aprovação de restrição com efeito comprovado; aprovação de cardápio sem efeito
  automático.
- Gates verdes.
