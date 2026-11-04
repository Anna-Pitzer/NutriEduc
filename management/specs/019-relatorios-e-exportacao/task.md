---
name: relatorios-e-exportacao
status: created
references: V6, V16, V17, V18, ADR-020, ADR-021, ADR-022, ADR-004, L6
---

# Tasks — Relatórios e exportação

## Pré-condição

- [ ] T0: Specs 009, 010, 012, 015, 016, 017, 018, 027, 028 e 029 implementadas.
  - Validação: `dotnet test` verde; dados de cardápio, ocorrência, solicitação e
  escola consultáveis.

## Estrutura e catálogo

- [ ] T1: criar `TipoRelatorio`, `ColunaRelatorio`, `RelatorioGerado` e
  `FiltroRelatorio` com validação de período.
  - Validação: testes de `de > ate` e de 367 dias.
- [ ] T2: implementar `CardapiosRelatorio` (com tipo e origem) e
  `AlunosRestricoesRelatorio` (com escola).
  - Validação: testes de colunas, ordenação e totais com base fixa.
- [ ] T3: implementar `OcorrenciasRelatorio` e `SolicitacoesFeedbacksRelatorio`.
  - Validação: testes de truncamento de descrição e de ordenação estável.
- [ ] T4: implementar `IndicadoresAdministrativosRelatorio`.
  - Validação: teste de contagens com base fixa, de escopo por escola e de unidade
  única (escolas ativas da própria unidade).

## Aplicação

- [ ] T5: criar `RelatorioService` com catálogo, validação de filtros e escopo por
  papel, sempre intersectado com unidade ou escola da sessão.
  - Validação: testes de 400/403, de `escolaId`/`unidadeId` fora do escopo e de 404
  entre unidades.
- [ ] T6: implementar preview com `Take(200)` e total agregado separado.
  - Validação: teste com 250 linhas exige 200 no preview e 250 no total.

## Exportação

- [ ] T7: adicionar os pacotes `QuestPDF`, `ClosedXML` e `DocumentFormat.OpenXml` na
  versão estável compatível com `net10.0`.
  - Validação: `dotnet restore` e `dotnet build` sem erro.
- [ ] T8: implementar `ExportadorPdf`, `ExportadorXlsx` e `ExportadorDocx` atrás de
  `IRelatorioExportador`.
  - Validação: testes de assinatura (`%PDF`, `PK`), nome e content type.
- [ ] T9: implementar o limite de 50.000 linhas com 413 antes de gerar bytes.
  - Validação: teste com 50.001 linhas confirma 413 e corpo sem arquivo.

## API e frontend

- [ ] T10: criar `RelatorioController` com preview e exportar.
  - Validação: testes HTTP da matriz 200/400/403/404/413.
- [ ] T11: criar tela de relatórios para nutrição, secretaria e escola com filtros,
  tabela, totais e download por `Blob`.
  - Validação: `npm run build` e `npm run lint`; baixar os três formatos pela
  interface em cada papel.

## Conformidade

- [ ] T12: cobrir as células da matriz da Spec 028 para relatórios (ADM `R`, Escola `R`,
  Nutricionista `R`, Merendeira `—`, Responsável `—`).
  - Validação: `MatrizPermissoesTests` passa para o recurso `Relatorio`; merendeira e
  responsável recebem 403.

## Fechamento

- [ ] T13: atualizar a tabela de endpoints e a seção de relatórios do `README.md`.
  - Validação: rotas documentadas coincidem com o controller.
- [ ] T14: executar os gates completos.
  - Validação: `dotnet build`; `dotnet test`; `cd frontend && npm run build && npm run lint`.

## Definição de pronto

- Cinco relatórios com preview, totais e exportação nos três formatos.
- Escopo por unidade e escola e ausência de dados sensíveis comprovados por teste;
  nenhum papel vê outra unidade.
- Gates verdes.
