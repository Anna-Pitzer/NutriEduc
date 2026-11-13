<!-- AI-ONLY. Durable cross-session memory. Never shown to end users. Keep entries minimal and factual. -->

# Cross-Session Memory

Decisions, rationale, and progress that must survive across sessions live here;
conversation history alone is not durable memory.

## 2026-10-30 — Estado do planejamento SDD

- `implementacoes.md` tem 128 funcionalidades em cinco blocos (Nutrição 39,
  Merendeiras 24, Pais 18, Secretaria 24, Escolas 23).
- Partição em 11 specs novas: 009 (46), 010 (3 + base para 2 da 017), 011 (10),
  012 (17), 013 (3), 014 (4), 015 (9), 016 (10), 017 (10), 018 (7), 019 (8);
  001 cobre 1. Soma = 128 (L12).
- Todas as 11 têm `requirements.md`, `design.md` e `task.md` com `status: created`.
- Ordem de execução derivada do grafo, não da numeração: `012 → 014 → 013 → 015`
  (L13). Cadeia completa: `009 → 010 → 011 → 012 → 014 → 013 → 015 → 016 → 017 →
  018 → 019`.
- 006–008 têm os três documentos e nenhuma task executada; a implementação segue a
  ordem `006 → 007 → 008` antes da 009.

## Decisões estruturais registradas

- ADR-007: escola/unidade são chaves de autorização; `Aluno.UnidadeId` anulável.
- ADR-008: desativação em vez de exclusão para cadastros referenciados.
- ADR-009: histórico append-only com snapshots de autor e alvo.
- ADR-010: concorrência otimista por `[ConcurrencyCheck]` (SQLite sem rowversion).
- ADR-011: compatibilidade é função pura com severidade única (usada por 013/014/015).
- ADR-012: relatório é tabela; exportadores são plugáveis e não consultam banco.

## Bloqueios conhecidos

- `management/roadmap.md` é a única fonte de verdade do planejamento, com 21 linhas
  `planned`; `PLANEJAMENTO.md` foi descartado. A promoção de status é feita por
  `/spec-new`, `/spec-implement` e `/spec-test`.
- Frontmatter `status: created` é o valor que o próprio `/spec-new` grava
  (`templates/prompts/spec_new.md.ejs:125`); o ciclo de vida vive no roadmap.
- `frontend/node_modules` instalado (51 pacotes) e `frontend/.env` criado localmente
  (ambos ignorados pelo git).

## Estado do código (verificado em 2026-10-31)

- `dotnet test backend.Tests`: 236/236 verdes (156 unitários + 80 de integração HTTP).
- Gates de frontend passam: `npm run build` OK e `npm run lint` com 0 erros.
- 7 controllers com rota (inclui `BootstrapController`) + `RespostaController` base;
  8 migrations (a última é `BootstrapPorUnidade`).
- Specs 001–009 e 020–022: tasks executadas (a 009 com T13 transferida para a 021).
  Especificadas e não implementadas: 010–019.

## Bootstrap v2 (2026-10-31)

- **Unidade é da rede, não da escola (ADR-013).** `UnidadeEscolar.EscolaId` é opcional
  (dado legado) e o nome é único na rede. `/api/escolas/{id}/unidades` foi substituído por
  `GET`/`POST /api/unidades`; a tela `/secretaria/unidades` não pede escola.
- `ConfiguracaoBootstrap` (interruptor booleano) foi **removida**. O painel agora emite um
  token de **30 minutos**, de uso único, preso à unidade de quem gerou (`TokensDeCadastro`).
- O token de ambiente (`Bootstrap__Token`) vale **uma vez por unidade**, registrado em
  `ConsumosBootstrap`; a unidade é informada pelo nome exato e precisa existir.
- `AdminSecretaria` passou a aceitar `UnidadeId` como **atribuição**, não escopo — o papel
  continua global. `Nutricionista` e `AdminEscola` seguem recusando unidade.
