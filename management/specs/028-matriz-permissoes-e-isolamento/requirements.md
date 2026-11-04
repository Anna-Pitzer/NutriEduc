---
name: matriz-permissoes-e-isolamento
status: created
references: V17, ADR-021, ADR-020, L5, L12
---

# Requirements — Matriz de permissões e isolamento de dados

## Objetivo

Dar um lugar único e verificável para a pergunta "quem pode fazer o quê, sobre qual
conjunto de dados". Hoje a permissão está declarada em treze specs diferentes, a
`AdminSecretaria` herdou escopo global da Spec 009, a `Merendeira` é escopada por unidade
(Spec 016) e uma spec chega a pedir relatório global para `Nutricionista` (Spec 019, RF7).
Sem uma matriz autoritativa e um conjunto de invariantes de isolamento, cada consulta nova
precisa lembrar do filtro — e o isolamento entre unidades depende de disciplina.

## Escopo

- Backend: resolução de escopo por sessão, matriz de permissão por recurso, semântica de
  401/403/404, auditoria de ações privilegiadas, suíte de conformidade da matriz.
- Frontend: apenas consumo do escopo devolvido pela API; menu e guarda de rota continuam
  conveniência, nunca controle (Spec 009, RF10).
- Fora de escopo: comportamento detalhado de cada recurso (dono: spec do recurso), entidades
  da hierarquia (Spec 027), cardápios (Spec 029), solicitações (Spec 030).

## Precedência e não duplicação

Esta spec é a **fonte autoritativa da matriz** (quem × recurso × alcance) e das invariantes
de isolamento. Cada spec de recurso continua dona do **detalhe** (campos, transições de
estado, cálculos). Quando as duas divergirem, a spec do recurso vale para o detalhe e esta
matriz vale para a permissão; a divergência é defeito e deve ser corrigida na mesma mudança
(RF12). Nenhum recurso é especificado de novo aqui (L12).

## Decisões e interpretações

| # | Ponto ambíguo | Decisão | Origem |
|---|---|---|---|
| D1 | "ADM" e "Escola" na tabela de perfis recebida | `ADM = AdminSecretaria` (escopo unidade); `Escola = AdminEscola` (escopo escola) | ADR-021 |
| D2 | "Escola: acesso aos dados da própria escola" | `AdminEscola` escreve em recurso **operacional** da escola (ocorrência, feedback) e apenas lê o **registro institucional** (usuário, aluno, vínculo, papel) | ADR-021 |
| D3 | "Somente o ADM altera dados do aluno" | Escrita de cadastro de aluno, vínculo e papel é exclusiva de `AdminSecretaria`; `AdminEscola` lê | ADR-021 |
| D4 | "Administrador define os papéis" | `AdminSecretaria` atribui e altera papel de usuário da própria unidade; ninguém atribui o próprio papel | ADR-021 |
| D5 | Relatório global para `Nutricionista` (Spec 019, RF7) | Superado: `Nutricionista` vê relatórios da própria unidade | ADR-021 |
| D6 | `Merendeira` escopada por unidade (Spec 016, RF3) | Superado: escopo é a própria escola (Spec 027) | ADR-020 |

## Requisitos funcionais

- RF1: papéis são exatamente cinco: `AdminSecretaria`, `AdminEscola`, `Nutricionista`,
  `Merendeira`, `Responsavel`. Não existe perfil global. Usuário sem papel não autentica.
- RF2: cada papel tem um único escopo administrativo: `AdminSecretaria → unidade`;
  `AdminEscola → escola`; `Merendeira → escola`; `Nutricionista → unidade`;
  `Responsavel → vínculos`. O escopo é resolvido no servidor a partir da sessão (Spec 027).
- RF3: `AdminSecretaria` atribui e altera o papel de usuário da própria unidade. Usuário não
  altera o próprio papel. Remover papel não é operação válida: o usuário é desativado.
- RF4: a matriz abaixo é a permissão autoritativa. `RW` = ler e escrever no escopo; `R` =
  ler no escopo; `W` = escrever no escopo; `RC` = ler no escopo e criar registro novo, sem
  alterar estado de registro existente; `P` = apenas solicitar (sem escrita direta);
  `—` = sem acesso.

