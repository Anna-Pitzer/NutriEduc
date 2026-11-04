---
name: hierarquia-unidade-escola-aluno
status: implemented
references: V16, V22, ADR-020, ADR-013, L11
---

# Requirements — Hierarquia rede → unidade → escola → aluno

## Objetivo

Restabelecer uma cadeia institucional única. Hoje a ADR-013 fez da unidade uma entidade da
rede e a escola deixou de pertencer a uma unidade, então o aluno não tem escola alcançável
(`Aluno.UnidadeId` é o único vínculo), `AdminEscola` e `Merendeira` não têm escopo de escola
verificável e o vínculo aluno→escola está registrado como PEND-024. Esta spec resolve a
causa: `Escola` pertence a uma unidade, `Aluno` pertence a uma escola e todo escopo de dado
é derivado dessa cadeia.

## Escopo

- Backend: `Escola.UnidadeId`, `Aluno.EscolaId`, escopo institucional de `Usuario`, bootstrap
  de unidade inicial, filtros de consulta por unidade e escola.
- Frontend: seleção unidade → escola no cadastro de escola e de aluno; filtros por escola.
- Fora de escopo: matriz de permissões e regras de isolamento (Spec 028), cardápios
  (Spec 029), solicitações (Spec 030), catálogo de alimentos e restrições (Specs 010 e 011).

### Cobertura de `implementacoes.md`

- **Secretaria:** CRUD de escolas dentro da própria unidade; informações gerais das escolas.
- **Escolas:** informações da própria escola; visualização dos alunos da própria escola.
- **Merendeiras:** alunos, restrições e cardápios da própria escola, em leitura.
- **Pais:** alunos vinculados ao próprio responsável, sem escopo institucional próprio.

Esta spec **transfere** para si a RF3 (parte de escolas/unidades) e a RF6 (vínculo) da
Spec 009 e o pré-requisito de unidade existente da Spec 022. Não declara de novo o que já é
coberto por 010–019 (L12).

## Decisões e interpretações

| # | Regra de negócio recebida | Interpretação adotada | Origem |
|---|---|---|---|
| D1 | "ADM" possui acesso geral dentro da sua unidade administrativa | `AdminSecretaria` é o ADM; seu escopo é a própria unidade | ADR-021 |
| D2 | Unidade tem escolas; escola tem alunos | `Escola.UnidadeId` e `Aluno.EscolaId` obrigatórios para novos registros | ADR-020 |
| D3 | Pais vinculados aos filhos → escola → unidade | `Responsavel` guarda apenas a **unidade administrativa de origem** (a que aprovou); o escopo de dados vem do vínculo e não usa essa unidade | ADR-021, Spec 033 |
| D4 | "O primeiro administrador e a unidade inicial vêm do bootstrap" | Bootstrap cria a unidade quando ela não existe; token vale uma vez por unidade | ADR-020, Spec 022 |
| D5 | "Escola só vê dados da própria escola" | `AdminEscola` e `Merendeira` exigem `EscolaId`; unidade é derivada | ADR-020 |
| D6 | "Uma unidade não vê outra" | Fronteira entre unidades responde 404; `AdminSecretaria` nunca consulta outra unidade | ADR-021, Spec 028 |
| D7 | "Pessoa" é um dos conceitos | `Usuario` e `Aluno` são pessoas; `Escola` e `Unidade` são estruturas e não autenticam. Nenhum usuário "é" uma escola ou unidade | Regra do dono |
| D8 | Integridade de relacionamento | A cadeia unidade→escola→aluno é validada no backend **e** no banco; o frontend apenas antecipa | Regra do dono, Spec 033 |

## Requisitos funcionais

- RF1: `Unidade` é entidade da rede, com nome normalizado (2–120) único na rede e
  `Ativo`. Unidades são criadas **somente** pelo bootstrap (RF8); o ADM edita apenas a
  própria unidade. Supera a RF3 da Spec 009 no ponto "AdminSecretaria cria unidades".
- RF2: `Escola` tem `UnidadeId` obrigatório, nome normalizado (2–120) único **dentro da
  unidade** e `Ativo`. Duas escolas de unidades diferentes podem ter o mesmo nome. A
  existência e a atividade da unidade são verificadas no backend; a FK e o índice garantem a
  integridade no banco.
- RF3: `Aluno` tem `EscolaId` obrigatório em criação e atualização; `Escola` deve existir,
  estar ativa e pertencer a uma unidade ativa. O payload de aluno **não** aceita `unidadeId`
  nem `escola` em texto: a unidade é derivada e conferida contra `Escola.UnidadeId`.
