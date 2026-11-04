---
name: solicitacoes
status: created
references: V6, V17, V19, ADR-020, ADR-021, ADR-004, L7
---

# Tasks — Solicitações

## Pré-condição

- [ ] T0: Specs 009, 010, 012, 015, 027, 028 e 030 implementadas.
  - Validação: `dotnet test` verde; aluno com restrições, cardápio publicado e
  tipo `DadosCadastrais` disponíveis.

## Domínio

- [ ] T1: criar enums (incluindo `DadosCadastrais`), `HistoricoSolicitacao` e
  `Solicitacao` com validação de campos por tipo e `EscolaId` derivado.
  - Validação: testes de cada tipo com campo obrigatório ausente e de `EscolaId`
  derivado do solicitante e do aluno.
- [ ] T2: implementar as transições com tabela explícita e validação de resposta.
  - Validação: testes de transições válidas/inválidas e de resposta curta/longa.

## Persistência

- [ ] T3: mapear `Solicitacoes` e `HistoricoSolicitacoes`, com `EscolaId`, índices e
  `[ConcurrencyCheck]` em `Status`; migration `Solicitacoes`.
  - Validação: `dotnet ef migrations add Solicitacoes`; teste de modelo confirma
  índices.
- [ ] T4: criar `ISolicitacaoRepository`/`SolicitacaoRepository` com `Listar(filtro,
  escopo)`, `ObterComHistorico` e `Salvar`.
  - Validação: teste em SQLite confirma escopo de responsável por vínculo.

## Aplicação e API

- [ ] T5: criar `SolicitacaoService` com autorização de criação por tipo/papel,
  decisão separada por tipo (nutrição para cardápio/ingrediente, ADM para restrição e
  dados cadastrais) e transação explícita.
  - Validação: testes de 403 por escopo e de decisão por papel que não é dono do tipo.
- [ ] T6: implementar o efeito por tipo, chamando `AlunoService.AdicionarRestricao`
  nos tipos de restrição e não tocando no cardápio.
  - Validação: teste de restrição aplicada; teste de restrição inexistente → 400 e
  status `EmAnalise`; teste de cardápio inalterado.
- [ ] T7: criar `SolicitacaoController` com as rotas e políticas do design.
  - Validação: testes HTTP 200/400/403/404/409 e concorrência.

## Frontend

- [ ] T8: criar telas de solicitação e acompanhamento para merendeira, pais e escola.
  - Validação: `npm run build`; cada papel cria o tipo permitido e vê só o seu escopo.
- [ ] T9: criar `FilaSolicitacoes.tsx` para nutrição (cardápio/ingrediente) e a fila
  de restrição/dados cadastrais para o ADM, com resposta obrigatória em aprovar/recusar.
  - Validação: decisão aparece no histórico do solicitante e cada fila mostra apenas
  os tipos do papel.

## Conformidade

- [ ] T10: cobrir as células da matriz da Spec 028 para solicitações (ADM `R + decide`,
  Escola `RC`, Nutricionista `RW`, Merendeira `RC`, Responsável `RC`).
  - Validação: `MatrizPermissoesTests` passa para o recurso `Solicitacao`; filtro de
  `escolaId`/`unidadeId` fora do escopo devolve 403.

## Fechamento

- [ ] T11: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Seis tipos de solicitação com campos, escopo, decisão e histórico funcionando.
- Aprovação de restrição pelo ADM com efeito comprovado; cardápio sem efeito
  automático; dados cadastrais seguindo a Spec 030.
- Gates verdes.
