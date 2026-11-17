---
name: painel-e-visao-por-papel
status: audited
references: V14, V6, ADR-002
---

# Requirements — Painel e visão por papel

## Objetivo

As quatro áreas de acesso já existem como estrutura — papéis (`PapelUsuario`), guarda de
rota (`ProtegidaPorPapel`) e menu por papel — mas **três são placeholders**:
`HomeSecretaria` (só gestão), `HomeMerendeiras` (lista pública de cardápios) e `PainelPais`
(só filhos e restrições). As features que alimentariam essas telas existem como spec
(012–019), cada uma definindo acesso por papel **do próprio recurso**, e não uma área de
entrada. Esta spec entrega o conteúdo de entrada de cada papel, sem criar recurso novo.

## Escopo

- Backend: leitura agregada somente (`PainelService`) — painel da secretaria, cardápio do
  dia da merendeira, visão da família e relatório do filho.
- Frontend: preencher as três telas de área já roteadas e o menu por papel.
- Fora de escopo: criar cardápio, alterar restrição, relatórios de gestão (Spec 019),
  dashboards em tempo real e gráficos históricos.

## Revisão de 2026-11-17 (antes da implementação)

A spec foi revista depois que as Specs 029 (versão servida) e 035 (área da família)
entraram em vigor. As decisões abaixo substituem o texto original e estão justificadas no
`design.md`:

- **D1** — o painel da secretaria é escopado à **unidade da sessão** (Spec 027/024), não à
  rede inteira. `AdminSecretaria` nunca enxerga outra unidade em nenhuma outra rota; um
  painel global seria ampliação de privilégio.
- **D2** — "alunos ativos" passa a **alunos cadastrados**: `Aluno` não tem flag de
  atividade e a exclusão é física.
- **D3** — "alunos por unidade" passa a **alunos por escola da unidade**, que é o recorte
  útil dentro do escopo (uma única linha por unidade seria o próprio total).
- **D4** — o painel da família **não** devolve o cardápio geral para filho com restrição
  (fail-closed); devolve `requerAdaptacao` e direciona para a versão servida da Spec 029.
- **D5** — negativa de vínculo é sempre `403`, inclusive para id inexistente: a resposta
  não revela existência.
- **D6** — a contagem por item (RF4) reutiliza a regra única de compatibilidade
  (ADR-011), e é a exceção documentada ao RNF1.

## Cobertura de `implementacoes.md`

- **Secretaria (1):** painel geral da unidade.
- **Merendeiras (1):** visualizar o cardápio do dia da própria unidade.
- **Pais (2):** visualizar o cardápio do dia e o relatório **do próprio filho**.

## Requisitos funcionais

- RF1: `GET /api/painel/secretaria` devolve, para a **unidade do solicitante**, sem PII:
  alunos cadastrados, alunos por escola, usuários ativos por papel, cardápios publicados
  nos últimos sete dias (incluindo hoje), ocorrências abertas e solicitações pendentes.
  Sessão sem unidade → 403.
- RF2: cada indicador é contagem agregada; nenhuma resposta expõe nome, CPF, e-mail,
  telefone, foto ou restrição de aluno individual.
- RF3: `GET /api/painel/merendeira?data=` devolve o cardápio **geral publicado** da
  unidade do solicitante na data (padrão: hoje), com refeições, itens, nomes de alimento e
  gramas. Sessão sem unidade → 403. Sem cardápio publicado → 200 com lista vazia e motivo.
- RF4: cada item traz `alunosComRestricao` — a quantidade de alunos **distintos** da
  unidade afetados por alguma restrição vinculada àquele alimento, conforme a regra da
  Spec 014. A merendeira vê **quantos**, nunca **quem**.
- RF5: `GET /api/painel/familia?data=` devolve os filhos vinculados ao responsável, com
  escola, unidade, restrições vigentes e o cardápio do dia. Para filho **sem** restrição,
  devolve as refeições do cardápio geral publicado. Para filho **com** restrição, não
  devolve o cardápio geral: devolve `requerAdaptacao: true`, `cardapio: null` e `motivo`,
  apontando a versão servida (Spec 029). Filho sem unidade → `cardapio: null` com motivo.