- RF4: `Aluno.UnidadeId` é denormalizado, com escritor único no domínio, sempre derivado de
  `Escola.UnidadeId`. Divergência entre `Aluno.UnidadeId` e a unidade da escola é estado
  inválido e não pode ser produzido por nenhum caminho de escrita.
- RF5: mudar `Escola.UnidadeId` é proibido enquanto a escola possuir alunos (ativos ou
  inativos). Movimentação de escola exige escola nova ou transferência aluno a aluno (RF11).
- RF6: vínculo institucional obrigatório por papel, validado no construtor e em
  `DefinirPapel`:
  `AdminSecretaria → UnidadeId`; `AdminEscola → EscolaId`; `Merendeira → EscolaId`;
  `Nutricionista → UnidadeId`; `Responsavel → UnidadeId (origem administrativa), sem EscolaId`.
  **Ajuste obrigatório:** a implementação atual de `Usuario.DefinirPapel` exige escola para
  `Responsavel` e trata `Nutricionista`/`AdminSecretaria` como globais; essa regra é
  substituída por esta spec e pela Spec 028.
- RF7: `Responsavel` é cadastrado pelo ADM da unidade e vinculado a um ou mais alunos por
  `VinculoResponsavelAluno` (N:N, único por par). O vínculo é a **única** autorização de
  leitura do responsável e não transfere escopo de escola nem de unidade.
- RF8: o bootstrap cria a unidade inicial quando ela não existir e o primeiro
  `AdminSecretaria` vinculado a ela, uma única vez por unidade, com o token da Spec 022.
  Deixa de ser pré-requisito que a unidade já exista.
- RF9: todo escopo de consulta é resolvido no servidor a partir da sessão e da cadeia
  `usuário → (unidade | escola | vínculo) → aluno`. Nenhuma rota aceita `unidadeId` do
  cliente como autorização.
- RF10: leitura de aluno por papel: ADM vê a própria unidade; `AdminEscola` e `Merendeira`
  veem a própria escola; `Nutricionista` vê a própria unidade; `Responsavel` vê os alunos
  vinculados. O detalhamento de escrita e de outros recursos é da Spec 028.
- RF11: o ADM transfere aluno entre escolas **da mesma unidade**; a operação é registrada na
  trilha da Spec 023 com escola de origem e destino. Transferência entre unidades é
  proibida nesta spec.
- RF12: exclusão de unidade, escola e aluno é desativação (ADR-008). Escola com alunos ou
  usuários vinculados não é excluída fisicamente.
- RF13: as rotas existentes preservam nome e formato; criação e edição de aluno passam a
  exigir `escolaId`; listagens aceitam o filtro `escolaId` além de `unidadeId`.
- RF14: `GET /api/usuario/me` devolve `papel`, `unidadeId` e `escolaId` derivados da cadeia;
  sessão sem vínculo obrigatório para o papel não autentica.
- RF15: `Pessoa` é o indivíduo; `Usuario` é a pessoa com login e `Aluno` é a pessoa sem
  login. `Escola` e `Unidade` não são pessoas e não possuem credenciais. Nenhum cadastro de
  pessoa cria, renomeia ou substitui escola ou unidade.
- RF16: o vínculo responsável↔aluno não grava `EscolaId` no usuário responsável; a
  `UnidadeId` é apenas a origem administrativa (unidade que aprovou) e não amplia a leitura,
  que continua limitada ao conjunto de alunos vinculados (RF7).
- RF17: toda referência cruzada é verificada antes da escrita e protegida no banco: escola
  pertencente à unidade, aluno pertencente à escola, usuário do papel no escopo. Violação
  devolve 400/404 e não grava estado parcial.

## Requisitos não funcionais

- RNF1: filtro de unidade e de escola aplicado no banco, nunca em memória (Spec 009, RNF4).
- RNF2: invariantes de RF2–RF6 no domínio (ADR-004); controller não conhece regra.
- RNF3: migration retrocompatível. Colunas novas são anuláveis no banco para registros
  legados; o domínio exige o vínculo em todo registro novo ou atualizado. Comando de
  diagnóstico lista alunos sem escola e usuários sem vínculo.
- RNF4: nomes de unidade e escola são normalizados (trim + caixa única) antes de comparar
  ou gravar.
- RNF5: mudança de papel, unidade ou escola invalida a sessão na requisição seguinte.
- RNF6: nenhum cache de escopo é compartilhado entre requisições de unidades diferentes.

## Restrições

