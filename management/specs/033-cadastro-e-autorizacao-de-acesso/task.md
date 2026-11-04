---
name: cadastro-e-autorizacao-de-acesso
status: created
references: V22, V17, ADR-021
---

# Tasks — Cadastro e autorização de acesso

## Pré-condição

- [ ] T0: Specs 020 (sessão/erro), 027 (hierarquia), 028 (matriz e trilha) e 031 (CRN)
  implementadas.
  - Validação: login por papel funciona; aluno devolve escola; trilha disponível; CRN validado.

## Domínio e persistência

- [ ] T1: criar `TipoCadastroSolicitado`, `StatusSolicitacaoAcesso` e `SolicitacaoDeAcesso`
  com as invariantes da spec.
  - Validação: tipo imutável; transição dupla lança; papel administrativo lança.
- [ ] T2: criar `PoliticaDeAprovacao` pura (tipo → papel/escopo/CRN/vínculo).
  - Validação: cada tipo produz o plano correto e os erros esperados.
- [ ] T3: criar a migration `SolicitacoesDeAcesso` com FKs, índice único filtrado de pendente e
  índice da fila.
  - Validação: duas pendentes para o mesmo usuário violam o índice; banco limpo aplica.
- [ ] T4: ajustar `Usuario` para o modelo da Spec 027 (Responsável sem escola/unidade;
  Nutricionista e AdminSecretaria com unidade; CRN por papel da Spec 031) e validar CPF.
  - Validação: papéis e escopos da matriz 028 aceitos; CPF inválido recusado.

## Registro público

- [ ] T5: criar `CadastroEntrada` e `RegistrarAcessoService`; estender
  `POST /api/usuario/registrar` com `tipo`, `unidadeId` e `crn`.
  - Validação: 400 para tipo/CRN inválido; 201 cria usuário sem papel + solicitação pendente.
- [ ] T6: validar e-mail e CPF únicos no serviço e devolver 409 sem gravar.
  - Validação: duplicidade em cada campo; nenhuma linha criada.
- [ ] T7: impedir login de conta com solicitação pendente ou recusada, com 401 genérico.
  - Validação: `Login_Pendente_401`; mensagem não confirma e-mail.

## Fila e decisão

- [ ] T8: criar consultas da fila por unidade, filtros `status`/`tipo`/`busca` e paginação.
  - Validação: outra unidade devolve 404; filtro por tipo funciona.
- [ ] T9: criar `POST /{id}/aprovar` com validação prévia, transação única, vínculos, trilha e
  notificação.
  - Validação: nenhum estado parcial quando o vínculo falha; notificação emitida.
- [ ] T10: implementar merendeira (escola da unidade), nutricionista (unidade + CRN) e
  responsável (≥1 aluno da unidade) com os erros da tabela.
  - Validação: 400/404 por escola e aluno de outra unidade; vínculos únicos.
- [ ] T11: criar `POST /{id}/recusar` com motivo, estado e trilha.
  - Validação: motivo obrigatório; conta continua sem acesso.
- [ ] T12: aplicar concorrência otimista e índice de pendente; decisão repetida devolve 409.
  - Validação: duas decisões simultâneas → uma 200 e uma 409.
- [ ] T13: criar reconsideração (`POST /api/solicitacoes-acesso`) e diagnóstico de contas
  legadas sem solicitação.
  - Validação: segunda pendente 409; legado listado sem inventar tipo.
- [ ] T14: aplicar a matriz da Spec 028 na fila (somente ADM da unidade; sem papel
  administrativo).
  - Validação: `MatrizPermissoesTests` cobre `SolicitacaoDeAcesso` e `CadastroPublico`.

## Frontend

- [ ] T15: cadastro em duas etapas com pergunta de tipo, campos condicionais, unidade e CRN;
  confirmação de pendência.
  - Validação: `npm run build`/`npm run lint`; cada tipo envia o payload correto.
- [ ] T16: tela de fila com abas por tipo, busca e detalhe; modal de aprovação com escola e
  multi-select de alunos; recusa com motivo.
  - Validação: aprovar responsável cria vínculos e a tela reflete o resultado.
- [ ] T17: remover a sugestão de papel para contas sem papel em `Usuarios.tsx`.
  - Validação: pendentes aparecem apenas na fila, com o tipo solicitado.

## Fechamento

- [ ] T18: atualizar `README.md` (rotas e fluxo) e a seção da spec em `arch.md`.
  - Validação: `DocumentationTests.Readme_ListaTodosOsEndpoints` verde.
- [ ] T19: executar os gates completos.
  - Validação: `dotnet build`, `dotnet test`, `npm run build`, `npm run lint`.

## Definição de pronto

- O cadastro registra o tipo e nunca concede acesso por si.
- A fila é da unidade e decide com um único ato.
- Responsável só é aprovado com ao menos um aluno vinculado na mesma transação.
- Nenhuma decisão deixa estado parcial ou concede papel administrativo.
- Login de conta pendente é 401 sem revelar existência.
