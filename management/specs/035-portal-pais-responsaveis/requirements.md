---
name: portal-pais-responsaveis
status: created
references: V24; V6; V17; V23; Spec 010; Spec 016; Spec 017; Spec 018; Spec 025; Spec 029; Spec 033; Spec 034; ADR-009; ADR-021
---

# Requirements — Portal dos Pais/Responsáveis

## Value Delivery

| Value block | O que a spec entrega | Cobertura |
| --- | --- | --- |
| V24 (portal dos pais) | Painel dos filhos, cardápio servido por filho/dia/refeição, leitura e relato de ocorrências, solicitação de alergia com acompanhamento e feedback com aluno opcional | 100% |
| V6 (escopo de produto) | Reutiliza ocorrências, solicitações, feedback e cardápio já implementados; não cria domínio paralelo | Suporte |
| V17 (permissão e isolamento) | Toda leitura/escrita continua passando pela matriz e pelo vínculo; nenhuma permissão nova | Suporte |
| V23 (vínculo responsável↔aluno) | Pré-requisito já implementado; a spec não refaz cadastro, autorização ou vínculo | Dependência |

> V6, V17 e V23 são restrições de suporte, não valores entregues por esta spec. O painel
> agregado da família (V14) continua na Spec 025.

### Vertical Slice

A spec cobre as quatro camadas de cada funcionalidade: **UI** (`pages/pais/` e componentes),
**API** (contratos existentes + extensão do feedback), **dados** (EF Core/SQLite; única mudança
é `Feedback.AlunoId`/`AlunoNome`) e **resposta → UI** (DTOs renderizados e estados de erro).
Não há camada de autorização nova: a matriz e o vínculo já existentes são reutilizados e
verificados por testes de integração.

## Objetivo

Entregar a área operacional dos pais/responsáveis em `/pais`: consultar o cardápio servido a cada
filho, consultar e relatar ocorrências, solicitar inclusão de alergia/restrição e enviar feedback.
O cadastro, a autorização e a vinculação responsável↔aluno já foram entregues pelas Specs 033 e
034 e **não fazem parte desta spec**.

## Scope

- Frontend: `/pais` com painel dos filhos e páginas de cardápios, ocorrências, solicitações de
  alergia e feedback.
- Backend: reutilizar os contratos existentes; a única mudança de modelo é persistir a associação
  opcional do feedback a um aluno.
- Permissões: todo acesso continua limitado aos alunos vinculados ao responsável.

## Out of Scope

- Refazer cadastro, aprovação de acesso, vínculo, matriz de permissões ou autenticação.
- Criar endpoint agregado de família (Spec 025); essa agregação continua na 025 e não é consumida nesta spec.
- Alterar diretamente o cadastro do aluno; toda mudança de restrição passa por solicitação e
  aprovação.
- Criar itens no catálogo de restrições (exclusivo da Nutricionista, Spec 010).
- Relatórios de gestão (Spec 019), substituição assistida por IA (Spec 026) e relatório do filho
  (Spec 025).

## Current State and Reuse

| Funcionalidade | Contrato existente | Lacuna desta spec |
| --- | --- | --- |
| Filhos vinculados | `GET /api/aluno` devolve somente os alunos do responsável | Não há tela “meus filhos” nem seletor de filho na área de pais |
| Cardápio por filho | `GET /api/aluno/{id}/cardapio?data=yyyy-MM-dd` devolve a versão servida (`CardapioServidoDTO`) | `HomePais` só exibe cardápios públicos por unidade; falta a visão por aluno e o filtro por refeição |
| Ocorrências — leitura | `GET /api/ocorrencias` aceita `alunoId`, `tipo`, `status`, `de`, `ate`, `pagina`, `tamanho`; o serviço filtra por vínculo | Existe apenas a tela compartilhada `/ocorrencias` |
| Ocorrências — relato | `POST /api/ocorrencias` aceita `Responsavel` com tipo `Aluno`; autor, aluno, unidade e escola são derivados no servidor | Falta formulário dedicado na área de pais |
| Alergias/restrições | `POST /api/solicitacoes` com `InclusaoAlergia`/`AlteracaoRestricao`; aprovação aplica a restrição e grava histórico | Falta tela de solicitação e acompanhamento na área de pais |
| Feedback | `POST/GET /api/feedbacks`; responsável vê só o próprio; nutrição/escola responde; autor encerra | `AlunoId` só deriva a escola e **não é persistido**; falta seletor de filho |
| Navegação | `Navbar` exibe “Pais” para `Responsavel`; `homePorPapel` leva a `/pais` | Não há sub-rotas nem layout próprio |