- Efeito colateral em **PEND-024**: sem `Aluno.EscolaId`, o escopo de aluno de
  `AdminEscola` ficou limitado a unidades legadas com escola; os testes de vínculo passaram
  a usar `AdminSecretaria`.
- **Unidade nasce no bootstrap (2026-10-31).** `POST /api/unidades` e
  `EscolaService.CriarUnidade` foram removidos: o token de ambiente cria a unidade junto
  com o primeiro administrador (`ResolverOuCriarUnidade`). A tela `/secretaria/unidades`
  só lista, renomeia, ativa/desativa e exclui.
- **Exclusão definitiva (ADR-014).** `DELETE /api/escolas/{id}`, `/api/unidades/{id}` e
  `/api/usuarios/{id}`, sempre com bloqueio por referência (`409`): escola com usuários ou
  unidade legada; unidade com usuário, aluno, token ou consumo de bootstrap; usuário com
  própria conta, último `AdminSecretaria`, vínculo de responsável ou token emitido. A
  checagem vive em `ExclusaoService`.
- `TimeProvider` é injetado no backend; o relógio falso dos testes parte da hora real
  porque o cookie de sessão também usa `TimeProvider`.
- Limitação conhecida: a criação grava usuário e consumo em dois `SaveChanges` (sem
  transação única), registrada no `design.md` da 022.

## Spec 016 — auditoria técnica concluída; validação manual pendente

- Backend + frontend de ocorrências implementados. Gates: `dotnet test` 703/703,
  `npm run build` OK, `npm run lint` 0 erros (6 avisos de react-hooks pré-existentes).
