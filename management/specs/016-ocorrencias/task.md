---
name: ocorrencias
status: created
references: V6, V16, V17, ADR-020, ADR-021, ADR-004, L7
---

# Tasks — Ocorrências

## Pré-condição

- [ ] T0: Specs 009, 012, 027 e 028 implementadas.
  - Validação: `dotnet test` verde; escopo por unidade/escola/vínculo disponível em
  `EscopoService`.

## Domínio

- [ ] T1: criar `TipoOcorrencia`, `StatusOcorrencia`, `HistoricoOcorrencia` e
  `Ocorrencia` com `ValidarVinculos` e `EscolaId` derivado.
  - Validação: testes de descrição 4/1001, de vínculo incoerente e de `EscolaId`
  derivado do aluno e do autor.
- [ ] T2: implementar `MudarStatus` e `Cancelar` com a tabela de transições.
  - Validação: testes de cada transição válida e inválida; teste de cancelamento
  fora do dia.

## Persistência

- [ ] T3: mapear `Ocorrencias` e `HistoricoOcorrencias` com FKs `SetNull`, cascata,
  `EscolaId` e índices; migration `Ocorrencias`.
  - Validação: `dotnet ef migrations add Ocorrencias`; teste de modelo confirma as
  FKs e índices.
- [ ] T4: criar `IOcorrenciaRepository`/`OcorrenciaRepository` com `Listar(filtro,
  escopo)`, `ObterComHistorico` e `Salvar`.
  - Validação: teste em SQLite confirma filtro por vínculo de responsável.

## Aplicação e API

- [ ] T5: criar `OcorrenciaService` com autorização de criação por vínculo, derivação
  de `EscolaId`/`UnidadeId` e escopo de listagem por escola/unidade.
  - Validação: testes de 403 para aluno não vinculado e para escola alheia; 404 para
  outra unidade.
- [ ] T6: criar `OcorrenciaController` com as rotas e políticas do design.
  - Validação: testes HTTP de 200/400/403/404/409.

## Frontend

- [ ] T7: criar formulário e listagem da merendeira, dos pais e da escola.
  - Validação: `npm run build` e `npm run lint`; registrar e acompanhar pela
  interface em cada papel.
- [ ] T8: adicionar link "Registrar ocorrência" no detalhe do aluno (nutrição).
  - Validação: formulário abre com o aluno pré-selecionado.

## Conformidade

- [ ] T9: cobrir as células da matriz da Spec 028 para ocorrências (ADM `R`, Escola
  `RW`, Nutricionista `RW`, Merendeira `RC`, Responsável `RC`).
  - Validação: `MatrizPermissoesTests` passa para o recurso `Ocorrencia`; filtro de
  `escolaId`/`unidadeId` fora do escopo devolve 403.

## Fechamento

- [ ] T10: atualizar a tabela de endpoints do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T11: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Registro, acompanhamento e histórico de ocorrências funcionando para os três
  papéis, com escopo comprovado por teste HTTP.
- Nenhuma ocorrência de outra unidade ou de outra escola é visível.
- Gates verdes.
