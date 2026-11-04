---
name: cadastro-e-autorizacao-de-acesso
status: created
references: V22, V17, ADR-021, ADR-020, ADR-004, ADR-019
---

# Requirements — Cadastro e autorização de acesso

## Objetivo

O autocadastro atual (`POST /api/usuario/registrar`) cria a conta **sem papel**, mas não
registra **o que a pessoa pediu**: `CriarUsuarioEntrada` não tem tipo de cadastro e
`Usuario.Papel` fica nulo. A fila administrativa não existe como entidade; a tela de usuários
adivinha um papel inicial (`Merendeira`) para contas sem papel, e o vínculo do responsável é uma
segunda operação separada (`POST /api/aluno/{alunoId}/responsaveis/{usuarioId}`). Esta spec
documenta o fluxo completo de solicitação, análise, autorização e vínculo em um único ato.

## Escopo

- Backend: tipo solicitado, solicitação de acesso com estado, fila por unidade, aprovação e
  recusa transacionais, vínculo de alunos na aprovação, autorização por escopo.
- Frontend: cadastro por tipo, confirmação de pendência, fila de solicitações e modal de
  aprovação com seleção de escola e de alunos.
- Fora de escopo: criação administrativa direta de contas (Specs 009 e 028 continuam donas),
  autenticação e sessão (Spec 020), matriz de permissões (Spec 028), hierarquia (Spec 027),
  CRN (Spec 031), cardápios públicos (Spec 012).

### Relação com specs existentes

| Spec | O que muda |
|---|---|
| 009 (papéis e estrutura) | D3 ("autocadastro sem papel") deixa de ser o único estado: passa a existir **tipo solicitado** e **solicitação pendente**. RF4/RF6 (AdminEscola criando usuário e vínculo) passam a ser exclusivos do ADM da unidade |
| 027 (hierarquia) | Fornece unidade, escola e aluno; esta spec não redefine a hierarquia |
| 028 (matriz) | Ganha as linhas "cadastro público" e "solicitação de acesso"; a aprovação é exclusiva do ADM da unidade |
| 031 (CRN) | O CRN do tipo Nutricionista é validado no cadastro e só vira `Usuario.Crn` na aprovação |
| 021 (telas) | A tela de usuários passa a separar **pendentes** de **contas existentes** |

## Decisões e interpretações

| # | Ponto | Decisão | Origem |
|---|---|---|---|
| D1 | Campos do autocadastro | Nome, e-mail, senha e CPF/data de nascimento (invariantes atuais); CRN somente no tipo Nutricionista; unidade obrigatória | Decisão do dono, 2026-11-04 |
| D2 | Quem vê a solicitação | Somente ADMs da unidade escolhida no cadastro | Decisão do dono, 2026-11-04; Spec 028 |
| D3 | Login antes da aprovação | Bloqueado (401); a mensagem não confirma se o e-mail existe | Decisão do dono, 2026-11-04 |
| D4 | Vínculo do responsável | Obrigatório ao menos um aluno da unidade na aprovação; não há etapa separada | Decisão do dono, 2026-11-04 |
| D5 | Papel concedido | Igual ao tipo solicitado; o ADM pode **corrigir** com justificativa registrada, nunca conceder papel administrativo por essa via | RF7 |
| D6 | Tipo solicitado | Imutável; mudança exige nova solicitação | RF2 |
| D7 | CPF | Único por pessoa (recomendado para evitar duplicidade); a regra é nova e está marcada como dúvida no relatório | Integridade |
| D8 | Recusa | Definitiva para aquela solicitação; reconsideração é uma nova solicitação criada pelo ADM | RF10 |

## Requisitos funcionais

- RF1: o cadastro público começa pela pergunta "Qual é o seu tipo de cadastro?" com exatamente
  três opções: `Nutricionista`, `Merendeira`, `Responsavel`. Papéis administrativos não são
  autossolicitáveis.
- RF2: o tipo escolhido é gravado na solicitação (`TipoSolicitado`) e é imutável. Ele **não**
  define `Usuario.Papel` e **não** concede permissão alguma.
- RF3: campos por tipo:
  - comuns: nome (2–100), CPF (11 dígitos), data de nascimento (passada), e-mail válido e único,
    senha (8–100), unidade de destino (existente e ativa);
  - `Nutricionista`: CRN obrigatório e válido pela Spec 031;
  - `Merendeira` e `Responsavel`: CRN proibido;
  - telefone e foto são opcionais.
- RF4: o cadastro cria, na mesma transação, o `Usuario` sem papel (`Papel = null`, `Ativo =
  true`) e uma `SolicitacaoDeAcesso` com `Status = Pendente`, `UnidadeId` escolhida e data UTC.
  E-mail já cadastrado devolve 409 e não cria nada.