- Migration nova; migrations existentes não são reescritas.
- `Aluno.Turma` e `Aluno.Turma.Escola` (texto) continuam como rótulo de exibição; a fonte de
  verdade institucional é `Aluno.EscolaId`.
- `Aluno.UnidadeId` não é removida nesta spec (compatibilidade de filtros e relatórios);
  passa a ser denormalizada e somente leitura para a aplicação.
- ADR-020 supera a ADR-013. As Specs 009 (RF3 parcial, RF6), 021 (telas de escola) e 022
  (pré-requisito de unidade) devem ser lidas com esta spec como referência de escopo.
- Não alterar o formato do cookie de forma incompatível: apenas acrescentar/derivar claims.

## Dependências

- Spec 009 (papéis e estrutura) — entidades e autorização base.
- Spec 022 (bootstrap por token) — fluxo de criação do administrador inicial.
- Spec 023 (histórico do aluno) — registro da transferência entre escolas.
- Spec 028 (matriz de permissões) — consome os escopos definidos aqui.
- Spec 033 (cadastro e autorização) — usa a cadeia no ato de aprovação e no vínculo do
  responsável.
- Spec 010 (restrições) e Spec 012 (cardápio) — leitura por unidade/escola.

## Casos de borda

- Unidade sem escola: estado válido; nenhum aluno pode ser criado até existir escola ativa.
- Aluno legado sem escola: leitura apenas pelo ADM da unidade derivada; não aparece para
  `AdminEscola` nem `Merendeira`; criação/edição bloqueada com 409 até o backfill.
- Escola desativada: leitura histórica permitida ao ADM; não recebe aluno nem vínculo novo.
- Responsável com filhos em duas unidades: vê os dois filhos (o vínculo autoriza), mas
  nenhum dado de escola, turma ou colega; cada unidade vê apenas o vínculo do próprio aluno.
- Nutricionista legado sem unidade: não cria nem lê cardápio até receber unidade.
- Unidade desativada: ADM autentica e lê, mas não escreve.
- Mudança de papel para `AdminEscola`/`Merendeira` sem escola informada: recusada com 400 no
  ato, sem gravar papel parcial.
- Duas escolas com o mesmo nome em unidades diferentes: permitido; a unicidade é por unidade.

## Critérios de aceitação

- [ ] `POST /api/escolas` sem `unidadeId` devolve 400 e não grava.
- [ ] `POST /api/aluno` sem `escolaId` devolve 400 e não grava.
- [ ] Aluno criado com `escolaId` devolve `unidadeId` igual à unidade da escola.
- [ ] Nenhum payload de aluno aceita `unidadeId`; quando enviado, é ignorado e a resposta
  nunca diverge da escola.
- [ ] `GET /api/aluno` de `AdminEscola` devolve somente alunos da própria escola.
- [ ] `GET /api/aluno` de `AdminSecretaria` devolve somente alunos da própria unidade.
- [ ] `GET /api/aluno` de `Merendeira` devolve somente alunos da própria escola.
- [ ] `GET /api/aluno/{id}` de `Responsavel` vinculado devolve 200; não vinculado devolve 403.
- [ ] Aluno de outra unidade respondendo a `AdminSecretaria` devolve 404.
- [ ] Bootstrap sem unidade cadastrada cria a unidade e o primeiro `AdminSecretaria`.
- [ ] Segundo bootstrap para a mesma unidade devolve conflito e não cria usuário novo.
- [ ] Escola com alunos não muda de unidade (409) e o estado permanece íntegro.
- [ ] `dotnet build`, `dotnet test`, `npm run build` e `npm run lint` sem erro novo.

## Validação

Criar uma unidade pelo bootstrap, duas escolas na mesma unidade e uma escola em outra
unidade. Cadastrar alunos nas três escolas, um `AdminEscola`, uma `Merendeira`, um
`Nutricionista` e um responsável com dois filhos em escolas diferentes. Autenticar com cada
papel e confirmar: 200 no próprio escopo, 403 na fronteira de escola, 404 na fronteira de
unidade, 401 sem sessão. Executar os gates do projeto e o comando de diagnóstico de legado.

## Ajustes — 2026-11-04

A aprovação do cadastro e o vínculo do responsável passam a acontecer no mesmo ato (Spec 033).
`Pessoa` fica distinta de `Escola` e `Unidade`. A implementação de `Usuario.DefinirPapel` deve ser
alinhada à RF6: `Responsavel` sem escola/unidade; `Nutricionista` e `AdminSecretaria` com
unidade, sem escopo global.