- **RNF3 (snapshot de nomes):** `Ocorrencia` grava `AlunoNome`, `CardapioNome`,
  `RefeicaoNome` e `AlimentoNome` no momento do registro (ADR-009 — "cópia dos nomes
  relevantes do alvo"). Renomear/excluir aluno, cardápio, refeição ou alimento não
  reescreve o que a ocorrência exibe; a FK vira `null` e o nome é preservado. O
  `design.md` da 016 omite esses campos (omissão, não conflito).
- **Fuso do cancelamento (RF9):** "mesmo dia" = dia civil de `America/Sao_Paulo`
  (UTC-3), decidido pelo dono. `Ocorrencia.Cancelar` usa `TimeZoneInfo`; `OcorrenciaService`
  injeta `TimeProvider`. Testes cobrem a fronteira UTC×local.
- **Status:** permanece `created` no roadmap. **Não promovido a `audited`** porque
  (1) validação manual no navegador está pendente (delegada a outro integrante) e
  (2) **PEND-028** bloqueia o motor SDD (`normalizeSpecNumber`/`parseRoadmap` comparam
  a célula crua `"016"` com `"16"`); a correção é do binário `dscode.exe`, externa ao
  repositório. Não alterei o status manualmente.
- **Próximo passo:** validação manual por papel (checklist no relatório de auditoria) e,
  após a correção do PEND-028, promover via `/spec-implement` → `/spec-test`.

## Spec 033 — backend implementado; frontend e validação manual pendentes

- **Base Git:** `branch-pedro` fez fast-forward para `origin/main` (`86d1842`); a 018 já
  estava mergeada (PR #13).
- **Backend completo (2026-11-11):** `SolicitacaoDeAcesso` + `StatusSolicitacaoAcesso` +
  `TipoCadastroSolicitado` + `PoliticaDeAprovacao` (pura), `RegistrarAcessoService`,
  `AprovarAcessoService`, `SolicitacaoAcessoController` (`/api/solicitacoes-acesso`),
  migration `20261111004628_SolicitacoesDeAcesso` (índice único filtrado de pendente +
  índice de fila + `Status` como token de concorrência). `UsuarioController.PostAdd` agora
  aceita `CadastroEntrada` (`tipo`,`unidadeId`,`crn`). Login de conta sem papel virou
  `401` genérico ("Email ou senha incorretos."). CPF: 11 dígitos no domínio + unicidade no
  serviço (409), **sem índice no banco** (decisão D7).
- **Decisões do dono (2026-11-11):** CPF único no serviço (sem índice); cadastro público
  anônimo, fora da matriz (a matriz mantém só `SolicitacaoDeAcesso → AdminSecretaria`).
- **Gates:** `dotnet test` **775/777** (41 testes novos: domínio, serviço, integração com
  rollback e concorrência). As 2 falhas restantes são pré-existentes e de fronteira de
  fuso — `OcorrenciaApiTests.Cancelar_NoMesmoDia_PeloAutor_200` e
  `Cancelar_NoDiaSeguinte_409` — falham perto da meia-noite UTC×`America/Sao_Paulo`;
  não têm relação com a 033. `npm run build` OK; `npm run lint` 0 erros (6 avisos
  pré-existentes). Workaround `env APPDATA='C:\Users\pv994\AppData\Roaming'
  ProgramFiles='C:\Program Files' dotnet ...` resolve o `path1` do NuGet (PEND-016).
- **Decisão do dono (2026-11-11, 2ª rodada):** aprovada a consulta pública mínima
  `GET /api/unidades/ativas` (anônima, apenas `id` e `nome`, só ativas) para o cadastro; a
  rota administrativa `GET /api/unidades` continua autenticada. O backend revalida a unidade
  (existente e ativa) no `RegistrarAcessoService`.
- **Frontend entregue (2026-11-11):** cadastro em 2 etapas (`login.tsx` + `authContext`),
  fila `SolicitacoesAcesso.tsx` (listar/aprovar/recusar) + rota `/secretaria/solicitacoes-acesso`
  + aba no menu, e remoção da sugestão `Merendeira` (`papelInicial`) em `Usuarios.tsx`.
  Reconsideração: endpoint `POST /api/solicitacoes-acesso` existe; a UI de reconsideração
  fica para a validação manual.
- **Correção fora da 033 (016):** `Ocorrencia.CriadoEmUtc`/`AtualizadoEmUtc` eram relidos do
  SQLite como `DateTimeKind.Unspecified`; `NormalizarUtc` tratava como hora local e deslocava
  a data na fronteira 00:00–03:00 UTC. Corrigido com `HasConversion(... DateTimeKind.Utc)` no
  `ConnectionContext` (mesmo padrão de `TrilhaAcao`). Os 2 testes de cancelamento passaram a
  ser determinísticos sem enfraquecer a regra `America/Sao_Paulo`.
- **Auditoria final (2026-11-11):** **T13 parcialmente indisponível** — reconsideração
  (`POST /api/solicitacoes-acesso`) entregue e testada; o diagnóstico de legado global
  (`GET /api/usuarios/diagnostico-sem-solicitacao`) foi **desabilitado** porque a leitura
  **global** de nome+e-mail de órfãos conflita com ADR-021/028 ("não existe perfil global").
  Rota removida de `UsuarioAdminController` (→ 4xx); preservados
  `AprovarAcessoService.DiagnosticarSemSolicitacao`, `ListarSemPapel` e `ExistePorUsuario`
  para reabilitação futura. Conflito registrado como **PEND-046** (decisão pendente do grupo).
  Adicionados testes: reconsideração (nova solicitação após recusa + pendente duplicada 409),
  concorrência de responsável sem vínculo duplicado, rollback transacional da aprovação
  (usuário/solicitação/papel/vínculo/trilha/notificação) e regressão determinística do fuso
  (round-trip SQLite em outro contexto + cancelamento na fronteira UTC/`America/Sao_Paulo`).
  `dotnet test` **787/787**; `npm run build`/`lint` OK.
- **Limitação documentada (CPF/D7):** unicidade só no serviço, sem índice no banco. SQLite
  serializa escritas, então duas operações que consultam antes de gravar podem ambas passar a
  checagem — não há garantia de unicidade em corrida real.
- **Não promovido** (`created` no roadmap); status formal é do motor SDD (`/spec-test`), não
  da edição manual. Validação manual no navegador é de outro integrante.

## Spec 034 — vínculo responsável↔aluno (2026-11-12)

- **Implementado:** vínculo obrigatório na criação administrativa e na definição de papel
  (`alunosIds` em `CriarUsuarioDTO`/`AtualizarUsuarioDTO`); usuário + vínculos + trilha na mesma
  transação; busca de aluno por `busca`/`escolaId`/`serie` no SQL; `SeletorDeAlunos` na aprovação e
  na tela de usuários; painel de responsáveis na página de alunos (listar/vincular/desvincular);
  `VinculoService` recusa usuário desativado.
- **Correção de acesso:** `HomePais` passou a usar `GET /api/unidades/ativas` (anônima). O
  `CardapiosPublicados` chamava `GET /api/unidades`, que exclui `Responsavel` e devolvia `403`,
  deixando o select de unidade vazio.
- **Bug corrigido de passagem:** `UsuarioAdminApi.exigeEscola` incluía `Responsavel`, então criar
  ou definir esse papel pela tela de usuários enviava `escolaId` e o domínio recusava com
  "O responsável não pertence a uma escola".
- **Decisão:** contato manual do aluno (`nomeResponsavel`/`telefoneResponsavel`) e conta vinculada
  são dados distintos; a página exibe os dois sem sincronização.
- **Testes:** novo `VinculoResponsavelApiTests` (5 casos). Ajustados `PapeisApiTests`,
  `MatrizPermissoesTests`, `AuditoriaAcaoTests` e `UsuarioServiceTests` para a nova invariante.
  `dotnet test` **792/792**; `npm run build` OK; `npm run lint` 0 erros (6 avisos pré-existentes).
- **Nota de execução:** `backend.exe` estava em uso, então os gates rodaram com
  `-p:OutDir=agentbuild-test/x/y/` (o `DocumentationTests` resolve o README por profundidade de
  diretório). Nada foi commitado nem teve status promovido manualmente.
- **Pendente:** validação manual no navegador; PEND-025 (telas de restrições/histórico para
  responsável) e PEND-028 (promoção pelo motor) permanecem abertos.

## Estado consolidado da documentação (2026-11-12)

- **Código:** 20 controllers (19 de recurso + base), 26 serviços de aplicação, 27 migrations,
  `dotnet test` **792/792**, `npm run build` OK e `npm run lint` 0 erros (6 avisos).
- **Roadmap atualizado por evidência:** `audited` 001–009, 013–015, 020–022, 027, 028, 030;
  `implemented` 010–012, 016–018, 023, 024, 031–034; `in-progress` 029; `created` 019;
  `planned` 025, 026. A promoção formal continua sendo do `/spec-test`; o PEND-028 bloqueia.
- **Documentos sincronizados:** `README.md` (estado, páginas, testes, pendências), `arch.md`
  (§1 componentes, §9 dívida, §10 estado verificado, §11 mapa 009–034 e seção da Spec 034),
  `vision.md` (estado consolidado V1–V23), `pending-items.md` (PEND-010/011/038/040/041
  encerrados; PEND-039/044/042/027 atualizados; PEND-047–049 abertos) e `lessons.md` (L14/L15).
- **Lacunas vivas:** 019 (relatórios/exportação) sem implementação; 029 sem frontend de
  montagem/detalhe (PEND-042); diagnóstico global de contas sem papel desabilitado (PEND-046);
  matriz × `CardapioService.PodeLer` divergem para `Responsavel` (PEND-048); frontend sem testes
  (PEND-049); validação manual da 034 pendente (PEND-047).

## Spec 035 — portal dos pais (somente especificação)

- **Criada em 2026-11-12** em `management/specs/035-portal-pais-responsaveis/`
  (`requirements.md`, `design.md`, `task.md`). Nenhum arquivo de código foi criado ou alterado.
- **Escopo:** painel dos filhos, cardápio servido por aluno/dia/refeição, leitura e relato de
  ocorrências, solicitação de inclusão de alergia com acompanhamento e feedback com aluno
  opcional.
- **Reuso:** `GET /api/aluno`, `GET /api/aluno/{id}/cardapio`, ocorrências, solicitações,
  catálogo de restrições, feedback e notificações. Nenhum endpoint novo de agregação; o painel
  da família continua na Spec 025.
- **Única mudança de modelo prevista:** persistir `AlunoId` + snapshot `AlunoNome` em `Feedback`
  (FK `SetNull`), com migration nova.
- **Decisões em aberto:** catálogo vs texto livre na alergia (DA1), ordem em relação à 025 (DA2)
  e confirmação do snapshot do aluno no feedback (DA3).
- **Status:** `verified` no roadmap (2026-11-12); implementação não iniciada.

## Sistema de notificações — correção e cobertura (2026-11-13)

- **Lacuna principal corrigida:** o cadastro público criava `SolicitacaoDeAcesso` sem emitir
  notificação. `RegistrarAcessoService` agora avisa, na mesma transação, todos os
  `AdminSecretaria` ativos da unidade do pedido (`NotificacaoDeAcessoService.NovaSolicitacao`).
  A criação manual do ADM (`AprovarAcessoService.Criar`) segue a mesma regra e não notifica o
  próprio autor.
- **Eventos de criação dos demais módulos:** `SolicitacaoService.Criar`/`CriarDadosCadastrais`
  notifica o dono da decisão por tipo (`Nutricionista` para cardápio/ingrediente,
  `AdminSecretaria` para alergia/restrição/dado cadastral), exceto o solicitante;
  `OcorrenciaService.Criar` notifica `Nutricionista` da unidade e `AdminEscola` da escola,
  exceto o autor.
- **Novos tipos:** `TipoNotificacao.SolicitacaoAcesso`, `NovaSolicitacao` e
  `OcorrenciaRegistrada`. Enum persistido como inteiro; não houve migration.
- **Anti-duplicidade:** `INotificacaoRepository.ExistePorReferencia` +
  `NotificacaoService.NotificarUmaVez`/`NotificarVariosUmaVez` (tipo + referência). Usado
  somente em eventos de criação; transições de status continuam emitindo por mudança.
- **Segurança mantida:** emissão e leitura continuam por destinatário; nenhuma rota nova.
- **Frontend:** o sino usa `total` de `GET /api/notificacoes?naoLidas=true&tamanho=1` como
  contador real (não mais o filtro da lista carregada), exibe data/hora, diferencia lida/não
  lida, mantém a navegação mesmo se a marcação de leitura falhar e abre a solicitação de
  acesso via `/secretaria/solicitacoes-acesso?abrir=<id>`.
- **Evidência:** `dotnet test` **819/819** (10 novos: 8 de integração em
  `NotificacaoEventosApiTests`, 2 de unidade em `NotificacaoServiceTests`); `npm run build` OK;
  `npm run lint` 0 erros (21 avisos pré-existentes fora dos arquivos alterados). Gates rodados
  com `-p:OutDir=agentbuild-test/x/y/` porque `backend.exe` estava em uso.
- **Limitação conhecida:** conta recusada continua sem papel e não autentica (decisão da Spec
  033), então o solicitante não vê a notificação de recusa até uma futura aprovação; a
  notificação existe e fica persistida. Nenhum status de spec foi promovido manualmente.
- **Causa do sintoma "não aparece notificação" (2026-11-13):** o processo em `:5001` era o
  `backend.exe` de 2026-11-12, anterior às correções; a base (`backend/database.db`) comprovava
  o comportamento antigo (solicitação aprovada sem notificação para o ADM). Processo encerrado,
  rebuild feito e backend reiniciado; um cadastro real gerou a notificação `SolicitacaoAcesso`
  para o `AdminSecretaria` da unidade. O sino também passou a atualizar a cada 30 s e no foco
  da janela, sem F5.

## Cardápio dos pais — geral e adaptado (2026-11-13)

- **Antes:** `GET /api/aluno/{id}/cardapio` devolvia só a versão servida (geral sem restrição,
  adaptada com restrição). Com restrição e sem adaptada publicada, o responsável ficava sem
  qualquer cardápio; não havia como ver as duas versões.
- **Agora:** o mesmo endpoint devolve `cardapioGeral` (geral publicado da unidade na data) e
  `cardapioAdaptado` (adaptada publicada da escola + conjunto exato). `cardapio` continua sendo
  a versão servida e `requerAdaptacao`/`alerta` sinalizam a ausência da adaptada. O geral nunca
  é promovido a versão servida quando o aluno tem restrição.
- **Tela:** `CardapioDoAluno` renderiza os dois cartões ("Cardápio geral" e "Cardápio adaptado
  de <aluno>"), mantém o aviso quando a adaptada falta e filtra refeições por nome (ids diferem
  entre versões). O responsável só alcança os próprios filhos.
- **Evidência:** `dotnet test` **821/821** (2 novos em `CardapioPaisApiTests`: geral + adaptado
  na mesma consulta, incluindo o estado "só geral publicado", e 403 para aluno sem vínculo);
  `npm run build` OK; `npm run lint` 0 erros.
- **Fora de escopo:** PEND-048 (leitura direta de `/api/cardapios` pelo responsável) continua
  aberto; a página dos pais não depende dessa rota.

## Notificações roláveis e anafilaxia na análise (2026-11-13)

- **Scroll do sino:** o `<header>` tinha `overflow-hidden` e cortava o painel na altura de
  240 px. O clipping saiu do header; o painel agora é uma coluna com cabeçalho fixo e lista
  `max-h-80 overflow-y-auto overscroll-contain`.
- **Anafilaxia:** novo `POST /api/solicitacoes/{id}/anafilaxia` marca `Aluno.Anafilaxia` para
  solicitações `InclusaoAlergia` em `Aberta`/`EmAnalise`, grava `HistoricoAluno` com origem
  `Solicitacao:<id>` e é idempotente. Autorização é a mesma da decisão
  (`AdminSecretaria` da unidade); tipo diferente devolve 400 e papel sem decisão, 403. A
  decisão da solicitação não muda.
- **Revisão (2026-11-13):** Nutricionista da unidade também marca anafilaxia. O botão não
  aparecia para esse papel porque `podeDecidir` exigia `AdminSecretaria`; a tela passou a usar
  `podeMarcarAnafilaxia` (ADM ou Nutricionista) e o bloco de ações renderiza para ambos.
- **Revisão 2 (2026-11-13):** o botão também aparece na área dos pais (`/pais/solicitacoes`),
  no detalhe do pedido de `InclusaoAlergia` em aberto/análise. O backend autoriza o
  `Responsavel` solicitante ou vinculado ao aluno (`NegacaoDeVisaoDoResponsavel`) e mantém
  403 para responsável de outra família.
- **Tela:** em `/solicitacoes` e `/pais/solicitacoes`, o botão "Marcar anafilaxia" aparece no
  detalhe de uma `InclusaoAlergia` enquanto ela está aberta/em análise, com confirmação.
- **Evidência:** `dotnet test` **828/828** (5 novos em `SolicitacaoAnafilaxiaApiTests`);
  `npm run build` OK; `npm run lint` 0 erros.

## Auditoria da Spec 034 — vínculo responsável↔aluno (2026-11-13)

- **Cobertura verificada:** busca `busca/serie` no SQL sem ampliar escopo (`FiltrosDeConsulta`
  + `AlunoRepository`); criação/atualização de `Responsavel` com `alunosIds` validados antes de
  mutar e vínculos+trilha na mesma transação; `VinculoService` recusa usuário inativo e
  duplicado (409); rotas listar/vincular/desvincular; frontend com `SeletorDeAlunos`,
  `VinculoApi` e painel de responsáveis.
- **Correção 1 (vertical slice):** `VisualizarAlunos` mostrava Vincular/Desvincular também para
  `AdminEscola`, mas a matriz 028 dá apenas `R` em `Recurso.Vinculo` para esse papel — o clique
  resultava em 403. A flag passou a ser exclusiva de `AdminSecretaria` (RF9).
- **Correção 2 (DRY):** o bloco "cria vínculo + `VinculoCriado`" estava duplicado em
  `CriarUsuarioAdministrado` e `AtualizarUsuario`; extraído para `VincularAlunos`, agora
  idempotente nos dois caminhos.
- **Testes novos:** `VincularResponsavel_Duplicado_Retorna409` e
  `VincularResponsavel_UsuarioDesativado_Retorna409SemGravar`. Suíte **826/826**.
- **Observação (não bloqueante):** `RF10` cita `HomePais`, página que não existe mais (o portal
  usa `CardapiosPais` orientado ao filho; a listagem anônima de unidades ficou em
  `CardapiosPublicados`). Nenhuma tela `/pais` chama `GET /api/unidades`, então o 403 descrito
  não ocorre. Documentos da spec não foram alterados (imutáveis a partir de `implemented`).
- **Pendência humana:** T14 (validação manual no navegador) continua em `PEND-047`.

## Auditoria da Spec 035 — portal dos pais (2026-11-13)

- **Cobertura verificada:** `Feedback.AlunoId`/`AlunoNome` + DTOs + `FiltroFeedback` +
  `FeedbackService` (vínculo 403) + `FeedbackController?alunoId` + índice/migration
  `FeedbackAluno`; páginas `PainelPais`, `CardapiosPais`, `OcorrenciasPais`,
  `NovaOcorrenciaPais`, `SolicitacoesPais`, `FeedbackPais`; componentes compartilhados;
  `OcorrenciaApi` com `de/ate`; rotas sob `Responsavel`; cinco abas no `LayoutPais`.
- **Correção (RF20):** `InclusaoAlergia` duplicada agora responde `409` com o id da aberta
  (mesmo aluno + restrição, comparação case-insensitive), fechando o **PEND-051**. A tela de
  pais já tinha o botão "Abrir pedido existente #id".
- **Testes novos:** `Criar_InclusaoAlergiaDuplicada_Retorna409ComIdExistente` (cobre 409,
  restrição diferente permitida e novo pedido após cancelamento). Suíte **829/829**.
- **Desvios aceitos (não bloqueantes):**
  - `FiltroFeedback` vive em `backend/Domain/FiltroFeedback.cs`, não em `EscopoUsuario.cs`
    (design §Modify); o tipo e o contrato são os mesmos.
  - O cardápio do filho passou a expor `cardapioGeral` e `cardapioAdaptado` (revisão do dono
    em 2026-11-13); RF5/RF6 originais descreviam só a versão servida.
- **Pendência humana:** T14 segue em `PEND-050` (validação manual no navegador).

## Anafilaxia informada no pedido de alergia (2026-11-13)

- **Pedido:** o responsável precisa informar, ao criar a solicitação, que a nova alergia é
  anafilática (antes só existia o botão de marcar depois, no detalhe).
- **Modelo:** `Solicitacao.Anafilaxia` (`bool`, default `false`), migration
  `20261113142544_SolicitacaoAnafilaxia`; `CriarSolicitacaoDTO.Anafilaxia`; o valor só é
  aceito em `InclusaoAlergia`/`AlteracaoRestricao`.
- **Efeito:** na aprovação (`SolicitacaoService.Aprovar`), se `Anafilaxia` for `true`, o aluno é
  marcado com `AlunoService.MarcarAnafilaxia` na mesma transação da restrição, com trilha
  `Solicitacao:<id>`.
- **Tela:** `FormularioSolicitacao` ganhou o checkbox “Esta alergia é anafilática” (área dos
  pais); o detalhe exibe “Informada pelo responsável” nas telas dos pais e da análise.
- **Evidência:** `dotnet test` **830/830** (`SolicitacaoComAnafilaxia_Aprovada_MarcaOAluno`);
  `npm run build` OK; `npm run lint` 0 erros.