- RF5: conta com solicitação `Pendente` ou `Recusada` não autentica. O login devolve 401 com
  mensagem genérica de credenciais/acesso, sem confirmar a existência do e-mail.
- RF6: o ADM da unidade vê somente as solicitações da própria unidade, com filtros por
  `status`, `tipo` e `busca` (nome, e-mail ou CPF) e paginação. Outra unidade responde 404.
- RF7: aprovar grava, em uma única transação: `Usuario.Papel` correspondente ao tipo (ou papel
  corrigido), o escopo institucional, o CRN (quando Nutricionista), os vínculos (quando
  Responsável), o estado `Aprovada`, autor/data e a trilha. Falha em qualquer passo desfaz tudo.
- RF8: escopo por tipo aprovado:
  - `Nutricionista` → `Papel = Nutricionista`, `UnidadeId` da solicitação, `EscolaId = null`,
    `Crn` obrigatório;
  - `Merendeira` → `Papel = Merendeira`, `EscolaId` escolhida e pertencente à unidade,
    `UnidadeId` derivada da escola (Spec 027), `Crn = null`;
  - `Responsavel` → `Papel = Responsavel`, `EscolaId = null`, `UnidadeId` da solicitação
    (origem administrativa), `Crn = null`, vínculos com um ou mais alunos da unidade
    (Spec 027). A unidade do responsável serve apenas à administração do cadastro; a leitura
    continua limitada aos vínculos.
- RF9: aprovar `Responsavel` exige ao menos um aluno ativo da unidade; cada aluno é validado
  pela escola → unidade. Aluno de outra unidade devolve 404; lista vazia devolve 400. Os
  vínculos são criados no mesmo ato, sem etapa posterior.
- RF10: recusar exige `motivo` (5–500), grava `Recusada`, autor/data e trilha. A conta
  permanece sem papel e sem acesso. Reconsiderar exige uma nova solicitação criada pelo ADM
  para o mesmo usuário; a solicitação recusada não muda de estado.
- RF11: pode existir no máximo uma solicitação `Pendente` por usuário. Segunda solicitação
  aberta devolve 409.
- RF12: decisão concorrente: a primeira aprovação/recusa vence; a segunda devolve 409 e não
  altera usuário, vínculos nem trilha.
- RF13: aprovar um tipo com papel corrigido (`papelConcedido` diferente de `TipoSolicitado`)
  exige `justificativaCorrecaoPapel` (10–500), aceita apenas `Nutricionista`, `Merendeira` ou
  `Responsavel`, e registra solicitado × concedido na trilha. As regras de escopo e CRN do papel
  concedido continuam valendo.
- RF14: nenhuma aprovação concede papel administrativo (`AdminSecretaria`/`AdminEscola`);
  essas contas continuam sendo criadas pelo ADM conforme as Specs 009/028.
- RF15: a criação administrativa direta (`POST /api/usuarios`) permanece, cria conta já ativa
  com papel e não passa pela fila. Ela obedece às mesmas regras de integridade de unidade,
  escola, CPF, CRN e escopo.
- RF16: a listagem administrativa separa `Pendentes` (solicitação aberta) de `Contas`
  (aprovadas, recusadas e criadas por administrador). Conta sem papel nunca é exibida como
  sugestão de papel; ela aparece na fila com o tipo solicitado.
- RF17: o cadastro valida que a unidade escolhida existe e está ativa; a escola escolhida na
  aprovação da merendeira deve pertencer a essa unidade. A validação é do backend; o frontend
  apenas antecipa.
- RF18: toda decisão emite notificação `SolicitacaoStatus` ao solicitante (Spec 018) e grava
  na trilha de ação privilegiada (Spec 028, RF11).
- RF19: cardápios publicados continuam anônimos (Spec 012, RF9); a pendência de cadastro não
  impede a consulta pública de cardápio.
- RF20: o endereço do cadastro público é único e não aceita parâmetro de papel administrativo;
  payload com `tipo` desconhecido devolve 400 com a lista aceita.

## Relacionamentos e integridade

- `Unidade` é estrutura administrativa; `Escola` pertence a exatamente uma unidade; `Pessoa` é
  o indivíduo (nutricionista, merendeira, responsável ou aluno). Escola e unidade **não** são
  pessoas e não autenticam.
- `Aluno` é pessoa sem login; `Usuario` é pessoa com login. O mesmo indivíduo pode ser aluno e
  responsável? Não: papéis de aluno e responsável são cadastros distintos, sem fusão
  automática. (Dúvida registrada no relatório.)
- O aluno é vinculado à escola; a unidade é derivada e conferida (Spec 027). O vínculo
  responsável↔aluno é N:N, único por par, e não cria escola no usuário responsável; a unidade
  do responsável é a de origem administrativa e não amplia a leitura.