## Functional Requirements

Cada requisito é verificável por endpoint, campo ou comportamento observável. “Filho” significa
aluno com vínculo ativo com o responsável autenticado.

### Cardápios

- **RF1:** `/pais` lista os filhos com `id`, `nome`, `escola`, `serie` e `restricoes`, ordenados
  por `nome` com `localeCompare("pt-BR")`; a lista vem de `GET /api/aluno` sem filtro adicional.
- **RF2:** a página de cardápios exige filho e data; o seletor de filho contém exatamente os
  alunos de RF1; a data usa `yyyy-MM-dd` montada com `getFullYear()`, `getMonth()+1` e
  `getDate()` do dispositivo, inicializada com a data local atual.
- **RF3:** a consulta chama `GET /api/aluno/{id}/cardapio?data={data}` e renderiza
  `CardapioServidoDTO`: `alunoId`, `alunoNome`, `escolaId`, `conjuntoRestricoesChave`,
  `disponivel`, `motivo`, `alerta` e `cardapio`; aluno fora do vínculo responde `403`.
- **RF4:** quando `disponivel=true`, a tela lista todas as `refeicoes` do `cardapio`, com
  `nome`, `itens` (`alimento`, `gramas`, `valores`) e `total`, e permite filtrar por uma refeição;
  o filtro “Todas” é o padrão.
- **RF5:** quando `disponivel=false`, a tela exibe `motivo` e `alerta`, não renderiza refeições e
  não substitui pelo cardápio geral.
- **RF6:** quando o cardápio é adaptado, a tela exibe as restrições do `cardapio.restricoes`;
  quando é geral, a seção de restrições não é renderizada.

### Ocorrências — leitura

- **RF7:** a lista chama `GET /api/ocorrencias` com `alunoId` opcional, `tipo`, `status`, `de`,
  `ate`, `pagina` e `tamanho=20` e renderiza `OcorrenciaResumoDTO`: `id`, `tipo`, `status`,
  `alunoId`, `aluno`, `descricao`, `criadoEm`; `alunoId` vazio significa “todos os filhos”; a API
  já filtra pelo vínculo e a tela não amplia o recorte.
- **RF8:** o filtro de filho contém apenas os alunos de RF1; tipo e status usam os enums
  existentes; `de`/`ate` são datas `yyyy-MM-dd`; a paginação mostra total e página atual.
- **RF9:** o detalhe chama `GET /api/ocorrencias/{id}` e exibe `OcorrenciaDTO` com descrição,
  tipo, status, aluno, data, histórico (`status`, `autor`, `observacao`, `criadoEm`), `autor`
  (nome) e `podeCancelar`; não exibe id de autor.
- **RF10:** ocorrência de aluno não vinculado não aparece na lista; acesso direto por id
  responde `403` sem revelar existência.

### Ocorrências — relato

- **RF11:** o formulário exige um filho de RF1 e `descricao` com 5–1000 caracteres; não oferece
  tipos diferentes de `Aluno`.
- **RF12:** o envio chama `POST /api/ocorrencias` com `{ tipo: "Aluno", alunoId, descricao }`; o
  backend associa `AutorId`, `AlunoId`, `UnidadeId`, `EscolaId` e snapshots de nomes.
- **RF13:** após `201`, a tela mostra confirmação, limpa o formulário e recarrega a lista; a
  ocorrência aparece para o autor e para os papéis com leitura no escopo (matriz 028).
- **RF14:** a ocorrência criada pelo responsável pode ser cancelada por ele somente enquanto
  `status=Aberta` e no mesmo dia civil da criação em `America/Sao_Paulo`, via
  `POST /api/ocorrencias/{id}/cancelar`; o botão só aparece quando `podeCancelar=true`; fora
  dessas condições a API responde `409`.

### Solicitação de inclusão de alergia/restrição

