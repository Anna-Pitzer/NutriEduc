---
name: solicitacao-alteracao-dados
status: created
references: V19, ADR-016, L9
---

# Tasks — Solicitação de alteração de dados do aluno

## Pré-condição

- [ ] T0: Spec 017 implementada (agregado e transições) e Spec 023 implementada (trilha do
  aluno); Spec 028 implementada (escopo de criação e decisão).
  - Validação: solicitação existente cria e decide; trilha do aluno grava evento com origem.

## Domínio

- [ ] T1: acrescentar `DadosCadastrais` em `TipoSolicitacao` e criar `CampoDeDadoDoAluno`.
  - Validação: enum fechado recusa campo desconhecido na desserialização.
- [ ] T2: criar `AlteracaoDeDado` imutável e as invariantes do tipo em `Solicitacao`
  (alterações não vazias, campos distintos, justificativa 10–500).
  - Validação: testes de construtor para cada invariante.
- [ ] T3: implementar `Aprovar(alunoAtual)` compondo o estado final e reutilizando as regras de
  `Aluno`, com exceção de divergência contendo os campos afetados.
  - Validação: campo inválido não altera nada; divergência lança sem mutar.

## Persistência

- [ ] T4: criar a migration `SolicitacaoDadosCadastrais` com a tabela de alterações e os
  índices.
  - Validação: `Cascade` remove alterações junto com a solicitação; consulta de duplicidade
    usa o índice.
- [ ] T5: registrar a consulta de pedido aberto por `(aluno, campo)`.
  - Validação: segundo pedido do mesmo campo é encontrado antes da gravação.

## Aplicação e API

- [ ] T6: criar `SolicitarAlteracaoDeDadosService` com escopo do responsável, aluno ativo,
  snapshot e bloqueio de duplicidade.
  - Validação: 403 sem vínculo, 409 duplicado, snapshot gravado.
- [ ] T7: criar `DecidirSolicitacaoDeDadosService` com comparação snapshot × atual, aplicação
  atômica, trilha na mesma transação e notificação.
  - Validação: `Aprovar_AplicaTodosOsCampos_EmUmaTransacao`,
  `Aprovar_ValorDivergente_409SemEscrita` e `Trilha_EventoComOrigemDaSolicitacao` verdes.
- [ ] T8: ampliar `SolicitacaoController` para o novo tipo e para as ações analisar, aprovar,
  recusar e cancelar.
  - Validação: testes HTTP de 400/403/409 e do corpo `{ erro }`.
- [ ] T9: aplicar `ConcurrencyCheck` na solicitação (ADR-010) para aprovação concorrente.
  - Validação: duas aprovações simultâneas resultam em uma 200 e uma 409.

## Frontend

- [ ] T10: tela do responsável para escolher filho e campo, enviar justificativa e acompanhar
  status; fila do ADM com diff, aprovar e recusar com motivo.
  - Validação: `npm run build` e `npm run lint` sem erro novo; fluxo manual completo.

## Fechamento

- [ ] T11: atualizar `README.md` e a seção da spec em `arch.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [ ] T12: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- Todo pedido de dados cadastrais passa por análise do ADM; nenhum é aplicado parcialmente.
- Toda aplicação tem evento na trilha com autor e origem.
- Divergência de valor e duplicidade de pedido têm resposta explícita e acionável.