| Recurso (spec dona) | ADM (unidade) | Escola (escola) | Nutricionista (unidade) | Merendeira (escola) | Responsável (vínculo) |
|---|---|---|---|---|---|
| Unidade — a própria (027) | RW | R | R | R | — |
| Escola (027) | RW | R | R | R | — |
| Conta de usuário (009/024) | RW | R | — | — | — |
| Papel do usuário (028) | W | — | — | — | — |
| Próprio perfil (020) | RW | RW | RW | RW | RW |
| Vínculo responsável↔aluno (027) | RW | R | — | — | R |
| Aluno — cadastro (027) | RW | R | R | R | P |
| Restrições e alergias do aluno (010) | RW | R | R | R | P |
| Histórico do aluno (023) | R | R | R | R | R |
| Catálogo de alimentos (011) | R | R | RW | R | — |
| Catálogo de restrições (010) | R | R | RW | R | R |
| Cardápio geral (012/029) | R | R | RW | R | R |
| Cardápio adaptado (029) | R | R | RW | R | R |
| Parâmetros de geração (013) | R | — | RW | — | — |
| Publicação e histórico de cardápio (015) | R | R | RW | R | — |
| Ocorrências (016) | R | RW (altera status) | RW (altera status) | RC | RC (próprias) |
| Solicitações (017/030) | R + decide restrição e cadastro | RC (escola) | RW (decide) | RC (próprias) + cancela | RC (próprias) + cancela |
| Feedback (018) | R | RW (responde) | RW (responde) | RC (próprio) + encerra | RC (próprio) + encerra |
| Notificações (018) | R (próprias) | R (próprias) | R (próprias) | R (próprias) | R (próprias) |
| Relatórios e exportação (019) | R | R | R | — | — |
| Painéis (025) | R | R | R | R | R (filhos) |
| Diagnóstico de legado (027) | R | — | — | — | — |
| Solicitação de acesso (033) | RW (própria unidade) | — | — | — | — |

O cadastro público (Spec 033) é anônimo e **não** é principal da matriz: ele cria apenas uma
solicitação pendente, sem papel e sem escopo.

- RF5: "no escopo" significa o alcance da RF2. Em recurso de aluno, `Responsável` só alcança
  aluno com vínculo; `Merendeira` e `AdminEscola`, apenas a própria escola; `Nutricionista` e
  `AdminSecretaria`, a própria unidade.
- RF6: `Nutricionista` escreve catálogo de alimentos e de restrições por ser **dado de
  referência compartilhado** (RF9), não por escopo de unidade.
- RF7: toda leitura é filtrada no banco pelo escopo (invariante I4). Toda escrita valida o
  alvo contra o escopo **antes** de mutar (invariante I5); nunca existe escrita seguida de
  checagem.
- RF8: semântica de borda:
  - sem sessão → `401`;
  - recurso inexistente → `404`;
  - recurso de **outra unidade** → `404` (não revela existência entre tenants);
  - recurso da **mesma unidade** fora do escopo mais estreito (escola ou vínculo) → `403`;
  - conflito de unicidade ou de estado → `409`;
  - todo corpo de erro segue `{ erro }` (ADR-005) e não contém dado pessoal.
- RF9: catálogo de alimentos e catálogo de restrições são dados de referência
  compartilhados: leitura por qualquer sessão autenticada, escrita exclusiva de
  `Nutricionista` e registrada em trilha. Não são dados de unidade e não entram em I1.
- RF10: nenhum escopo é aceito do cliente. `unidadeId` e `escolaId` em query são filtros
  subordinados ao escopo da sessão; valor fora do escopo devolve `403`, sem ampliar e sem
  ignorar silenciosamente. `POST`/`PUT` que informem escopo divergente devolvem `400`.
- RF11: ações privilegiadas — atribuir/alterar papel, criar/remover vínculo, transferir
  aluno de escola, desativar usuário — são registradas em trilha append-only com autor,
  data UTC, alvo e valores anterior/novo (ADR-009, Spec 023).
- RF12: qualquer spec que altere uma célula da matriz deve atualizar esta tabela e acrescentar
  o caso correspondente à suíte de conformidade na **mesma mudança**. A suíte é o mecanismo
  de verificação; a tabela é o contrato.
- RF13: respostas de erro e de listagem nunca expõem CPF, e-mail, telefone, foto ou senha de
  usuário fora do escopo, nem em contagem agregada (Spec 019, RF11; Spec 018, RF13).
- RF14: a sessão é revalidada a cada requisição: usuário desativado, papel alterado ou
  escopo alterado passam a valer na requisição seguinte, sem esperar novo login.
- RF15: conta com solicitação pendente ou recusada não tem papel, não é principal da matriz,
  não autentica e não recebe escopo. A fila de solicitações é visível apenas ao ADM da
  unidade de destino (Spec 033).

## Invariantes de isolamento

- I1: unidade é a fronteira externa. Nenhuma resposta contém identificador ou nome de
  entidade de outra unidade; a tentativa responde 404.
- I2: escola é a fronteira interna. `AdminEscola` e `Merendeira` recebem 403 para recurso de
  outra escola da mesma unidade.
- I3: vínculo é a fronteira do responsável. Aluno sem vínculo responde 403, mesmo dentro da
  mesma unidade.
- I4: o filtro de escopo é aplicado na consulta (SQL), nunca em memória após carregar tudo.
- I5: escrita valida o alvo no escopo antes de qualquer mutação (proteção contra IDOR).
- I6: escopo nunca vem de parâmetro do cliente (RF10).
- I7: agregações, contadores e relatórios usam o mesmo filtro; não existe vazamento por
  contagem.