- **RF15:** o formulário exige filho de RF1, um `restricaoNome` ativo de `GET /api/restricoes` e
  `descricao` com 5–1000 caracteres; não aceita texto livre.
- **RF16:** o envio chama `POST /api/solicitacoes` com
  `{ tipo: "InclusaoAlergia", alunoId, restricaoNome, descricao }`; o backend grava
  `SolicitanteId`, `AlunoId`, `RestricaoNome` e snapshot do nome do aluno.
- **RF17:** o envio responde `201` e **não** altera `Aluno.Restricoes`; a restrição só é
  adicionada na aprovação de `AdminSecretaria`, com evento `RestricaoAdicionada` na trilha.
- **RF18:** a tela do responsável não oferece aprovar/recusar; a decisão é de `AdminSecretaria`
  da unidade e a `Nutricionista` mantém leitura, conforme Specs 017/028.
- **RF19:** a lista chama `GET /api/solicitacoes?alunoId=&tipo=&status=&pagina=&tamanho=20` e
  exibe `id`, `tipo`, `status`, `aluno`, `restricaoNome`, `descricao`, `resposta`, `criadoEm`; a
  API devolve apenas pedidos do próprio responsável e dos filhos vinculados.
- **RF20:** o responsável cancela o próprio pedido com `POST /api/solicitacoes/{id}/cancelar`
  somente quando `status=Aberta`; em `EmAnalise` ou estado final a API responde `409`. Pedido
  duplicado do mesmo aluno/campo responde `409` e a tela exibe a mensagem com o id existente.

### Feedback

- **RF21:** o formulário exige `assunto` (5–120) e `mensagem` (5–1000) e oferece filho opcional
  (incluindo a opção “Sem aluno”); o envio chama `POST /api/feedbacks` com `{ assunto, mensagem,
  alunoId? }`.
- **RF22:** quando `alunoId` é informado, o backend valida o vínculo e persiste `Feedback.AlunoId`
  e `Feedback.AlunoNome` (snapshot), além de derivar `EscolaId`/`UnidadeId`; aluno fora do vínculo
  responde `403`.
- **RF23:** a lista chama `GET /api/feedbacks?status=&pagina=&tamanho=20` e exibe somente os
  feedbacks do autor: `id`, `assunto`, `mensagem`, `alunoId`, `aluno`, `status`, `resposta`,
  `respondidoPor`, `criadoEm`, `podeEncerrar`.
- **RF24:** quando `podeEncerrar=true`, o autor encerra com `POST /api/feedbacks/{id}/encerrar`;
  responder é exclusivo de `Nutricionista`/`AdminEscola` e não aparece na área de pais.
- **RF25:** o feedback não exibe nem envia dado clínico do aluno; o aluno é apenas referência de
  contexto e a mensagem é texto livre.

### Regras gerais de interface

- **RF26:** com mais de um filho, toda ação de aluno exige seleção explícita; com exatamente um
  filho, o seletor é pré-selecionado e permanece editável.
- **RF27:** nenhuma tela, lista ou detalhe exibe aluno fora do vínculo; o frontend não aplica
  filtro de autorização por conta própria.
- **RF28:** a área usa o layout visual existente (Header, Navbar, Footer, Tailwind, tipografia e
  cores) e um `LayoutPais` com abas Início, Cardápios, Ocorrências, Solicitações e Feedback.
- **RF29:** estados vazios são explícitos para: sem filhos, sem cardápio na data, sem ocorrências,
  sem solicitações e sem feedback; cada estado tem texto e botão “Voltar para o início”
  (`/pais`) ou “Limpar filtros”.
- **RF30:** toda escrita exibe confirmação de sucesso e atualiza a lista/detalhe local; erro exibe
  a mensagem do backend sem perder os dados digitados.
- **RF31:** o cabeçalho reutiliza `SinoNotificacoes` existente; nenhum comportamento novo de
  notificação é criado.
- **RF32:** toda data exibida usa `dd/MM/yyyy` e todo envio à API usa `yyyy-MM-dd`.

## Non-Functional Requirements

- **NFR1 (superfície HTTP):** nenhuma rota nova; o número de controllers e rotas permanece o
  atual, exceto os campos/filtro do feedback. Verificação: `DocumentationTests` e diff de rotas.