- O backend valida a cadeia em toda escrita: escola existente e ativa, escola pertencente à
  unidade, aluno pertencente à escola/unidade, usuário do escopo. O frontend não é fonte de
  integridade.

## Requisitos não funcionais

- RNF1: invariantes de estado no domínio; decisão de escopo na aplicação; controller só traduz
  (ADR-004, Spec 024).
- RNF2: aprovação, vínculos, estado e trilha na mesma unidade de trabalho (ADR-019).
- RNF3: consultas da fila filtram unidade no SQL, nunca em memória (Spec 028, I4).
- RNF4: concorrência otimista no estado da solicitação (ADR-010); duas decisões simultâneas
  resultam em uma 200 e uma 409.
- RNF5: nenhum dado pessoal em log; a fila expõe CPF/e-mail apenas ao ADM da unidade.
- RNF6: contrato `{ erro }` em 400/401/403/404/409/422 (ADR-005).

## Restrições

- A Spec 009 está `audited` e é imutável; esta spec declara o que passa a valer e não reescreve
  o documento dela.
- A Spec 021 está `audited`; as telas novas são responsabilidade desta spec.
- Não criar papel novo: o conjunto continua com os cinco valores existentes.
- Não permitir autoaprovação: o solicitante nunca decide a própria solicitação.
- Não criar etapa separada de vínculo após a aprovação do responsável; o endpoint antigo de
  vínculo continua existindo para manutenção administrativa (Spec 027), mas não faz parte do
  fluxo de cadastro.
- Nenhuma dependência nova.

## Dependências

- Spec 009 (papéis e base de autorização), Spec 020 (sessão e contrato de erro).
- Spec 027 (hierarquia e vínculo), Spec 028 (matriz, escopo e trilha).
- Spec 031 (CRN), Spec 018 (notificação), Spec 023 (trilha do aluno, quando houver vínculo).
- Spec 022 (bootstrap) para a existência de unidades e do primeiro ADM.

## Casos de borda

- Unidade inexistente ou inativa no cadastro → 400, nada criado.
- E-mail já cadastrado → 409; CPF já cadastrado → 409 (D7).
- CPF com menos de 11 dígitos ou data futura → 400.
- Nutricionista sem CRN ou com formato inválido → 400.
- Merendeira ou responsável com CRN → 400.
- Solicitação pendente duplicada para o mesmo usuário → 409.
- ADM de outra unidade acessando a solicitação → 404.
- Merendeira aprovada com escola de outra unidade → 400/404 e nada gravado.
- Responsável aprovado sem aluno → 400.
- Responsável com aluno de outra unidade → 404 e nada gravado.
- Aluno inativo selecionado → 400.
- Aprovação concorrente → uma 200 e uma 409.
- Conta pendente tentando login → 401 genérico.
- Conta recusada tentando login → 401 genérico.
- Aprovação de papel administrativo pela fila → 400/403.
- Requisição de vínculo antiga (`POST /api/aluno/{id}/responsaveis/{usuarioId}`) continua
  funcionando para ADM, mas não é necessária no fluxo novo.

## Critérios de aceitação

- [ ] O cadastro exibe a pergunta de tipo e adapta os campos (CRN só para nutricionista).
- [ ] Selecionar tipo não concede papel; a conta nasce sem papel e pendente.
- [ ] Login de conta pendente devolve 401 sem confirmar existência do e-mail.
- [ ] A fila mostra apenas solicitações da unidade do ADM, com filtro por tipo.
- [ ] Aprovar nutricionista grava papel, unidade e CRN; sem escola.
- [ ] Aprovar merendeira exige escola da unidade e grava escola + unidade derivada.
- [ ] Aprovar responsável exige ao menos um aluno e grava todos os vínculos no mesmo ato.
- [ ] Nenhuma aprovação grava estado parcial quando um passo falha.
- [ ] Recusa exige motivo e mantém a conta sem acesso.
- [ ] Duas decisões concorrentes: uma 200 e uma 409.
- [ ] Nenhuma aprovação concede papel administrativo.
- [ ] Cardápio publicado continua acessível sem sessão.
- [ ] A criação administrativa direta continua funcionando e respeita a mesma integridade.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Cadastrar um nutricionista, uma merendeira e dois responsáveis na mesma unidade, com um aluno
em cada uma de duas escolas. Conferir a fila por tipo e o bloqueio de login. Aprovar cada um com
o escopo correto, vincular os alunos do responsável no mesmo ato e recusar uma solicitação com
motivo. Repetir aprovação concorrente, escola de outra unidade e aluno de outra unidade.
Executar os gates do projeto.
