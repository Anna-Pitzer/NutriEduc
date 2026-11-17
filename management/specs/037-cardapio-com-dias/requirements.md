---
name: cardapio-com-dias
status: audited
references: V25, ADR-027, ADR-022, Spec 012, Spec 013, Spec 029, Spec 015, Spec 019
---

# Requirements — Cardápio com dias

## Objetivo

O cardápio guarda hoje um único conjunto de refeições válido para todo o período. O modelo
oficial do PNAE é uma grade semanal, com conteúdo distinto por dia. Esta spec introduz a
dimensão de dia no agregado, sem alterar as regras de seleção, adaptação ou publicação —
elas passam a ser aplicadas por dia.

## Escopo

- Backend: novo filho do agregado `DiaDeCardapio`, com refeições e itens; geração,
  validação, adaptação, publicação e portal passam a operar por dia.
- Frontend: detalhe e formulário do cardápio exibem e editam dias.
- Relatório `Cardapios` (Spec 019) passa a consumir os dias reais.
- Fora de escopo: mudar o algoritmo de seleção (Spec 032), novos formatos de exportação,
  novos relatórios, alteração da matriz de permissões.

## Requisitos funcionais

- RF1: `Cardapio` passa a conter `Dias`; cada `DiaDeCardapio` tem `Data` (`DateOnly`),
  `Ordem` e a coleção de refeições. `Refeicao` e `ItemRefeicao` deixam de pertencer ao
  cardápio e passam a pertencer ao dia.
- RF2: cada dia tem exatamente as três refeições fixas, nesta ordem: **Café, Almoço e
  Lanche**. O nome é canônico no domínio; refeição faltante ou nome diferente invalida o
  dia. `Refeicao` mantém `Horario` opcional (`TimeOnly?`), sem exigir preenchimento.
- RF3: invariantes do dia — a data pertence ao período do cardápio; não há duas datas
  iguais no mesmo cardápio; todo cardápio tem ao menos um dia; a ordem é derivada da data.
  O período tem no máximo **15 dias corridos** — regra quinzenal que revisa o limite de 366
  dias da Spec 012. Cada dia contém exatamente as três refeições fixas, na ordem canônica.
- RF4: cardápios existentes são descartados na migration — são dados de teste, confirmados
  pelo dono do produto em 2026-11-16. Nenhum backfill é executado e nenhum cardápio antigo
  precisa ser preservado.
- RF5: o contrato de detalhe do cardápio passa a expor `dias[]`, cada um com `data`,
  `refeicoes[]` (nome, ordem, horário, itens) e totais; o campo `refeicoes` no nível do
  cardápio é removido. Todos os consumidores são atualizados nesta spec.
- RF6: a geração automática aceita `dias` explícitos (datas) ou, quando ausentes, gera
  todos os dias úteis do período. Cada dia é gerado com a mesma função pura da Spec 032,
  uma vez para cada uma das três refeições fixas; a variedade entre dias é obtida derivando
  a semente com a data, e o conjunto de alimentos usados é reiniciado por dia.
- RF7: a validação pós-montagem roda por dia; o cardápio só é aceito se todos os dias
  passarem. Diagnóstico nomeia a data, a refeição faltante e a refeição que falhou na faixa.
- RF8: a adaptação por escola e conjunto de restrições é feita por dia, reutilizando
  `AdaptacaoDeCardapio` sem alteração de regra.
- RF9: publicação, cancelamento, revisão e histórico continuam no nível do cardápio; o
  snapshot de publicação inclui os dias.
- RF10: o portal consulta o cardápio do aluno pela data: devolve o dia correspondente;
  data sem dia no cardápio devolve 404 com mensagem.
- RF11: o relatório `Cardapios` monta as colunas a partir das datas existentes no
  cardápio, não mais de dias úteis presumidos; um dia sem refeição aparece vazio.
- RF12: o frontend mostra os dias em abas ou seções, com data, refeições e itens; o
  formulário permite adicionar/remover dia e definir horário da refeição.
- RF13: o limite quinzenal é validado no domínio, no serviço e no formulário; período
  acima de 15 dias corridos devolve 400 com mensagem.
- RF14: o parâmetro de geração deixa de definir nomes de refeição. `RefeicoesPadrao` é
  removido da entidade, do DTO, da tela e da coluna no banco; a geração usa as três
  refeições fixas do domínio. A Spec 013 é revisada neste ponto.

## Requisitos não funcionais

- RNF1: carregar um cardápio usa uma consulta com `Include` de dias → refeições → itens,
  sem N+1.
- RNF2: a geração continua determinística — mesma entrada (semente + datas) produz os
  mesmos dias.
- RNF3: nenhuma dependência nova; a migration é aditiva e não apaga dados.
- RNF4: o período do cardápio tem no máximo 15 dias corridos; a geração produz os dias
  úteis desse intervalo — no máximo 11 — e recusa data fora dele.

## Restrições

- O algoritmo de seleção da Spec 032 não muda; muda apenas o recorte por dia.
- A matriz de permissões (Spec 028) não muda.
- A decisão arquitetural está no ADR-027.

## Dependências

- Specs 012 (agregado), 013/032 (geração), 029 (adaptação), 015 (publicação e histórico),
  035 (portal), 019 (relatório de cardápios), ADR-022 e ADR-027.
- A Spec 013 é revisada quanto ao parâmetro de refeições padrão (RF14).

## Casos de borda

- Data fora do período → 400.
- Duas datas iguais → 400.
- Cardápio sem dia → 400 na criação e na edição.
- Refeição faltante no dia → 400.
- Nome de refeição fora de Café/Almoço/Lanche → 400.
- Período sem dia útil → 400 com mensagem.
- Período acima de 15 dias corridos → 400 na criação e na geração.
- Publicação de cardápio com dia inválido → bloqueada pela validação do dia.
- Portal com data sem dia → 404.
- Adaptação de dia sem itens → pendência registrada, sem exceção.

## Critérios de aceitação

- [ ] Migration cria a nova estrutura e descarta os cardápios de teste existentes.
- [ ] Período acima de 15 dias corridos é rejeitado com 400.
- [ ] Criar cardápio com dois dias distintos grava os dois e rejeita datas repetidas.
- [ ] Todo dia contém exatamente Café, Almoço e Lanche.
- [ ] Refeição faltante ou com nome diferente é rejeitada com 400.
- [ ] A geração cria as três refeições em cada dia.
- [ ] Data fora do período é rejeitada com 400.
- [ ] Geração sem `dias` cria um conjunto por dia útil e cada dia passa na validação.
- [ ] Mesma semente e mesmas datas produzem o mesmo resultado.
- [ ] Adaptação por escola gera uma versão por dia.
- [ ] Publicação e histórico continuam funcionando com dias.
- [ ] Portal devolve o dia da data consultada e 404 quando não existe.
- [ ] Relatório de cardápios mostra conteúdo distinto por dia.
- [ ] Frontend exibe, edita e remove dias.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Com uma base fixa (um cardápio com três dias), conferir: leitura e
edição dos dias; geração com e sem datas explícitas; adaptação por escola; publicação e
reversão; consulta do portal por data; relatório com colunas por data. Executar os gates:
`dotnet build backend/backend.csproj`, `dotnet test backend.Tests/backend.Tests.csproj` e
`cd frontend && npm run build && npm run lint`.