- **NFR2 (dependências):** nenhuma dependência nova em `backend.csproj` ou `frontend/package.json`.
- **NFR3 (chamadas iniciais):** cada página faz no máximo 3 requisições iniciais: filhos
  (`GET /api/aluno`), listagem do recurso e catálogo (somente em solicitações); cardápios faz 1
  requisição inicial e a segunda somente após selecionar filho/data; paginação e filtros só
  disparam novas requisições quando acionados.
- **NFR4 (paginação):** listas usam `tamanho=20`; o máximo aceito pelo backend (100) não é
  ultrapassado; a UI exibe total e página.
- **NFR5 (build):** `npm run build` termina com 0 erros TypeScript; `npm run lint` termina com 0
  erros (avisos pré-existentes permitidos).
- **NFR6 (testes):** os 792 testes existentes continuam passando; novos testes de integração do
  feedback passam; 0 falhas.
- **NFR7 (autorização):** 100% das decisões de acesso são do backend; nenhuma tela decide escopo
  por conta própria. Verificação: revisão de código + testes de integração de `403`.
- **NFR8 (acessibilidade):** todos os inputs têm `label`/`aria-label`, são alcançáveis por teclado
  e têm foco visível; formulários exibem erro associado ao campo.
- **NFR9 (idioma e formato):** textos em pt-BR; números e medidas formatados com
  `toLocaleString("pt-BR")`; datas conforme RF32.
- **NFR10 (privacidade):** nenhum CPF, senha, dado clínico ou texto de ocorrência/feedback é
  enviado em URL, log ou mensagem de erro.
- **NFR11 (migração):** exatamente 1 migration nova (`FeedbackAluno`); nenhuma migration
  existente é editada.
- **NFR12 (arquivos):** a implementação altera somente os arquivos listados em `design.md` §File /
  Module Layout; qualquer arquivo extra exige revisão desta spec antes da implementação.

## Constraints

- **C1:** a implementação desta spec é uma etapa posterior; a criação/verificação da spec não
  altera código de aplicação.
- **C2:** stack fixa: React 19 + TypeScript + Vite + Tailwind no frontend; ASP.NET Core 10 +
  EF Core + SQLite no backend.
- **C3:** contratos de ocorrência, solicitação, cardápio e restrição não mudam; o feedback é a
  única extensão de contrato.
- **C4:** uma única migration, apenas para o feedback.
- **C5:** não duplicar a Spec 025; nenhum endpoint de painel agregado.
- **C6:** alergia usa somente item ativo do catálogo; texto livre está fora de escopo.
- **C7:** a matriz da Spec 028 é autoritativa; nenhuma célula nova.
- **C8:** o frontend não importa biblioteca de estado global; usa estado local e serviços
  existentes.
- **C9:** TypeScript sem `any` explícito nos novos arquivos.
- **C10:** o padrão visual e os componentes existentes são reutilizados; não se cria design
  system paralelo.
- **C11:** arquivos e identificadores seguem o padrão do projeto; documentação em pt-BR.

## Architecture Decisions (resolved)

- **DA1 — Catálogo apenas.** O pedido de alergia referencia um item ativo do catálogo; nome livre
  é recusado. Motivo: o catálogo é dado de referência da Nutricionista (Spec 010) e texto livre
  exigiria criação de item por solicitação, fora do escopo.
- **DA2 — Independente da Spec 025.** A 035 usa endpoints por aluno e não depende da 025 para ser
  implementada; a troca para agregação, se ocorrer, pertence à própria 025 e não altera as páginas
  desta spec.
- **DA3 — Feedback persiste o aluno.** `AlunoId` + `AlunoNome` são gravados quando informado;
  sem aluno, ambos ficam nulos. Motivo: o requisito pede associação e o snapshot preserva o
  histórico após renomeação/exclusão (ADR-009).
- **DA4 — Decisão da alergia.** `AdminSecretaria` decide; `Nutricionista` lê. Motivo: comportamento
  atual das Specs 017/028; mudar o dono exigiria ADR e nova matriz.

## Permissions

