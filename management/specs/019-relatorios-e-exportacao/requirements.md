---
name: relatorios-e-exportacao
status: created
references: V6, ADR-004, L6
---

# Requirements — Relatórios e exportação

## Objetivo

Os dados operacionais existem ao final das specs 016–018, mas não há como consolidá-los
nem entregá-los fora do sistema. O produto exige relatórios, tabelas formatadas e
exportação em PDF, Excel e Word para nutrição, secretaria e escola.

## Escopo

- Backend: catálogo de cinco relatórios, montagem de tabela (colunas + linhas +
  totais), indicadores administrativos e três exportadores (PDF, XLSX, DOCX).
- Frontend: tela de relatórios com filtros, pré-visualização em tabela e botões de
  exportação.
- Fora de escopo: agendamento, envio por e-mail, dashboards em tempo real e
  armazém de dados (data warehouse).

### Cobertura de `implementacoes.md`

- **Nutrição (5):** gerar relatórios, gerar tabelas já formatadas, exportar em PDF,
  Excel e Word.
- **Secretaria (2):** visualizar indicadores gerais do sistema e gerar relatórios
  administrativos.
- **Escolas (1):** gerar relatórios da escola.

## Requisitos funcionais

- RF1: catálogo fechado de relatórios:
  `Cardapios`, `AlunosRestricoes`, `Ocorrencias`, `SolicitacoesFeedbacks`,
  `IndicadoresAdministrativos`. Nenhum relatório fora do catálogo é aceito.
- RF2: todo relatório aceita `de`, `ate`, `escolaId`, `unidadeId` (quando aplicável);
  `de > ate` ou intervalo maior que 366 dias devolve 400.
- RF3: a resposta de pré-visualização contém `titulo`, `colunas[]`
  (`chave`, `titulo`, `tipo`, `formato`), `linhas[]`, `totais`, `geradoEmUtc` e
  `filtrosAplicados`; a tabela é renderizada sem transformação no frontend.
- RF4: `GET /api/relatorios/{tipo}/preview` devolve no máximo 200 linhas; o total de
  linhas e os totais agregados refletem o conjunto completo do filtro.
- RF5: `GET /api/relatorios/{tipo}/exportar?formato=pdf|xlsx|docx` gera o arquivo
  completo com `Content-Disposition` e nome
  `relatorio-{tipo}-{yyyyMMddHHmm}.{ext}`.
- RF6: exportação acima de 50.000 linhas devolve 413 com `{ erro }`; nenhum arquivo
  parcial é enviado.
- RF7: escopo por papel: `Nutricionista` acessa todos os relatórios (global ou
  filtrado); `AdminEscola` acessa `AlunosRestricoes`, `Ocorrencias`,
  `SolicitacoesFeedbacks` e `IndicadoresAdministrativos` restritos à própria escola;
  `AdminSecretaria` acessa `IndicadoresAdministrativos` e
  `SolicitacoesFeedbacks` globais. `Merendeira` e `Responsavel` não acessam.
- RF8: `IndicadoresAdministrativos` devolve contagens: escolas ativas, unidades
  ativas, usuários ativos por papel, alunos, cardápios publicados no período,
  ocorrências abertas e solicitações abertas — no escopo do papel.
- RF9: `Cardapios` lista cardápio, unidade, período, situação, versão e totais
  nutricionais por refeição; `AlunosRestricoes` lista aluno, unidade, restrições e
  anafilaxia; `Ocorrencias` lista data, tipo, status, unidade, aluno e descrição
  resumida; `SolicitacoesFeedbacks` lista tipo, status, solicitante, unidade e datas.
- RF10: a ordenação de cada relatório é determinística e documentada na resposta
  (`ordenacao`).
- RF11: nenhuma coluna expõe CPF, e-mail, telefone, foto ou senha.
- RF12: o frontend exibe a pré-visualização, os totais e os três botões de exportação;
  erro 413 é exibido com orientação para reduzir o período.

## Requisitos não funcionais

- RNF1: geração no servidor; o frontend não recalcula totais nem formata números.
- RNF2: consultas de relatório são somente leitura e usam `AsNoTracking`.
- RNF3: filtros de escopo aplicados na consulta SQL, nunca em memória.
- RNF4: exportadores são plugáveis por interface; adicionar formato não altera a
  montagem do relatório.
- RNF5: geração síncrona com limite de 50.000 linhas; sem fila nesta spec.
- RNF6: contrato `{ erro }` em 400/403/404/413.

## Restrições

- Migration não é necessária: relatórios leem dados existentes.
- Novas dependências de exportação: `QuestPDF` (PDF), `ClosedXML` (XLSX) e
  `DocumentFormat.OpenXml` (DOCX), na versão estável mais recente compatível com
  `net10.0`. Nenhuma outra dependência é adicionada.
- Nenhuma alteração nos modelos das specs anteriores.

## Dependências

- Specs 009, 010, 012, 015, 016 e 017 implementadas; 018 enriquece
  `SolicitacoesFeedbacks`.
- Spec 007 (contrato de erro) e Spec 008 (documentação de endpoints).

## Casos de borda

- Período sem dados → tabela com cabeçalho, zero linhas, totais zerados, 200.
- `formato` desconhecido → 400 com a lista aceita.
- Relatório inexistente → 404.
- `AdminEscola` pedindo `Cardapios` → 403.
- `AdminEscola` filtrando `escolaId` de outra escola → 403.
- Exportação com 50.001 linhas → 413, sem arquivo.
- Valores decimais seguem cultura invariável na exportação e no JSON.
- Nome de arquivo com data local do servidor em UTC.
- Relatório de escola sem alunos → indicadores com zeros, não erro.

## Critérios de aceitação

- [ ] Cada um dos cinco relatórios responde com colunas, linhas, totais e ordenação.
- [ ] Totais conferem com os dados de uma base fixa de teste.
- [ ] Pré-visualização limita a 200 linhas e informa o total real.
- [ ] PDF começa com `%PDF`, XLSX e DOCX são arquivos ZIP válidos (`PK`).
- [ ] `Content-Disposition` e extensão correspondem ao formato.
- [ ] Período inválido e formato inválido devolvem 400.
- [ ] 50.001 linhas devolvem 413 sem corpo de arquivo.
- [ ] Matriz de escopo 200/403 por papel e por escola.
- [ ] Nenhuma coluna expõe CPF, e-mail, telefone ou foto.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Com uma base fixa (uma escola, duas unidades, alunos com restrições, cardápios,
ocorrências e solicitações), gerar cada relatório, conferir totais e exportar nos
três formatos; repetir com `AdminEscola` e conferir escopo e 403. Executar os gates: `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e `cd frontend && npm run build && npm run lint`.
