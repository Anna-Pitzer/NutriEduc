---
name: solicitacao-alteracao-dados
status: created
references: V19, ADR-016, ADR-004, L9
---

# Requirements — Solicitação de alteração de dados do aluno

## Objetivo

Dar ao responsável um canal formal para pedir correção dos dados cadastrais do filho e ao ADM
um fluxo de decisão com trilha. A Spec 017 cobre solicitações de cardápio, ingrediente,
restrição e alergia, mas **não** cobre dados cadastrais: hoje o responsável não tem como pedir
correção de nome, nascimento ou contato, e o ADM altera diretamente pela Spec 001, sem registrar
que houve um pedido.

## Escopo

- Backend: novo tipo `DadosCadastrais` no agregado `Solicitacao` da Spec 017, com validação por
  campo, decisão do ADM e aplicação atômica via `Aluno.AtualizarDados`.
- Frontend: tela do responsável para pedir correção e fila do ADM com o diff
  (valor atual → valor solicitado).
- Fora de escopo: solicitações de cardápio, ingrediente, restrição e alergia (Spec 017);
  feedback (Spec 018); edição direta do aluno (Spec 001 e Spec 023); transferência de escola
  (ação direta do ADM, Spec 027).

### Cobertura de `implementacoes.md`

- **Pais:** solicitar alteração dos dados do filho e acompanhar a decisão.
- **Secretaria:** analisar, aprovar ou recusar com motivo, e ver o histórico do pedido.

## Decisões e interpretações

| # | Ponto ambíguo | Decisão | Origem |
|---|---|---|---|
| D1 | Quem decide | `AdminSecretaria` da unidade do aluno; `AdminEscola` apenas lê (matriz 028) | ADR-021 |
| D2 | Quais campos | `nome`, `dataNascimento`, `nomeResponsavel`, `telefoneResponsavel`, `observacao` | RF3 |
| D3 | Restrição, alergia e anafilaxia | Continuam no tipo próprio da Spec 017 / Spec 010; não entram aqui | Spec 017 |
| D4 | CPF, escola e dados de conta | Fora do escopo: CPF não é editável; escola é transferência (Spec 027); conta é perfil (Spec 020) | RF3 |
| D5 | Vários campos por pedido | Permitido; a aplicação é tudo ou nada | RF6 |
| D6 | Pedido concorrente para o mesmo campo | Bloqueado enquanto houver pedido aberto para o mesmo campo | RF13 |

## Requisitos funcionais

- RF1: `TipoSolicitacao` ganha o valor `DadosCadastrais`; os tipos existentes e suas transições
  não mudam (Spec 017, RF1–RF4).
- RF2: o pedido de `DadosCadastrais` carrega `alteracoes[]` com `{ campo, valorSolicitado }` e
  `justificativa` (10–500). O valor atual **não** é enviado pelo cliente: é lido pelo servidor
  no momento da criação e guardado como snapshot informativo.
- RF3: campos aceitos: `nome`, `dataNascimento`, `nomeResponsavel`, `telefoneResponsavel`,
  `observacao`. Qualquer outro campo devolve 400 com a lista dos aceitos.
- RF4: quem cria é `Responsavel` com vínculo ao aluno (Spec 027). Sem vínculo devolve 403.
  `AdminSecretaria` e `AdminEscola` não criam este tipo: editam diretamente quando têm
  permissão.
- RF5: status segue a Spec 017: `Aberta → EmAnalise → Aprovada | Recusada`; `Aprovada →
  Concluida` após a aplicação. Recusa exige `motivo` (5–500).
- RF6: aprovar aplica **todas** as alterações em uma única operação, por `Aluno.AtualizarDados`
  (ADR-004), e grava um evento na trilha do aluno (Spec 023) com autor = quem aprovou e origem
  = id da solicitação. Se qualquer campo falhar na validação do domínio, nada é aplicado e a
  solicitação permanece `EmAnalise` com a mensagem do campo que falhou.
- RF7: antes de aplicar, cada campo é conferido contra o valor atual. Se o valor atual divergir
  do snapshot registrado na criação, a aprovação devolve 409, nada é aplicado e a solicitação
  permanece `EmAnalise` com a lista de campos divergentes.
- RF8: aprovação é idempotente por status: aprovar uma solicitação já `Aprovada` ou `Concluida`
  devolve 409 e não aplica de novo.
- RF9: listagem e detalhe seguem a Spec 017 (filtros `tipo`, `status`, `alunoId`), com escopo da
  Spec 028: responsável vê os próprios pedidos; ADM vê os da unidade; `AdminEscola` vê os da
  própria escola; `Merendeira` e `Nutricionista` não veem este tipo.