| Recurso/ação | Responsável | AdminSecretaria | Nutricionista | AdminEscola | Merendeira |
| --- | --- | --- | --- | --- | --- |
| Ler alunos vinculados | sim | unidade | unidade | escola | escola |
| Ler cardápio servido do filho | sim | unidade | unidade | escola | escola |
| Ler ocorrências dos filhos | sim | unidade | unidade | escola | escola |
| Criar ocorrência tipo `Aluno` | sim (filhos) | — | sim | sim | sim |
| Criar solicitação de alergia | sim (filhos) | — | — | — | — |
| Decidir solicitação de alergia | — | sim (unidade) | leitura | — | — |
| Criar feedback | sim | — | — | sim | sim |
| Responder feedback | — | — | sim | sim | — |
| Encerrar o próprio feedback | sim | — | — | — | — |

> A matriz da Spec 028 continua autoritativa. Divergência entre esta tabela e a matriz é bug da
> matriz e deve ser resolvida nela, com teste.

## Flows

1. **Entrar na área:** login → `homePorPapel["Responsavel"] = "/pais"` → `GET /api/aluno` →
   cartões dos filhos; sem filhos, estado vazio e ações desabilitadas.
2. **Ver cardápio:** filho + data → `GET /api/aluno/{id}/cardapio?data=` → renderizar refeições
   (filtro local) ou `Indisponivel` com motivo.
3. **Ver ocorrências:** filho (ou todos) + filtros →
   `GET /api/ocorrencias?alunoId=&tipo=&status=&de=&ate=&pagina=&tamanho=20` → lista paginada →
   detalhe por `GET /api/ocorrencias/{id}`.
4. **Relatar ocorrência:** filho + descrição → `POST /api/ocorrencias` → confirmação → lista.
5. **Solicitar alergia:** filho + item do catálogo + descrição → `POST /api/solicitacoes` →
   acompanhar status; aprovação do ADM aplica a restrição e notifica.
6. **Feedback:** assunto + mensagem + filho opcional → `POST /api/feedbacks` → lista própria;
   autor encerra quando `podeEncerrar=true`.

## Edge Cases & Error States

| Condição | Resposta da API | Comportamento da UI |
| --- | --- | --- |
| Sem sessão | `401` | Logout e redirecionamento para `/login` |
| Responsável sem filhos | `200` com lista vazia | Estado vazio; ações de aluno desabilitadas |
| Aluno de outro responsável na URL | `403` | Mensagem genérica de acesso negado; não revelar existência |
| Aluno inexistente | `404` | Estado “não encontrado” com retorno |
| Data inválida (`2026-02-30`) | `400` | Erro no campo; não enviar |
| Sem cardápio publicado na data | `200` com `disponivel=false`, `motivo` | Estado informativo; sem refeições |
| Cardápio adaptado ausente | `200` com `disponivel=false`, `motivo` | Estado informativo; nunca exibir o geral |
| Aluno sem escola/unidade | `200` com `alerta=true`, `motivo` | Estado de alerta; sem refeições |
| `GET /api/ocorrencias` filtro inválido | `400` | Erro no filtro; manter lista anterior |
| Ocorrência com descrição < 5 ou > 1000 | `400` | Erro no campo; preservar texto |
| Ocorrência de aluno não vinculado | `403` | Mensagem genérica; não exibir dados |
| Cancelar ocorrência fora do dia da criação | `409` | Botão só aparece quando `podeCancelar=true` |
| Restrição inexistente/inativa | `400` | Erro no campo; recarregar catálogo |
| Solicitação duplicada | `409` | Mensagem com id existente; link para o pedido |
| Cancelar solicitação em `EmAnalise` ou estado final | `409` | Botão só aparece em `Aberta`; mensagem se chamado |
| Feedback com aluno não vinculado | `403` | Mensagem genérica; não enviar |
| Feedback assunto/mensagem fora do limite | `400` | Erro no campo; preservar texto |
| Encerrar feedback não respondido | `409` | Botão indisponível; mensagem se chamado |
| Resposta de feedback por papel sem permissão | `403` | Ação não é exibida na área de pais |
| JSON malformado / content-type inválido | `400`/`415` | Mensagem do contrato único `{ erro }` |
| Erro interno | `500` | Mensagem genérica; preservar formulário |
| Falha de rede | sem resposta | Mensagem “não foi possível conectar”; botão repetir |
| Paginação além do total | `200` com lista vazia | Página vazia; controles ajustados |
| Sessão expira durante escrita | `401` | Logout; dados não são reenviados automaticamente |