- RF6: `GET /api/painel/familia/{alunoId}/relatorio?de=&ate=` devolve, do **próprio
  filho**: restrições vigentes, alterações de restrição no período (Spec 023) e ocorrências
  registradas no período (Spec 016). Período padrão: últimos 30 dias; intervalo máximo de
  366 dias; intervalo invertido → 400.
- RF7: aluno fora do vínculo responde 403, **exista ele ou não**; a negativa não revela
  existência.
- RF8: secretaria acessa o painel e mantém o acesso às telas de gestão já existentes
  (Spec 021); merendeira continua sem qualquer rota de escrita de cardápio.
- RF9: cada tela de área mostra apenas as entradas de menu do próprio papel; menu vazio
  não é renderizado.

## Requisitos não funcionais

- RNF1: toda contagem da secretaria acontece no SQL (`GROUP BY`/`COUNT`), via filtros de
  negócio. **Exceção documentada (D6):** a contagem por item do RF4 deriva da mesma função
  de domínio da Spec 014, porque `Aluno.Restricoes` é coleção primitiva (JSON) e
  reimplementar o cruzamento em SQL duplicaria a regra única de compatibilidade.
- RNF2: leitura pura — nenhuma rota desta spec grava em banco.
- RNF3: resposta sem dado pessoal além do necessário ao próprio papel; o relatório do
  filho é a única resposta com dados do aluno, e é escopada ao vínculo.
- RNF4: tempo de resposta do painel não depende do volume de alunos; contagens usam os
  índices existentes.
- RNF5: bloco sem origem devolve zero ou lista vazia com o motivo, nunca erro. Falha real
  de banco não é silenciada: não se converte exceção em zero.

## Restrições

- Nenhuma entidade nova; nenhuma migration.
- Nenhuma regra de negócio no controller: a decisão de escopo fica no serviço.
- O relatório da família **não** entra na Spec 019, que permanece o catálogo de relatórios
  de gestão e segue sem acesso para `Responsavel`.

## Dependências

- Spec 009 (papéis e escopo) e Spec 010/023 (restrições e histórico).
- Spec 012/015 (cardápio e publicação) para o cardápio do dia; Spec 029 para a versão servida.
- Spec 014 (compatibilidade) para os alertas por item.
- Spec 016 (ocorrências) e 017 (solicitações) para os indicadores e o relatório do filho.
- Spec 035 (área da família) já entrega filhos, restrições e versão servida; esta spec
  acrescenta o painel de entrada e o relatório consolidado.

## Casos de borda

- Unidade sem cardápio publicado na data → lista vazia e 200, nunca erro.
- Merendeira sem unidade definida → 403 com mensagem da regra.
- Responsável sem filho vinculado → lista vazia, 200.
- Responsável com mais de um filho → cardápio e restrições por filho, sem misturar.
- Aluno sem unidade → aparece na visão do responsável, sem cardápio associado.
- Secretaria sem nenhum cardápio publicado nos últimos sete dias → indicador zero, painel íntegro.
- Aluno de outro responsável (ou inexistente) → 403 sem revelar existência.
- Filho com restrição → sem cardápio geral no painel; `requerAdaptacao` verdadeiro.

## Critérios de aceitação

- [ ] `GET /api/painel/secretaria` por `Nutricionista` devolve 403.
- [ ] `GET /api/painel/secretaria` devolve apenas a unidade da sessão.
- [ ] `GET /api/painel/merendeira` devolve apenas o cardápio da unidade do solicitante.
- [ ] Merendeira sem unidade devolve 403.
- [ ] `GET /api/painel/familia` lista somente os filhos vinculados ao solicitante.
- [ ] Filho com restrição não recebe cardápio geral no painel.
- [ ] Relatório de aluno não vinculado devolve 403, inclusive para id inexistente.
- [ ] Relatório de aluno vinculado devolve apenas dados daquele aluno.
- [ ] Painel da secretaria não contém nome, CPF, e-mail, telefone nem foto.
- [ ] Menu de cada papel não exibe entrada de outro papel.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro.

## Validação

Preparar duas unidades com cardápio publicado, um aluno com restrição por unidade e dois
responsáveis (um com dois filhos). Conferir as três telas e a matriz de acesso, incluindo a
negativa para aluno de outro responsável. Executar `dotnet build backend/backend.csproj`,
`dotnet test backend.Tests/backend.Tests.csproj` e, em `frontend`,
`npm run build && npm run lint`.