- RF10: toda mudança de status emite notificação `SolicitacaoStatus` (Spec 018) ao solicitante e
  ao decisor, sem dado clínico no conteúdo.
- RF11: solicitação não é apagada; cancelamento é status (Spec 017, RF12).
- RF12: a tela do responsável permite escolher o filho, o campo e o novo valor, exigir
  justificativa e mostrar o status. A fila do ADM mostra aluno, campos com valor atual → valor
  solicitado, justificativa e as ações aprovar/recusar com motivo.
- RF13: não pode existir mais de uma solicitação `Aberta` ou `EmAnalise` para o mesmo
  `(aluno, campo)`. A segunda criação devolve 409 com o id da solicitação existente.
- RF14: aluno inativo ou fora do escopo não recebe solicitação nova (409 e 403,
  respectivamente).

## Requisitos não funcionais

- RNF1: validação de campo no domínio (ADR-004), reutilizando as mesmas regras de `Aluno`; a
  solicitação nunca cria um caminho de escrita paralelo ao agregado.
- RNF2: aplicação e trilha na mesma unidade de trabalho (ADR-019); não existe aprovação sem
  evento de histórico.
- RNF3: nenhum dado pessoal em log, notificação ou mensagem de erro além do necessário para o
  decisor no próprio escopo.
- RNF4: a decisão é registrada com autor (snapshot), data UTC e motivo, no padrão da ADR-009.
- RNF5: o pedido é imutável no conteúdo: editar o pedido não é permitido; corrigir exige novo
  pedido após recusa ou cancelamento.

## Restrições

- O agregado `Solicitacao` e suas transições pertencem à Spec 017; esta spec acrescenta um tipo
  e a regra de aplicação, sem reescrever o fluxo (L12).
- O histórico do aluno continua dono da trilha de alteração (Spec 023); esta spec só grava o
  evento com a origem `solicitacaoId`.
- Nenhum campo novo é criado em `Aluno`; os campos já existentes são os únicos editáveis.
- Sem exclusão física e sem alteração retroativa de solicitação decidida.

## Dependências

- Spec 017 (fluxo de solicitações) — agregado, transições, listagem.
- Spec 023 (histórico do aluno) — trilha da aplicação.
- Spec 027 (hierarquia) e Spec 028 (matriz) — escopo de quem cria, vê e decide.
- Spec 018 (notificações) — evento de status.
- Spec 001 (CRUD de aluno) — regras de edição já existentes.

## Casos de borda

- ADM altera o campo antes da decisão: aprovação devolve 409 e mostra a divergência; o
  responsável precisa criar novo pedido.
- Dois pedidos abertos para o mesmo campo: o segundo é recusado na criação (RF13).
- Responsável desvinculado depois do pedido: o pedido continua visível para a unidade e pode ser
  decidido; o ex-responsável deixa de ver pedidos novos.
- Aluno transferido de escola entre a criação e a decisão: o decisor é o ADM da unidade atual do
  aluno no momento da decisão.
- Valor inválido (nome vazio, data futura): aprovação falha com 400, nada é aplicado, status
  permanece `EmAnalise`.
- Pedido de campo fora da lista: 400 na criação, sem gravar.
- Pedido cancelado pelo responsável: não pode ser aprovado depois.
- Solicitação de dados cadastrais criada por `AdminEscola`: 403 (RF4).

## Critérios de aceitação

- [ ] `POST /api/solicitacoes` com tipo `DadosCadastrais` e campo válido cria a solicitação.
- [ ] Campo fora da lista devolve 400 com a lista dos aceitos.
- [ ] Responsável sem vínculo devolve 403.
- [ ] `AdminEscola` não cria este tipo (403).
- [ ] Aprovação aplica todos os campos de uma vez e grava um evento na trilha com a origem.
- [ ] Campo inválido na aprovação não altera nenhum campo e mantém `EmAnalise`.
- [ ] Divergência entre snapshot e valor atual devolve 409 sem aplicar.
- [ ] Aprovar duas vezes devolve 409 e não duplica histórico.
- [ ] Segundo pedido aberto para o mesmo campo devolve 409 com o id existente.
- [ ] Recusa sem motivo devolve 400.
- [ ] `Nutricionista` e `Merendeira` não veem solicitações deste tipo.
- [ ] Notificação de status é emitida sem dado clínico.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Cadastrar um aluno com responsável vinculado. Criar um pedido de alteração de nome e telefone,
conferir o snapshot e a fila do ADM com o diff. Aprovar e confirmar a alteração no aluno e o
evento na trilha com autor e origem. Repetir com campo inválido, com divergência provocada por
edição direta do ADM e com segundo pedido para o mesmo campo. Executar os gates do projeto.