## Contracts and Required Changes

### Reused without change

- `GET /api/aluno`
- `GET /api/aluno/{id}/cardapio?data=yyyy-MM-dd`
- `GET /api/ocorrencias`, `GET /api/ocorrencias/{id}`, `POST /api/ocorrencias`,
  `POST /api/ocorrencias/{id}/cancelar`
- `GET /api/solicitacoes`, `POST /api/solicitacoes`,
  `POST /api/solicitacoes/{id}/cancelar`
- `GET /api/restricoes`
- `GET /api/feedbacks`, `POST /api/feedbacks`, `POST /api/feedbacks/{id}/encerrar`
- `GET /api/notificacoes`, `POST /api/notificacoes/{id}/ler`

### Required change (feedback only)

- `Feedback`: `AlunoId` (`int?`), `AlunoNome` (`string?`), FK opcional com `OnDelete(SetNull)`.
- `CriarFeedbackDTO`: já tem `AlunoId`; o serviço passa a persistir.
- `FeedbackResumoDTO`/`FeedbackDTO`: `AlunoId` (`int?`) e `Aluno` (`string?`).
- `GET /api/feedbacks`: parâmetro opcional `alunoId` (`int?`).
- Migration `FeedbackAluno` com índice em `AlunoId`.
- Nenhuma alteração em `Aluno`, `Ocorrencia`, `Solicitacao`, `Cardapio` ou `Restricao`.

### Frontend

- `CardapioApi.ts`: `buscarCardapioDoAluno(alunoId: number, data: string)`.
- `FeedbackApi.ts`: `alunoId?: number` no POST e no filtro de listagem.
- `OcorrenciaApi.ts`: adicionar `de?: string` e `ate?: string` a `FiltrosOcorrencia` (o backend já
  aceita; o serviço do frontend não os envia hoje).
- Reutilizar `AlunoApi`, `SolicitacaoApi`, `RestricaoApi`, `NotificacaoApi`.

## Dependencies

- Implementadas: 010, 016, 017, 018, 027, 028, 029 (backend), 033, 034.
- Planejada: 025 (não é dependência da 035; a agregação futura pertence à 025).
- Pendências: PEND-042 (frontend da 029), PEND-047 (validação manual da 034), PEND-048
  (matriz × cardápio), PEND-028 (motor SDD), PEND-049 (testes de frontend).
- Nenhuma biblioteca nova.

## Traceability

| Requisito | Value block | Componente/contrato | Task |
| --- | --- | --- | --- |
| RF1 | V24 | `PainelPais`, `GET /api/aluno` | T4, T5.1 |
| RF2–RF6 | V24 | `CardapiosPais`, `CardapioApi`, `GET /api/aluno/{id}/cardapio` | T6 |
| RF7–RF10 | V24, V6 | lista/detalhe de ocorrências, `OcorrenciaApi` | T7 |
| RF11–RF14 | V24, V6 | formulário de ocorrência, `POST /api/ocorrencias` | T8 |
| RF15–RF20 | V24, V6 | `SolicitacoesPais`, `SolicitacaoApi` | T9 |
| RF21–RF25 | V24, V6 | `FeedbackPais`, `FeedbackApi`, backend de feedback | T1–T3, T10 |
| RF26–RF32 | V24, V17 | layout, seletor, estados e erros | T4, T5, T11 |
| NFR1–NFR12 | V24, V17 | design, testes e revisão | T12–T14 |

## Acceptance Criteria

### Cardápios

- [ ] `/pais` exibe exatamente os filhos de `GET /api/aluno`, com nome, escola e série.
- [ ] Com dois filhos, trocar o seletor muda a consulta para o `alunoId` escolhido.
- [ ] `GET /api/aluno/{id}/cardapio?data=YYYY-MM-DD` é chamado com a data escolhida.
- [ ] `disponivel=true` renderiza todas as refeições; o filtro por refeição altera apenas a
  exibição local e inclui a opção “Todas”.
- [ ] `disponivel=false` exibe `motivo` e não renderiza refeições nem o cardápio geral.
- [ ] Cardápio adaptado exibe as restrições consideradas.

### Ocorrências

