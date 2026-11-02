---
name: painel-e-visao-por-papel
status: planned
references: V14, V6, ADR-002
---

# Requirements — Painel e visão por papel

## Objetivo

As quatro áreas de acesso já existem como estrutura — papéis (`PapelUsuario`), guarda de
rota (`ProtegidaPorPapel`) e menu por papel — mas **três são placeholders**:
`HomeSecretaria`, `HomeMerendeiras` e `HomePais`. As features que alimentariam essas telas
existem como spec (012–019), cada uma definindo acesso por papel **do próprio recurso**, e
não uma área de entrada. Esta spec entrega o conteúdo de entrada de cada papel, sem criar
recurso novo.

## Escopo

- Backend: leitura agregada somente (`PainelService`) — painel da secretaria, cardápio do
  dia da merendeira, visão da família e relatório do filho.
- Frontend: preencher as três telas de área já roteadas e o menu por papel.
- Fora de escopo: criar cardápio, alterar restrição, relatórios de gestão (Spec 019),
  dashboards em tempo real e gráficos históricos.

## Cobertura de `implementacoes.md`

- **Secretaria (1):** painel geral da rede.
- **Merendeiras (1):** visualizar o cardápio do dia da própria unidade.
- **Pais (2):** visualizar o cardápio do dia e o relatório **do próprio filho**.

## Requisitos funcionais

- RF1: `GET /api/painel/secretaria` devolve indicadores agregados da rede, sem PII:
  alunos ativos, alunos por unidade, usuários ativos por papel, cardápios publicados na
  semana, ocorrências abertas e solicitações pendentes.
- RF2: cada indicador é contagem agregada; nenhuma resposta expõe nome, CPF, e-mail,
  telefone, foto ou restrição de aluno individual.
- RF3: `GET /api/painel/merendeira` devolve o cardápio publicado do dia da unidade do
  solicitante, com refeições, itens e nomes de alimento.
- RF4: a mesma resposta traz, por item do cardápio, a contagem de alunos da unidade com
  restrição que incide sobre aquele alimento — o alerta que a Spec 014 calcula, resumido.
- RF5: `GET /api/painel/familia` devolve, para o responsável autenticado, a lista dos
  próprios filhos vinculados, o cardápio do dia da unidade de cada filho e as restrições
  de cada filho.
- RF6: `GET /api/painel/familia/{alunoId}/relatorio?de=&ate=` devolve o relatório do
  **próprio filho**: restrições vigentes, alterações de restrição no período (Spec 023) e
  ocorrências registradas no período (Spec 016). Aluno não vinculado responde 403.
- RF7: responsável nunca acessa dado de aluno de outro responsável, nem por parâmetro nem
  por id adivinhado; a negativa não revela existência.
- RF8: secretaria acessa o painel e mantém o acesso às telas de gestão já existentes
  (Spec 021); merendeira continua sem qualquer rota de escrita de cardápio.
- RF9: cada tela de área mostra apenas as entradas de menu do próprio papel; menu vazio
  não é renderizado.

## Requisitos não funcionais

- RNF1: toda agregação e todo filtro acontecem no SQL, via filtros de negócio (Spec 024,
  F5); nenhuma consulta carrega a tabela inteira para filtrar em memória.
- RNF2: leitura pura — nenhuma rota desta spec grava em banco.
- RNF3: resposta sem dado pessoal além do necessário ao próprio papel; o relatório do
  filho é a única resposta com dados do aluno, e é escopada ao vínculo.
- RNF4: tempo de resposta do painel não depende do volume de alunos; contagens usam os
  índices existentes.
- RNF5: falha de um indicador não derruba o painel: cada bloco é independente e devolve
  zero com o motivo no corpo quando a origem estiver vazia.

## Restrições

- Nenhuma entidade nova; nenhuma migration.
- Nenhuma regra de negócio no controller: a decisão de escopo fica no serviço.
- O relatório da família **não** entra na Spec 019, que permanece o catálogo de relatórios
  de gestão e segue sem acesso para `Responsavel`.

## Dependências

- Spec 009 (papéis e escopo) e Spec 010/023 (restrições e histórico).
- Spec 012/015 (cardápio e publicação) para o cardápio do dia.
- Spec 014 (compatibilidade) para os alertas por item.
- Spec 016 (ocorrências) e 017 (solicitações) para os indicadores e o relatório do filho.

## Casos de borda

- Unidade sem cardápio publicado na data → lista vazia e 200, nunca erro.
- Merendeira sem unidade definida → 403 com mensagem da regra.
- Responsável sem filho vinculado → lista vazia, 200.
- Responsável com mais de um filho → cardápio e restrições por filho, sem misturar.
- Aluno sem unidade → aparece na visão do responsável, sem cardápio associado.
- Secretaria sem nenhum cardápio publicado na semana → indicador zero, painel íntegro.
- Aluno de outro responsável → 403 sem revelar existência.

## Critérios de aceitação

- [ ] `GET /api/painel/secretaria` por `Nutricionista` devolve 403.
- [ ] `GET /api/painel/merendeira` devolve apenas o cardápio da unidade do solicitante.
- [ ] `GET /api/painel/familia` lista somente os filhos vinculados ao solicitante.
- [ ] Relatório de aluno não vinculado devolve 403.
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