- I8: exportação (PDF/XLSX/DOCX) respeita I1–I7 antes de gerar o arquivo.
- I9: notificação e recurso estritamente pessoal de outro usuário respondem 404 (Spec 018,
  RF11), exceção explícita a I2.
- I10: nenhum cache de dados é compartilhado entre escopos; cache de requisição é descartado
  ao final.
- I11: dado de referência (RF9) é a única exceção declarada a I1.

## Requisitos não funcionais

- RNF1: negação por padrão. Recurso novo nasce sem permissão até ser declarado na matriz.
- RNF2: nenhuma regra de autorização no controller além de atributo/política; o escopo é
  resolvido em um único serviço (Spec 009, RNF1).
- RNF3: a suíte de conformidade é parametrizada por (papel, recurso, alcance) e cobre todas
  as células não vazias da matriz; célula sem teste é falha de entrega.
- RNF4: a matriz e a suíte precisam concordar exatamente; teste compara o conjunto de casos
  com a tabela declarada em código.
- RNF5: nenhum segredo ou dado pessoal em log, prompt ou resposta de erro.

## Restrições

- Supera, por decisão do dono: Spec 009 (escopo global de `AdminSecretaria`), Spec 016 RF3
  (merendeira por unidade), Spec 019 RF7 (relatório global de nutricionista), Spec 025 RF1
  ("rede" passa a significar a própria unidade).
- Não cria papel novo nem renomeia papel existente: a mudança é de escopo e de matriz.
- Não altera o formato do cookie além de claims adicionais.
- A matriz é mantida em código como fonte executável da suíte; a tabela deste documento é a
  referência legível e deve ser atualizada junto.
- A implementação atual de `EscopoUsuario.Global` (Nutricionista e AdminSecretaria com escopo
  de rede) deve ser removida na mesma mudança: não existe perfil global.
- A aprovação de cadastro é exclusiva do ADM da unidade; `AdminEscola` não decide solicitação
  de acesso (Spec 033).

## Dependências

- Spec 027 (hierarquia) — fornece unidade, escola e vínculo.
- Spec 033 (cadastro e autorização) — dona da solicitação de acesso e da aprovação que
  atribui papel e escopo.
- Spec 009 (autorização base), Spec 020 (contrato de erro e perfil), Spec 018 (notificações),
  Spec 019 (relatórios), Spec 023 (trilha).
- Specs 010–017 e 025 — donas do detalhe de cada recurso da matriz.

## Casos de borda

- Usuário com dois papéis no histórico: vale o papel atual; a trilha guarda a mudança.
- Papel alterado com sessão aberta: requisição seguinte já usa o novo escopo (RF14).
- `AdminSecretaria` de unidade desativada: autentica, lê, não escreve.
- `Responsavel` com filhos em duas unidades: vê os dois filhos; não vê escola, turma ou
  colega; cada unidade vê apenas o vínculo do próprio aluno.
- Recurso pessoal de outro usuário (notificação, feedback próprio): 404, não 403.
- Consulta com `unidadeId` de outra unidade: `403`, nunca 200 com dado de fora.
- Exportação grande: mesma matriz, mesmos filtros; acima do limite responde 413 (Spec 019).
- Catálogo compartilhado: leitura permitida a todos; escrita por nutricionista de qualquer
  unidade, com trilha.
- Conta pendente tentando acessar qualquer recurso: 401, sem revelar se o e-mail existe.
- ADM de outra unidade pedindo detalhe de solicitação: 404, mesmo que o id exista.

## Critérios de aceitação

- [ ] A suíte de conformidade cobre todas as células não vazias da matriz e passa.
- [ ] Nenhuma rota devolve dado de outra unidade (404) em teste por papel.
- [ ] `AdminEscola` e `Merendeira` recebem 403 para recurso de outra escola da mesma unidade.
- [ ] `Responsavel` recebe 403 para aluno sem vínculo.
- [ ] `AdminSecretaria` não consegue atribuir papel a usuário de outra unidade (404).
- [ ] Nenhum usuário consegue alterar o próprio papel (403).
- [ ] Toda escrita com alvo fora do escopo devolve 403/404 e não altera o banco.
- [ ] Relatório e exportação de `Nutricionista` contêm apenas a própria unidade.
- [ ] `GET /api/relatorios/...?unidadeId=<outra>` não amplia o resultado.
- [ ] Mudança de papel invalida a sessão na requisição seguinte.
- [ ] Ações privilegiadas aparecem na trilha com autor e valores anterior/novo.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Montar duas unidades, cada uma com duas escolas, usuários de todos os papéis e alunos com e
sem vínculo. Executar a suíte parametrizada da matriz e conferir, para cada célula, o código
esperado (200/201, 403, 404, 409, 401). Conferir a trilha das ações privilegiadas e que
nenhuma resposta de erro carrega dado pessoal.
