---
name: ocorrencias
status: created
references: V6, ADR-004, L7
---

# Tasks — Ocorrências

## Pré-condição

- [ ] T0: Specs 009 e 012 implementadas.
  - Validação: `dotnet test` verde; escopo por papel disponível em `EscopoService`.

## Domínio

- [ ] T1: criar `TipoOcorrencia`, `StatusOcorrencia`, `HistoricoOcorrencia` e
  `Ocorrencia` com `ValidarVinculos`.
  - Validação: testes de descrição 4/1001 e de vínculo incoerente.
- [ ] T2: implementar `MudarStatus` e `Cancelar` com a tabela de transições.
  - Validação: testes de cada transição válida e inválida; teste de cancelamento
  fora do dia.

## Persistência

- [ ] T3: mapear `Ocorrencias` e `HistoricoOcorrencias` com FKs `SetNull`, cascata e
  índices; migration `Ocorrencias`.
  - Validação: `dotnet ef migrations add Ocorrencias`; teste de modelo confirma as
  FKs e índices.
- [ ] T4: criar `IOcorrenciaRepository`/`OcorrenciaRepository` com `Listar(filtro,
  escopo)`, `ObterComHistorico` e `Salvar`.
  - Validação: teste em SQLite confirma filtro por vínculo de responsável.

## Aplicação e API

- [ ] T5: criar `OcorrenciaService` com autorização de criação por vínculo e escopo
  de listagem.
  - Validação: testes de 403 para aluno não vinculado e para unidade alheia.
- [ ] T6: criar `OcorrenciaController` com as rotas e políticas do design.
  - Validação: testes HTTP de 200/400/403/404/409.

## Frontend

- [ ] T7: criar formulário e listagem da merendeira, dos pais e da escola.
  - Validação: `npm run build` e `npm run lint`; registrar e acompanhar pela
  interface em cada papel.
- [ ] T8: adicionar link "Registrar ocorrência" no detalhe do aluno (nutrição).
  - Validação: formulário abre com o aluno pré-selecionado.

## Fechamento

- [ ] T9: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T10: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Registro, acompanhamento e histórico de ocorrências funcionando para os três
  papéis, com escopo comprovado por teste HTTP.
- Gates verdes.