- [ ] A lista exibe apenas ocorrências dos filhos e respeita os filtros de filho/tipo/status/período.
- [ ] `tamanho=20` é enviado; total e página são exibidos.
- [ ] O detalhe mostra histórico de status e `podeCancelar`.
- [ ] O relato cria `{ tipo: "Aluno", alunoId, descricao }`, mostra confirmação e aparece na lista.
- [ ] A ocorrência criada aparece na consulta dos profissionais com leitura no escopo (matriz 028).
- [ ] Descrição com menos de 5 caracteres é bloqueada na UI e recusada pelo backend.
- [ ] Ocorrência de aluno não vinculado responde `403` e não aparece em nenhuma tela.
- [ ] O botão de cancelar só aparece quando `podeCancelar=true`; `Aberta` + mesmo dia atualiza o
  detalhe; fora disso exibe `409`.

### Alergia/restrição

- [ ] O seletor de restrição contém somente itens ativos de `GET /api/restricoes`.
- [ ] O envio cria `InclusaoAlergia` com `alunoId`, `restricaoNome` e `descricao`.
- [ ] O cadastro do aluno não muda antes da aprovação.
- [ ] A aprovação do ADM adiciona a restrição e grava `RestricaoAdicionada` na trilha.
- [ ] A recusa não altera o aluno.
- [ ] A lista mostra status/resposta e o cancelamento só é oferecido em `Aberta`; `EmAnalise`
  ou estado final exibe `409`.
- [ ] A tela do responsável não exibe ações de aprovar ou recusar.
- [ ] Pedido duplicado responde `409` com o id existente.

### Feedback

- [ ] O formulário envia assunto, mensagem e `alunoId` opcional.
- [ ] Feedback com aluno vinculado persiste `AlunoId` e `AlunoNome`; a lista exibe o aluno.
- [ ] Feedback sem aluno é aceito e exibe “Sem aluno”.
- [ ] Feedback com aluno de outro responsável responde `403`.
- [ ] A lista mostra somente feedbacks do autor, com status, resposta e `podeEncerrar`.
- [ ] O autor encerra somente feedback `Respondido`.
- [ ] Responder não é oferecido na área de pais.
- [ ] Nenhum dado clínico do aluno é exibido ou enviado no feedback.

### Gerais

- [ ] Com mais de um filho, nenhuma ação de aluno ocorre sem seleção explícita.
- [ ] Nenhuma tela exibe aluno fora do vínculo.
- [ ] Estados vazios e de erro da tabela de edge cases são implementados.
- [ ] `LayoutPais` tem as cinco abas e o cabeçalho com `SinoNotificacoes`.
- [ ] Datas são exibidas em `dd/MM/yyyy` e enviadas em `yyyy-MM-dd`.
- [ ] `dotnet build`, `dotnet test` (792+), `npm run build` e `npm run lint` passam.
- [ ] Nenhuma rota nova e nenhuma dependência nova.

### NFR

- [ ] NFR1: número de controllers/rotas inalterado; `DocumentationTests` passa.
- [ ] NFR2: `backend.csproj` e `package.json` sem dependência nova.
- [ ] NFR3: contagem de requisições iniciais por página conforme a regra (≤3).
- [ ] NFR4: listas enviam `tamanho=20` e exibem total/página.
- [ ] NFR5: `npm run build` 0 erros e `npm run lint` 0 erros.
- [ ] NFR6: 792 testes existentes + novos testes do feedback, 0 falhas.
- [ ] NFR7: nenhuma autorização no frontend; testes de `403` passam.
- [ ] NFR8: 100% dos inputs com label/aria e foco visível.
- [ ] NFR9: textos pt-BR, números com `toLocaleString("pt-BR")`, datas conforme RF32.
- [ ] NFR10: nenhum CPF/senha/texto clínico em URL, log ou erro.
- [ ] NFR11: exatamente uma migration nova, sem editar migration existente.
- [ ] NFR12: nenhum arquivo fora de `design.md` §File / Module Layout.

## Resolved Decisions

- DA1: catálogo ativo apenas; texto livre fora de escopo.
- DA2: 035 independente da 025; sem endpoint agregado.
- DA3: feedback persiste aluno + snapshot; sem aluno, ambos nulos.
- DA4: `AdminSecretaria` decide alergia; `Nutricionista` lê.
